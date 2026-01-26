using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BarManegment.Models;
using BarManegment.Helpers;
using BarManegment.Services;

namespace BarManegment.Areas.Admin.Controllers
{
    [Authorize]
    [CustomAuthorize(Permission = "CanView")] // الصلاحية العامة للمتحكم
    public class PayrollController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. عرض سجلات الرواتب الشهرية
        // ============================================================
        public ActionResult Index()
        {
            var payrolls = db.MonthlyPayrolls
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Month)
                .ToList();
            return View(payrolls);
        }

        // ============================================================
        // 2. صفحة إنشاء مسير جديد
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            ViewBag.Month = DateTime.Now.Month;
            ViewBag.Year = DateTime.Now.Year;
            return View();
        }

        // ============================================================
        // 3. معالجة وإنشاء الرواتب (The Engine)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Generate(int month, int year, string notes)
        {
            // أ) التحقق من عدم التكرار
            if (db.MonthlyPayrolls.Any(p => p.Month == month && p.Year == year))
            {
                TempData["ErrorMessage"] = $"عفواً، تم إصدار رواتب شهر {month}/{year} مسبقاً.";
                return RedirectToAction("Index");
            }

            // ب) جلب الموظفين النشطين
            var activeEmployees = db.Employees
                                    .Include(e => e.Department)
                                    .Where(e => e.IsActive)
                                    .ToList();

            if (!activeEmployees.Any())
            {
                TempData["ErrorMessage"] = "لا يوجد موظفين نشطين لإصدار الرواتب لهم.";
                return RedirectToAction("Index");
            }

            // ج) إنشاء رأس المسير
            var payroll = new MonthlyPayroll
            {
                Month = month,
                Year = year,
                Notes = notes,
                CreatedBy = Session["FullName"]?.ToString() ?? "System",
                IssueDate = DateTime.Now,
                PayrollSlips = new List<PayrollSlip>()
            };

            decimal totalGross = 0;
            decimal totalNet = 0;

            // د) حساب الرواتب
            foreach (var emp in activeEmployees)
            {
                // 1. تجميع العلاوات الثابتة
                decimal allowances = emp.ManagerAllowance +
                                     emp.HeadOfDeptAllowance +
                                     emp.MasterDegreeAllowance +
                                     emp.PhdDegreeAllowance +
                                     emp.SpecializationAllowance;

                // 2. قراءة الزيادة السنوية (المعتمدة مسبقاً في ملف الموظف)
                // ✅ تعديل هام: لا نقوم بحساب الزيادة هنا لتجنب الأخطاء التراكمية.
                // يتم الاعتماد على القيمة المخزنة في سجل الموظف والتي يتم تحديثها سنوياً بإجراء منفصل.
                decimal annualIncrement = emp.AnnualIncrementAmount ?? 0;

                // 3. إنشاء القسيمة
                var slip = new PayrollSlip
                {
                    EmployeeId = emp.Id,
                    BasicSalary = emp.BasicSalary,
                    AllowancesTotal = allowances,
                    AnnualIncrementAmount = annualIncrement,
                    TransportAllowance = emp.TransportAllowance,

                    // الاستقطاعات
                    EmployeePensionDeduction = emp.PensionAmountEmployee,
                    OtherDeductions = emp.OtherMonthlyDeduction,

                    // الإجماليات (يفترض أن TotalSalary في المودل يجمع كل ما سبق)
                    // Gross = Basic + Allowances + Increment + Transport
                    GrossSalary = emp.TotalSalary,
                    NetSalary = emp.NetSalary,

                    // بيانات البنك للحفظ التاريخي
                    BankName = emp.BankName,
                    BankAccountNumber = emp.BankAccountNumber
                };

                totalGross += slip.GrossSalary;
                totalNet += slip.NetSalary;

                payroll.PayrollSlips.Add(slip);
            }

            payroll.TotalGrossAmount = totalGross;
            payroll.TotalNetAmount = totalNet;

            db.MonthlyPayrolls.Add(payroll);
            db.SaveChanges();

            AuditService.LogAction("Generate Payroll", "Payroll", $"Generated Payroll for {month}/{year} - Total Employees: {activeEmployees.Count}");
            TempData["SuccessMessage"] = $"تم إصدار مسير رواتب شهر {month}/{year} بنجاح.";

            return RedirectToAction("Details", new { id = payroll.Id });
        }

        // ============================================================
        // 4. عرض التفاصيل
        // ============================================================
        public ActionResult Details(int id)
        {
            var payroll = db.MonthlyPayrolls.Include(p => p.PayrollSlips.Select(s => s.Employee)).FirstOrDefault(p => p.Id == id);
            if (payroll == null) return HttpNotFound();
            return View(payroll);
        }

        // ============================================================
        // 5. الترحيل للمالية (Post to GL) - النسخة الديناميكية
        // ============================================================
        [HttpPost]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult PostToJournal(int id)
        {
            var payroll = db.MonthlyPayrolls.Include(p => p.PayrollSlips).FirstOrDefault(p => p.Id == id);
            if (payroll == null) return HttpNotFound();

            if (payroll.IsPostedToJournal)
            {
                TempData["ErrorMessage"] = "تم ترحيل هذا المسير مسبقاً.";
                return RedirectToAction("Details", new { id = id });
            }

            // ✅ التصحيح هنا: استخدام Name أو StartDate.Year بدلاً من y.Year
            // بما أن payroll.Year رقم، نقوم بتحويله لنص لمقارنته مع Name
            string payrollYearString = payroll.Year.ToString();

            var fiscalYear = db.FiscalYears
                .FirstOrDefault(y => (y.Name == payrollYearString || y.StartDate.Year == payroll.Year) && !y.IsClosed);

            // إذا لم نجد سنة مطابقة، نبحث عن السنة الحالية المفتوحة
            if (fiscalYear == null)
            {
                fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
            }

            if (fiscalYear == null)
            {
                TempData["ErrorMessage"] = $"لا توجد سنة مالية مفتوحة لعام {payroll.Year}. يرجى فتح السنة المالية من الإعدادات أولاً.";
                return RedirectToAction("Details", new { id = id });
            }

            try
            {
                int expenseAccId = GetAccountIdFromSettings("Payroll_SalariesExpenseAccount", "5101");
                int bankAccId = GetAccountIdFromSettings("Default_Bank_Payment_Account", "1102");
                int liabilityAccId = GetAccountIdFromSettings("Payroll_PensionLiabilityAccount", "2103");

                var journalEntry = new JournalEntry
                {
                    FiscalYearId = fiscalYear.Id,
                    EntryDate = DateTime.Now,
                    Description = $"استحقاق رواتب شهر {payroll.Month}/{payroll.Year}",
                    ReferenceNumber = $"PAY-{payroll.Year}-{payroll.Month}",
                    IsPosted = true,
                    CreatedBy = Session["FullName"]?.ToString() ?? "System",
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                // المدين
                journalEntry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = expenseAccId,
                    Debit = payroll.TotalGrossAmount,
                    Credit = 0,
                    Description = "إجمالي الرواتب والأجور"
                });

                // الدائن 1 (الالتزامات)
                decimal totalDeductions = payroll.PayrollSlips.Sum(s => s.EmployeePensionDeduction + s.OtherDeductions);
                if (totalDeductions > 0)
                {
                    journalEntry.JournalEntryDetails.Add(new JournalEntryDetail
                    {
                        AccountId = liabilityAccId,
                        Debit = 0,
                        Credit = totalDeductions,
                        Description = "استقطاعات التأمين وخصومات الموظفين"
                    });
                }

                // الدائن 2 (البنك)
                journalEntry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = bankAccId,
                    Debit = 0,
                    Credit = payroll.TotalNetAmount,
                    Description = "صافي الرواتب المحول للبنك"
                });

                db.JournalEntries.Add(journalEntry);
                payroll.IsPostedToJournal = true;
                payroll.JournalEntry = journalEntry;

                db.SaveChanges(); // لحفظ القيد أولاً

                payroll.JournalEntryId = journalEntry.Id;
                db.SaveChanges(); // لحفظ التحديث في المسير

                AuditService.LogAction("Post Payroll", "Payroll", $"Posted Payroll #{id} to Journal #{journalEntry.Id}");
                TempData["SuccessMessage"] = $"تم اعتماد الرواتب وترحيل القيد المحاسبي رقم #{journalEntry.Id} للسنة المالية {fiscalYear.Name}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "فشل الترحيل المالي: " + ex.Message;
            }

            return RedirectToAction("Details", new { id = id });
        }

        // ============================================================
        // 6. حذف المسير (تراجع)
        // ============================================================
        [HttpPost]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int id)
        {
            var payroll = db.MonthlyPayrolls.Include(p => p.PayrollSlips).FirstOrDefault(p => p.Id == id);

            if (payroll == null) return HttpNotFound();

            if (payroll.IsPostedToJournal)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف مسير تم ترحيله مالياً. يجب عكس القيد أولاً.";
                return RedirectToAction("Index");
            }

            try
            {
                // حذف التفاصيل أولاً
                if (payroll.PayrollSlips != null && payroll.PayrollSlips.Any())
                {
                    db.PayrollSlips.RemoveRange(payroll.PayrollSlips);
                }

                db.MonthlyPayrolls.Remove(payroll);
                db.SaveChanges();

                AuditService.LogAction("Delete Payroll", "Payroll", $"Deleted Payroll #{id}");
                TempData["SuccessMessage"] = "تم حذف مسير الرواتب بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء الحذف: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ============================================================
        // 7. الطباعة
        // ============================================================
        public ActionResult Print(int id)
        {
            var payroll = db.MonthlyPayrolls
                            .Include(p => p.PayrollSlips.Select(s => s.Employee.JobTitle))
                            .Include(p => p.PayrollSlips.Select(s => s.Employee.Department))
                            .FirstOrDefault(p => p.Id == id);

            if (payroll == null) return HttpNotFound();

            AuditService.LogAction("Print Payroll", "Payroll", $"Printed Payroll #{id}");
            return View(payroll);
        }

        // ============================================================
        // Helper: دالة جلب الحسابات من الإعدادات
        // ============================================================
        private int GetAccountIdFromSettings(string settingKey, string fallbackCode)
        {
            // 1. محاولة جلب الإعداد من قاعدة البيانات
            var setting = db.SystemSettings.FirstOrDefault(s => s.SettingKey == settingKey);

            if (setting != null && setting.ValueInt.HasValue)
            {
                return setting.ValueInt.Value;
            }

            // 2. Fallback: البحث بالكود القديم إذا لم يتم ضبط الإعداد
            var account = db.Accounts.FirstOrDefault(a => a.Code == fallbackCode)
                          ?? db.Accounts.FirstOrDefault(a => a.Code.StartsWith(fallbackCode));

            if (account == null)
            {
                throw new Exception($"لم يتم العثور على الحساب المطلوب. يرجى ضبط الإعداد '{settingKey}' أو التأكد من وجود حساب بالكود '{fallbackCode}'.");
            }

            return account.Id;
        }


        // ============================================================
        // 8. وظيفة تطبيق العلاوة السنوية (تُشغل مرة واحدة شهرياً أو سنوياً)
        // ============================================================
        [CustomAuthorize(Permission = "CanEdit")] // صلاحية خاصة للمدير المالي
        public ActionResult ManageAnnualIncrements()
        {
            // عرض صفحة تحتوي على زر لتطبيق الزيادات
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ApplyAnnualIncrements(int month, int year)
        {
            try
            {
                // 1. جلب إعدادات النسبة
                var incPercentSetting = db.SystemSettings.Find("AnnualIncrementPercent");
                decimal percentage = decimal.Parse(incPercentSetting?.SettingValue ?? "0");

                if (percentage <= 0)
                {
                    TempData["ErrorMessage"] = "نسبة الزيادة السنوية غير محددة في الإعدادات.";
                    return RedirectToAction("ManageAnnualIncrements");
                }

                // 2. جلب الموظفين النشطين
                var employees = db.Employees.Where(e => e.IsActive).ToList();
                int updatedCount = 0;
                int maxServiceYears = 25; // السقف الزمني

                foreach (var emp in employees)
                {
                    if (!emp.HireDate.HasValue) continue;

                    // 3. حساب سنوات الخدمة
                    var today = new DateTime(year, month, 1);
                    int yearsOfService = today.Year - emp.HireDate.Value.Year;
                    if (today.Month < emp.HireDate.Value.Month) yearsOfService--;

                    // 4. التحقق من الاستحقاق:
                    // - هل هذا هو شهر التعيين؟ (أي أكمل سنة كاملة جديدة)
                    // - هل لم يتجاوز السقف؟
                    if (emp.HireDate.Value.Month == month && yearsOfService > 0 && yearsOfService < maxServiceYears)
                    {
                        // حساب قيمة الزيادة (على الراتب الأساسي)
                        decimal incrementValue = Math.Round(emp.BasicSalary * (percentage / 100), 2);

                        // إضافة الزيادة للرصيد المتراكم
                        if (emp.AnnualIncrementAmount == null) emp.AnnualIncrementAmount = 0;

                        emp.AnnualIncrementAmount += incrementValue;
                        updatedCount++;
                    }
                }

                if (updatedCount > 0)
                {
                    db.SaveChanges();
                    AuditService.LogAction("Apply Increments", "Payroll", $"Applied annual increments for {updatedCount} employees in {month}/{year}.");
                    TempData["SuccessMessage"] = $"تم تطبيق الزيادة السنوية بنجاح على {updatedCount} موظف.";
                }
                else
                {
                    TempData["InfoMessage"] = "لا يوجد موظفين يستحقون الزيادة السنوية في هذا الشهر.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ: " + ex.Message;
            }

            return RedirectToAction("ManageAnnualIncrements");
        }










        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}