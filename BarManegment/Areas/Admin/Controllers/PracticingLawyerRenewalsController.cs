using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Models;
using BarManegment.Helpers;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Net;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class PracticingLawyerRenewalsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. الترسيت السنوي (Annual Reset) - 🔒 خاص بالمسؤول العام
        // ============================================================
        // ============================================================
        // 1. الترسيت السنوي (Annual Reset) - 🔒 خاص بالمسؤول العام
        // ============================================================
        // ✅ السماح للمدير العام فقط، مع تخطي فحص الصلاحيات الروتيني لهذه الدالة الحساسة
        public ActionResult AnnualStatusReset()
        {
            // 1. التحقق من أن المستخدم هو "مدير عام" فعلاً
            // نفترض أن Administrator هو الـ Role Name بالإنجليزية
            // يفضل استخدام دالة مساعدة، لكن للسرعة سنفحص الاثنين (عربي وانجليزي)
            var userType = Session["UserType"]?.ToString();

            if (userType != "Administrator" && userType != "مسؤول عام")
            {
                TempData["ErrorMessage"] = "عذراً، هذا الإجراء مخصص للمسؤول العام (مدير النظام) فقط.";
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            var practicingStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول");

            // تهيئة القيمة لتجنب الخطأ في العرض
            ViewBag.PracticingCount = 0;

            if (practicingStatus != null)
            {
                ViewBag.PracticingCount = db.GraduateApplications.Count(g => g.ApplicationStatusId == practicingStatus.Id);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PerformReset()
        {
            // 🔒 التحقق الأمني الصارم قبل التنفيذ
            if (Session["UserType"]?.ToString() != "Administrator")
            {
                AuditService.LogAction("Unauthorized Access", "PracticingLawyerRenewals", $"User {Session["UserId"]} tried to perform Annual Reset.");
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Access Denied: Admins Only.");
            }

            var practicingStatusId = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول")?.Id;

            var pendingRenewalStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "بانتظار تجديد المزاولة");
            if (pendingRenewalStatus == null)
            {
                pendingRenewalStatus = new ApplicationStatus { Name = "بانتظار تجديد المزاولة" };
                db.ApplicationStatuses.Add(pendingRenewalStatus);
                db.SaveChanges();
            }

            if (!practicingStatusId.HasValue)
            {
                TempData["ErrorMessage"] = "خطأ: حالة 'محامي مزاول' غير معرفة في النظام.";
                return RedirectToAction("AnnualStatusReset");
            }

            int rowsAffected = 0;

            try
            {
                using (var updateContext = new ApplicationDbContext())
                {
                    updateContext.Configuration.AutoDetectChangesEnabled = false;

                    var lawyersToUpdate = updateContext.GraduateApplications
                        .Where(g => g.ApplicationStatusId == practicingStatusId.Value)
                        .ToList();

                    if (lawyersToUpdate.Any())
                    {
                        foreach (var lawyer in lawyersToUpdate)
                        {
                            lawyer.ApplicationStatusId = pendingRenewalStatus.Id;
                            updateContext.Entry(lawyer).State = EntityState.Modified;
                        }

                        updateContext.SaveChanges();
                        rowsAffected = lawyersToUpdate.Count;
                    }
                }

                var resetDateSetting = db.SystemSettings.Find("LastAnnualResetDate");
                if (resetDateSetting == null)
                    db.SystemSettings.Add(new SystemSetting { SettingKey = "LastAnnualResetDate", SettingValue = DateTime.Now.ToString("yyyy-MM-dd") });
                else
                    resetDateSetting.SettingValue = DateTime.Now.ToString("yyyy-MM-dd");

                db.SaveChanges();

                AuditService.LogAction("Annual Reset", "PracticingLawyerRenewals", $"Reset status for {rowsAffected} lawyers to 'Pending Renewal' by Administrator.");

                TempData["SuccessMessage"] = $"تمت العملية بنجاح. تم تحويل {rowsAffected} محامي إلى حالة 'بانتظار تجديد المزاولة'.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء المعالجة: " + ex.Message;
            }

            return RedirectToAction("AnnualStatusReset");
        }

        // ============================================================
        // 2. اختيار المحامي للتجديد (Index/Search)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult SelectLawyer(string searchTerm = null)
        {
            var lawyerStatusNames = new List<string> {
                "محامي مزاول", "Advocate", "محامي غير مزاول",
                "محامي موقوف", "محامي موظف", "محامي متقاعد", "بانتظار تجديد المزاولة"
            };

            var lawyerStatusIds = db.ApplicationStatuses
                .Where(s => lawyerStatusNames.Contains(s.Name))
                .Select(s => s.Id)
                .ToList();

            if (!lawyerStatusIds.Any())
            {
                TempData["ErrorMessage"] = "خطأ: لم يتم العثور على حالات المحامين في النظام.";
                return View(new List<GraduateApplication>());
            }

            var query = db.GraduateApplications.AsNoTracking()
                        .Include(g => g.ApplicationStatus)
                        .Where(g => lawyerStatusIds.Contains(g.ApplicationStatusId));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a => a.ArabicName.Contains(searchTerm) ||
                                         a.NationalIdNumber.Contains(searchTerm) ||
                                         a.MembershipId.Contains(searchTerm));
            }

            var lawyers = query.OrderBy(a => a.MembershipId).Take(50).ToList();
            ViewBag.SearchTerm = searchTerm;

            return View(lawyers);
        }

        // ============================================================
        // 3. إنشاء قسيمة التجديد (Create)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var lawyer = db.GraduateApplications.AsNoTracking().FirstOrDefault(g => g.Id == id);
            if (lawyer == null) return HttpNotFound();

            var allActiveFees = db.FeeTypes
                .Include(f => f.Currency)
                .Include(f => f.BankAccount)
                .Where(f => f.IsActive &&
                            (f.Name.Contains("مزاول") ||
                             f.Name.Contains("تقاعد") ||
                             f.Name.Contains("تعاون") ||
                             f.Name.Contains("زمالة") ||
                             f.Name.Contains("غرامة") ||
                             f.Name.Contains("بطاقة المزاولة")) &&
                            !f.Name.Contains("متدرب"))
                .ToList();

            if (!allActiveFees.Any())
            {
                TempData["ErrorMessage"] = "خطأ: لم يتم تعريف رسوم تجديد للمحامين في النظام.";
                return RedirectToAction("SelectLawyer");
            }

            int age = 0;
            if (lawyer.BirthDate != DateTime.MinValue)
            {
                age = DateTime.Now.Year - lawyer.BirthDate.Year;
                if (lawyer.BirthDate.Date > DateTime.Now.Date.AddYears(-age)) age--;
            }

            decimal calculatedRetirementFee = 0;
            if (age <= 30) calculatedRetirementFee = 50;
            else if (age <= 40) calculatedRetirementFee = 100;
            else if (age <= 50) calculatedRetirementFee = 150;
            else if (age <= 60) calculatedRetirementFee = 200;
            else calculatedRetirementFee = 250;

            var viewModel = new CreatePracticingRenewalViewModel
            {
                LawyerId = lawyer.Id,
                LawyerName = lawyer.ArabicName,
                LawyerMembershipId = lawyer.MembershipId,
                RenewalYear = DateTime.Now.Year,
                ExpiryDate = DateTime.Now.AddDays(7),
                AvailableFees = new List<FeeSelectionViewModel>()
            };

            foreach (var fee in allActiveFees)
            {
                var feeVM = new FeeSelectionViewModel
                {
                    FeeTypeId = fee.Id,
                    FeeTypeName = fee.Name,
                    Amount = fee.DefaultAmount,
                    IsSelected = false,
                    CurrencySymbol = fee.Currency?.Symbol ?? "N/A",
                    BankName = fee.BankAccount?.BankName ?? "N/A",
                    AccountNumber = fee.BankAccount?.AccountNumber ?? "N/A",
                    Iban = fee.BankAccount?.Iban ?? "N/A"
                };

                if (fee.Name.Contains("رسوم تقاعد"))
                {
                    feeVM.Amount = calculatedRetirementFee;
                    feeVM.FeeTypeName = $"{fee.Name} (فئة عمرية: {age} سنة)";
                    feeVM.IsSelected = true;
                }
                else if (fee.Name.Contains("تجديد مزاولة") ||
                         fee.Name.Contains("صندوق التعاون") ||
                         fee.Name.Contains("الزمالة") ||
                         fee.Name.Contains("بطاقة المزاولة"))
                {
                    feeVM.IsSelected = true;
                }

                viewModel.AvailableFees.Add(feeVM);
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(CreatePracticingRenewalViewModel viewModel)
        {
            var lawyer = db.GraduateApplications.Find(viewModel.LawyerId);
            if (lawyer == null) return HttpNotFound();

            var selectedFees = viewModel.AvailableFees?.Where(f => f.IsSelected).ToList() ?? new List<FeeSelectionViewModel>();

            if (!selectedFees.Any()) ModelState.AddModelError("AvailableFees", "يجب اختيار رسم واحد على الأقل.");

            bool alreadyRenewed = db.PracticingLawyerRenewals.Any(r => r.GraduateApplicationId == viewModel.LawyerId && r.RenewalYear == viewModel.RenewalYear);
            if (alreadyRenewed) ModelState.AddModelError("RenewalYear", $"تم التجديد لسنة {viewModel.RenewalYear} مسبقاً.");

            if (ModelState.IsValid)
            {
                string feeDescription = $"تجديد مزاولة {viewModel.RenewalYear} - {lawyer.ArabicName}";

                // ✅ استدعاء الدالة من BaseController (تم إزالة النسخة المحلية)
                var voucher = CreateBatchPaymentVoucher(lawyer.Id, selectedFees, feeDescription, viewModel.ExpiryDate);

                if (voucher != null)
                {
                    db.PaymentVouchers.Add(voucher);

                    var renewalRecord = new PracticingLawyerRenewal
                    {
                        GraduateApplicationId = lawyer.Id,
                        RenewalYear = viewModel.RenewalYear,
                        RenewalDate = DateTime.Now,
                        PaymentVoucherId = voucher.Id,
                        IsActive = false
                    };
                    db.PracticingLawyerRenewals.Add(renewalRecord);

                    db.SaveChanges();

                    AuditService.LogAction("Create Renewal Voucher", "PracticingLawyerRenewals", $"Created voucher #{voucher.Id} for Lawyer {lawyer.ArabicName} (Year: {viewModel.RenewalYear}).");

                    TempData["SuccessMessage"] = "تم إصدار قسيمة التجديد بنجاح.";
                    return RedirectToAction("PrintVoucher", "PaymentVouchers", new { id = voucher.Id, area = "Admin" });
                }
                else
                {
                    ModelState.AddModelError("", "فشل إنشاء القسيمة. يرجى مراجعة إعدادات الرسوم.");
                }
            }
            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}