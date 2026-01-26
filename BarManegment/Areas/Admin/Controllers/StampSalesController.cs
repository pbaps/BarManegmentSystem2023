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

        // =========================================================================
        // 🛑 دالة الإصلاح الذاتي (معدلة لتتجاهل الطوابع المباعة)
        // =========================================================================
        private void FixStampsForContractor(int contractorId)
        {
            try
            {
                // التعديل الهام: أضفنا شرط AND S.Status <> N'تم بيعه'
                // لكي لا يقوم النظام بإرجاع الطوابع المباعة إلى عهدة المتعهد مرة أخرى
                db.Database.ExecuteSqlCommand(@"
                    UPDATE S
                    SET S.Status = N'مع المتعهد', S.ContractorId = SB_Issue.ContractorId
                    FROM Stamps S
                    INNER JOIN StampBooks SB ON S.StampBookId = SB.Id
                    CROSS APPLY (
                        SELECT TOP 1 ContractorId 
                        FROM StampBookIssuances I 
                        WHERE I.StampBookId = SB.Id 
                        ORDER BY I.IssuanceDate DESC
                    ) SB_Issue
                    WHERE SB.Status = N'مع المتعهد' 
                      AND (S.Status = N'في المخزن' OR S.ContractorId IS NULL) -- نصلح فقط ما هو في المخزن أو غير مربوط
                      AND S.Status <> N'تم بيعه' -- ⛔ شرط هام جداً: لا تلمس المباع
                      AND SB_Issue.ContractorId = {0}", contractorId);
            }
            catch { /* تجاهل الأخطاء */ }
        }

        // =========================================================================
        // GET: Admin/StampSales/RecordSale
        // =========================================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult RecordSale()
        {
            var model = new RecordStampSaleViewModel();
            bool isContractor = Session["UserType"] != null &&
                                (Session["UserType"].ToString() == "Contractor" || Session["UserType"].ToString() == "Advocate");
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

                FixStampsForContractor(contractorId);

                // جلب الطوابع واستبعاد الموجود في جدول المبيعات للتأكد
                model.AvailableStamps = db.Stamps
                    .Where(s => s.ContractorId == contractorId && s.Status == "مع المتعهد" && !db.StampSales.Any(ss => ss.StampId == s.Id))
                    .OrderBy(s => s.SerialNumber).Take(500).ToList();
            }
            else
            {
                ViewBag.ContractorsList = new SelectList(db.StampContractors.Where(c => c.IsActive), "Id", "Name");
            }
            return View(model);
        }

        // =========================================================================
        // POST: Admin/StampSales/RecordSale
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult RecordSale(RecordStampSaleViewModel model)
        {
            bool isContractor = Session["UserType"] != null &&
                                (Session["UserType"].ToString() == "Contractor" || Session["UserType"].ToString() == "Advocate");
            ViewBag.IsContractorUser = isContractor;

            int currentUserId;
            string currentUserName;

            if (Session["UserId"] != null)
            {
                currentUserId = (int)Session["UserId"];
                currentUserName = Session["FullName"]?.ToString() ?? "System";
            }
            else
            {
                var adminUser = db.Users.FirstOrDefault(u => u.UserType.NameEnglish == "Administrator");
                if (adminUser != null)
                {
                    currentUserId = adminUser.Id;
                    currentUserName = "System Auto";
                }
                else
                {
                    TempData["ErrorMessage"] = "خطأ: يجب تسجيل الدخول لإتمام العملية.";
                    return RedirectToAction("Login", "Account");
                }
            }

            if (isContractor)
            {
                if (Session["ContractorId"] != null) model.ContractorId = (int)Session["ContractorId"];
                else return RedirectToAction("Login", "Account");
            }

            Action DoRepopulate = () => {
                if (isContractor)
                    model.AvailableStamps = db.Stamps.Where(s => s.ContractorId == model.ContractorId && s.Status == "مع المتعهد" && !db.StampSales.Any(ss => ss.StampId == s.Id)).OrderBy(s => s.SerialNumber).Take(500).ToList();
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

            FixStampsForContractor(model.ContractorId);

            // التحقق الدقيق: الحالة "مع المتعهد" + غير موجود في جدول المبيعات
            var stampsToSell = db.Stamps
                .Where(s => s.SerialNumber >= start &&
                            s.SerialNumber <= end &&
                            s.ContractorId == model.ContractorId &&
                            s.Status == "مع المتعهد" &&
                            !db.StampSales.Any(ss => ss.StampId == s.Id)) // 🛑 شرط حاسم
                .ToList();

            if (!stampsToSell.Any()) { TempData["ErrorMessage"] = "لا توجد طوابع متاحة، أو أنها بيعت مسبقاً."; DoRepopulate(); return View(model); }

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
                            RecordedByUserId = currentUserId,
                            RecordedByUserName = currentUserName
                        };
                        db.StampSales.Add(sale);

                        stamp.Status = "تم بيعه";
                        stamp.SoldToLawyerId = lawyerFile.Id;
                        stamp.DateSold = DateTime.Now;
                        db.Entry(stamp).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    AuditService.LogAction("StampSale", "Stamps", $"تم بيع {stampsToSell.Count} طوابع للمحامي {lawyerFile.ArabicName}");
                    TempData["SuccessMessage"] = $"تم تسجيل بيع {stampsToSell.Count} طابع بنجاح.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "خطأ أثناء الحفظ: " + ex.Message;
                    DoRepopulate();
                    return View(model);
                }
            }
            return RedirectToAction("RecordSale");
        }

        // =========================================================================
        // AJAX: Check Lawyer
        // =========================================================================
        [HttpPost]
        public JsonResult CheckLawyer(string searchKey)
        {
            var lawyer = db.GraduateApplications.Include(g => g.ApplicationStatus)
                .FirstOrDefault(g => g.MembershipId == searchKey || g.ArabicName.Contains(searchKey));
            if (lawyer == null) return Json(new { success = false });
            return Json(new { success = true, lawyerId = lawyer.Id, lawyerName = lawyer.ArabicName, isPracticing = LawyerStatusHelper.IsActiveLawyer(lawyer), lawyerBankName = lawyer.BankName, lawyerBankBranch = lawyer.BankBranch });
        }

        // =========================================================================
        // AJAX: Get Contractor Stamps
        // =========================================================================
        [HttpGet]
        public JsonResult GetContractorStamps(int contractorId)
        {
            FixStampsForContractor(contractorId);

            var data = db.Stamps
                .Where(s => s.ContractorId == contractorId && s.Status == "مع المتعهد" && !db.StampSales.Any(ss => ss.StampId == s.Id))
                .OrderBy(s => s.SerialNumber)
                .Select(s => new { s.SerialNumber, s.Value })
                .Take(500)
                .ToList();
            return Json(data, JsonRequestBehavior.AllowGet);
        }


        // =========================================================================
        // تقرير حركة مبيعات الطوابع (Report)
        // =========================================================================
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult SalesReport(DateTime? fromDate, DateTime? toDate, int? contractorId)
        {
            // إعداد القيم الافتراضية للتواريخ (بداية الشهر الحالي)
            if (!fromDate.HasValue) fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!toDate.HasValue) toDate = DateTime.Now; // نهاية اليوم الحالي ضمناً في الاستعلام

            var query = db.StampSales.Include(s => s.Contractor).AsQueryable();

            // تطبيق الفلاتر
            // نضيف يوم واحد لتاريخ النهاية ليشمل اليوم بالكامل (لأن الوقت 00:00:00)
            var actualToDate = toDate.Value.Date.AddDays(1);

            query = query.Where(s => s.SaleDate >= fromDate.Value && s.SaleDate < actualToDate);

            if (contractorId.HasValue)
            {
                query = query.Where(s => s.ContractorId == contractorId.Value);
            }

            // إذا كان المستخدم الحالي "متعهد"، نفرض عليه رؤية مبيعاته فقط
            bool isContractorUser = Session["UserType"] != null &&
                                   (Session["UserType"].ToString() == "Contractor" || Session["UserType"].ToString() == "Advocate");

            if (isContractorUser && Session["ContractorId"] != null)
            {
                int myId = (int)Session["ContractorId"];
                query = query.Where(s => s.ContractorId == myId);
                contractorId = myId; // لتثبيت القيمة في الـ View
            }

            // تنفيذ الاستعلام
            var salesData = query.OrderByDescending(s => s.SaleDate)
                .Select(s => new StampSaleItemDto
                {
                    Id = s.Id,
                    SaleDate = s.SaleDate,
                    ContractorName = s.Contractor.Name,
                    LawyerName = s.LawyerName,
                    LawyerMembershipId = s.LawyerMembershipId,
                    SerialNumber = s.Stamp.SerialNumber,
                    Value = s.StampValue,
                    LawyerShare = s.AmountToLawyer,
                    BarShare = s.AmountToBar,
                    RecordedBy = s.RecordedByUserName
                }).ToList();

            // تجهيز الـ ViewModel
            var model = new StampSalesReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                ContractorId = contractorId,
                Sales = salesData,
                TotalQuantity = salesData.Count,
                TotalValue = salesData.Sum(s => s.Value),
                TotalLawyerShare = salesData.Sum(s => s.LawyerShare),
                TotalBarShare = salesData.Sum(s => s.BarShare)
            };

            // قائمة المتعهدين للفلترة (فقط للمشرفين)
            if (!isContractorUser)
            {
                ViewBag.ContractorsList = new SelectList(db.StampContractors.Where(c => c.IsActive), "Id", "Name", contractorId);
            }

            return View(model);
        }


        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}