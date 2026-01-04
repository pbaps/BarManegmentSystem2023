using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanEdit")] // (استخدم صلاحية مناسبة، ربما "FinancialReports")
    public class StampShareManagementController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/StampShareManagement/Manage/5
        public ActionResult Manage(int? id) // id هو LawyerId (GraduateApplicationId)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var lawyer = db.GraduateApplications
                           .Include(g => g.ApplicationStatus)
                           .FirstOrDefault(g => g.Id == id);

            if (lawyer == null)
            {
                return HttpNotFound("لم يتم العثور على ملف المحامي.");
            }

            // 1. جلب الحصص المحجوزة (طوابع)
            var heldShares = db.StampSales
                .Where(s => s.GraduateApplicationId == id &&
                            s.IsOnHold == true &&
                            s.IsPaidToLawyer == false)
                .OrderByDescending(s => s.SaleDate)
                .ToList();

            // 2. جلب الحصص الجاهزة للدفع (طوابع)
            var releasedShares = db.StampSales
                .Where(s => s.GraduateApplicationId == id &&
                            s.IsOnHold == false &&
                            s.IsPaidToLawyer == false)
                .OrderByDescending(s => s.SaleDate)
                .ToList();

            var viewModel = new StampShareManagementViewModel
            {
                LawyerId = lawyer.Id,
                LawyerName = lawyer.ArabicName,
                LawyerStatus = lawyer.ApplicationStatus.Name,
                HeldShares = heldShares,
                ReleasedShares = releasedShares
            };

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            ViewBag.ErrorMessage = TempData["ErrorMessage"];

            return View(viewModel);
        }

        // POST: Admin/StampShareManagement/ReleaseShares (فك الحجز)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReleaseShares(List<int> selectedSaleIds, int lawyerId)
        {
            if (selectedSaleIds == null || !selectedSaleIds.Any())
            {
                TempData["ErrorMessage"] = "الرجاء تحديد معاملة واحدة على الأقل لفك حجزها.";
                return RedirectToAction("Manage", new { id = lawyerId });
            }

            var sharesToRelease = db.StampSales
                .Where(s => s.GraduateApplicationId == lawyerId &&
                            selectedSaleIds.Contains(s.Id) &&
                            s.IsOnHold == true)
                .ToList();

            foreach (var share in sharesToRelease)
            {
                share.IsOnHold = false;
                share.HoldReason = $"[تم فك الحجز يدوياً بواسطة: {Session["FullName"]}] " + share.HoldReason;
                db.Entry(share).State = EntityState.Modified;
            }

            db.SaveChanges();

            // >>> تسجيل العملية (Log) <<<
            AuditService.LogAction("Release Stamp Shares", "StampSales", $"LawyerId {lawyerId}, Count: {sharesToRelease.Count}");

            TempData["SuccessMessage"] = $"تم فك حجز ({sharesToRelease.Count}) معاملة بنجاح.";
            return RedirectToAction("Manage", new { id = lawyerId });
        }

        // POST: Admin/StampShareManagement/HoldShares (تطبيق الحجز)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HoldShares(List<int> selectedSaleIds, int lawyerId, string HoldReason)
        {
            if (selectedSaleIds == null || !selectedSaleIds.Any())
            {
                TempData["ErrorMessage"] = "الرجاء تحديد معاملة واحدة على الأقل لتطبيق الحجز عليها.";
                return RedirectToAction("Manage", new { id = lawyerId });
            }
            if (string.IsNullOrWhiteSpace(HoldReason))
            {
                TempData["ErrorMessage"] = "الرجاء كتابة سبب الحجز.";
                return RedirectToAction("Manage", new { id = lawyerId });
            }

            var sharesToHold = db.StampSales
                .Where(s => s.GraduateApplicationId == lawyerId &&
                            selectedSaleIds.Contains(s.Id) &&
                            s.IsOnHold == false &&
                            s.IsPaidToLawyer == false)
                .ToList();

            foreach (var share in sharesToHold)
            {
                share.IsOnHold = true;
                share.HoldReason = HoldReason;
                db.Entry(share).State = EntityState.Modified;
            }

            db.SaveChanges();

            // >>> تسجيل العملية (Log) <<<
            AuditService.LogAction("Hold Stamp Shares", "StampSales", $"LawyerId {lawyerId}, Count: {sharesToHold.Count}, Reason: {HoldReason}");

            TempData["SuccessMessage"] = $"تم حجز ({sharesToHold.Count}) معاملة بنجاح.";
            return RedirectToAction("Manage", new { id = lawyerId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}