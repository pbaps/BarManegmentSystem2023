using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class StampSalesController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/StampSales/RecordSale
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult RecordSale()
        {
            var model = new RecordStampSaleViewModel();
            // التحقق إذا كان المستخدم متعهد
            bool isContractor = Session["UserType"] != null && (Session["UserType"].ToString() == "Contractor" || Session["UserType"].ToString() == "Advocate");
            ViewBag.IsContractorUser = isContractor;

            if (isContractor)
            {
                if (Session["ContractorId"] == null)
                {
                    TempData["ErrorMessage"] = "جلسة المتعهد غير صالحة.";
                    return RedirectToAction("Index", "Home");
                }
                int contractorId = (int)Session["ContractorId"];
                model.ContractorId = contractorId;
                model.AvailableStamps = db.Stamps.Where(s => s.ContractorId == contractorId && s.Status == "مع المتعهد").OrderBy(s => s.SerialNumber).ToList();
            }
            else
            {
                ViewBag.ContractorsList = new SelectList(db.StampContractors.Where(c => c.IsActive), "Id", "Name");
            }
            return View(model);
        }

        // POST: Admin/StampSales/RecordSale
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult RecordSale(RecordStampSaleViewModel model)
        {
            bool isContractor = Session["UserType"] != null && (Session["UserType"].ToString() == "Contractor" || Session["UserType"].ToString() == "Advocate");

            Action DoRepopulate = () => {
                if (isContractor) model.AvailableStamps = db.Stamps.Where(s => s.ContractorId == model.ContractorId && s.Status == "مع المتعهد").OrderBy(s => s.SerialNumber).ToList();
                else ViewBag.ContractorsList = new SelectList(db.StampContractors.Where(c => c.IsActive), "Id", "Name", model.ContractorId);
            };

            if (!ModelState.IsValid) { DoRepopulate(); return View(model); }

            // 1. التحقق من المحامي
            var lawyerFile = db.GraduateApplications.Include(g => g.ApplicationStatus).FirstOrDefault(g => g.Id == model.LawyerId);
            if (lawyerFile == null)
            {
                TempData["ErrorMessage"] = "المحامي غير موجود.";
                DoRepopulate(); return View(model);
            }

            // استخدام Helper للتحقق من الصلاحية
            if (!LawyerStatusHelper.IsActiveLawyer(lawyerFile))
            {
                TempData["ErrorMessage"] = $"المحامي غير فعال (الحالة: {lawyerFile.ApplicationStatus.Name}).";
                DoRepopulate(); return View(model);
            }

            // 2. التحقق من الطوابع
            long start, end;
            if (!long.TryParse(model.StartSerial, out start)) { TempData["ErrorMessage"] = "رقم البداية خطأ."; DoRepopulate(); return View(model); }
            end = string.IsNullOrWhiteSpace(model.EndSerial) ? start : long.Parse(model.EndSerial);

            var stampsToSell = db.Stamps
                .Where(s => s.SerialNumber >= start && s.SerialNumber <= end && s.ContractorId == model.ContractorId && s.Status == "مع المتعهد")
                .ToList();

            if (!stampsToSell.Any())
            {
                TempData["ErrorMessage"] = "لا توجد طوابع متاحة في هذا النطاق.";
                DoRepopulate(); return View(model);
            }

            // 3. النسب والحسابات
            var stampFeeType = db.FeeTypes.FirstOrDefault(f => f.Name == "رسوم طوابع");
            decimal lPer = stampFeeType?.LawyerPercentage ?? 0.4m;
            decimal bPer = stampFeeType?.BarSharePercentage ?? 0.6m;

            // التحقق من مديونية المحامي للحجز على الحصة
            bool onHold = db.LoanInstallments.Any(i => i.LoanApplication.LawyerId == lawyerFile.Id && (i.Status == "مستحق" || i.Status == "متأخر"));

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var salesRecords = new List<StampSale>();

                    foreach (var stamp in stampsToSell)
                    {
                        var sale = new StampSale
                        {
                            StampId = stamp.Id,
                            ContractorId = model.ContractorId,
                            SaleDate = DateTime.Now,
                            GraduateApplicationId = lawyerFile.Id,
                            LawyerMembershipId = lawyerFile.MembershipId,
                            LawyerName = lawyerFile.ArabicName,
                            StampValue = stamp.Value,
                            AmountToLawyer = stamp.Value * lPer,
                            AmountToBar = stamp.Value * bPer,
                            IsOnHold = onHold,
                            HoldReason = onHold ? "حجز مديونية قروض" : null,
                            IsPaidToLawyer = false, // ستدفع لاحقاً عبر سند صرف أو مقاصة
                            RecordedByUserId = (int)Session["UserId"],
                            RecordedByUserName = Session["FullName"] as string
                        };
                        salesRecords.Add(sale);

                        // تحديث الطابع
                        stamp.Status = "تم بيعه";
                        stamp.SoldToLawyerId = lawyerFile.Id;
                        stamp.DateSold = DateTime.Now;
                        db.Entry(stamp).State = EntityState.Modified;
                    }

                    db.StampSales.AddRange(salesRecords);
                    db.SaveChanges();

                    // ============================================================
                    // === 💡 التكامل المالي: قيد تسوية (بدون إيصال قبض) 💡 ===
                    // ============================================================
                    bool entryCreated = false;
                    using (var accService = new AccountingService())
                    {
                        // استدعاء الدالة الخاصة بتسوية الطوابع (لا قبض نقدي هنا)
                        entryCreated = accService.GenerateEntryForStampSale(salesRecords, (int)Session["UserId"]);
                    }

                    // لا نوقف العملية إذا فشل القيد هنا، لكن نسجل تحذير
                    if (!entryCreated)
                    {
                        // AuditService.LogWarning(...);
                    }

                    transaction.Commit();
                    TempData["SuccessMessage"] = $"تم بيع {salesRecords.Count} طابع بنجاح، وتسجيل حصص المحامين.";
                    return RedirectToAction("RecordSale");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "خطأ: " + ex.Message;
                    DoRepopulate();
                    return View(model);
                }
            }
        }

        // Helpers (AJAX)
        [HttpPost]
        public JsonResult CheckLawyer(string searchKey)
        {
            var lawyer = db.GraduateApplications.Include(g => g.ApplicationStatus)
                .FirstOrDefault(g => g.MembershipId == searchKey || g.ArabicName.Contains(searchKey));

            if (lawyer == null) return Json(new { success = false, message = "غير موجود" });

            return Json(new
            {
                success = true,
                lawyerId = lawyer.Id,
                lawyerName = lawyer.ArabicName,
                isPracticing = LawyerStatusHelper.IsActiveLawyer(lawyer)
            });
        }

        [HttpGet]
        public JsonResult GetContractorStamps(int contractorId)
        {
            var availableStamps = db.Stamps
                .Where(s => s.ContractorId == contractorId && s.Status == "مع المتعهد")
                .OrderBy(s => s.SerialNumber)
                .Select(s => new { s.SerialNumber, s.Value })
                .ToList();
            return Json(availableStamps, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}