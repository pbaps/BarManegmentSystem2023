using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
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
                model.AvailableStamps = db.Stamps.Where(s => s.ContractorId == contractorId && s.Status == "مع المتعهد")
                                          .OrderBy(s => s.SerialNumber).Take(500).ToList();
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
            ViewBag.IsContractorUser = isContractor;

            Action DoRepopulate = () => {
                if (isContractor)
                    model.AvailableStamps = db.Stamps.Where(s => s.ContractorId == model.ContractorId && s.Status == "مع المتعهد").OrderBy(s => s.SerialNumber).Take(500).ToList();
                else
                    ViewBag.ContractorsList = new SelectList(db.StampContractors.Where(c => c.IsActive), "Id", "Name", model.ContractorId);
            };

            if (!ModelState.IsValid) { DoRepopulate(); return View(model); }

            var lawyerFile = db.GraduateApplications.Include(g => g.ApplicationStatus).FirstOrDefault(g => g.Id == model.LawyerId);
            if (lawyerFile == null) { TempData["ErrorMessage"] = "المحامي غير موجود."; DoRepopulate(); return View(model); }

            if (!LawyerStatusHelper.IsActiveLawyer(lawyerFile)) { TempData["ErrorMessage"] = "المحامي غير فعال."; DoRepopulate(); return View(model); }

            long start, end;
            if (!long.TryParse(model.StartSerial, out start)) { TempData["ErrorMessage"] = "رقم البداية خطأ."; DoRepopulate(); return View(model); }
            end = string.IsNullOrWhiteSpace(model.EndSerial) ? start : long.Parse(model.EndSerial);

            var stampsToSell = db.Stamps
                .Where(s => s.SerialNumber >= start && s.SerialNumber <= end && s.ContractorId == model.ContractorId && s.Status == "مع المتعهد")
                .ToList();

            if (!stampsToSell.Any()) { TempData["ErrorMessage"] = "لا توجد طوابع متاحة."; DoRepopulate(); return View(model); }

            var stampFeeType = db.FeeTypes.FirstOrDefault(f => f.Name.Contains("طوابع"));
            decimal lPer = stampFeeType?.LawyerPercentage ?? 0.4m;
            decimal bPer = stampFeeType?.BarSharePercentage ?? 0.6m;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
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
                            IsPaidToLawyer = false,
                            RecordedByUserId = (int)(Session["UserId"] ?? 1),
                            RecordedByUserName = Session["FullName"] as string ?? "System"
                        };
                        db.StampSales.Add(sale);

                        stamp.Status = "تم بيعه";
                        stamp.SoldToLawyerId = lawyerFile.Id;
                        stamp.DateSold = DateTime.Now;
                        db.Entry(stamp).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    // ✅ تسجيل تدقيق إداري فقط (لا يوجد قيد محاسبي هنا كما طلبت)
                    AuditService.LogAction("StampSaleAdmin", "Stamps", $"إثبات بيع {stampsToSell.Count} طوابع للمحامي {lawyerFile.ArabicName}");
                    TempData["SuccessMessage"] = $"تم إثبات بيع {stampsToSell.Count} طابع للمحامي بنجاح.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "خطأ في الحفظ: " + ex.Message;
                    DoRepopulate(); return View(model);
                }
            }
            return RedirectToAction("RecordSale");
        }

        [HttpPost]
        public JsonResult CheckLawyer(string searchKey)
        {
            var lawyer = db.GraduateApplications.Include(g => g.ApplicationStatus)
                .FirstOrDefault(g => g.MembershipId == searchKey || g.ArabicName.Contains(searchKey));
            if (lawyer == null) return Json(new { success = false });
            return Json(new { success = true, lawyerId = lawyer.Id, lawyerName = lawyer.ArabicName, isPracticing = LawyerStatusHelper.IsActiveLawyer(lawyer), lawyerBankName = lawyer.BankName, lawyerBankBranch = lawyer.BankBranch });
        }

        [HttpGet]
        public JsonResult GetContractorStamps(int contractorId)
        {
            var data = db.Stamps.Where(s => s.ContractorId == contractorId && s.Status == "مع المتعهد")
                .OrderBy(s => s.SerialNumber).Select(s => new { s.SerialNumber, s.Value }).Take(500).ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}