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
    [CustomAuthorize(Permission = "CanView")] // تأكد من إضافة هذا الإذن لاحقاً
    public class PayrollController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // 1. عرض سجلات الرواتب الشهرية
        public ActionResult Index()
        {
            var payrolls = db.MonthlyPayrolls.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToList();
            return View(payrolls);
        }

        // 2. صفحة إنشاء مسير جديد (اختيار الشهر والسنة)
        public ActionResult Create()
        {
            ViewBag.Month = DateTime.Now.Month;
            ViewBag.Year = DateTime.Now.Year;
            return View();
        }

        // 3. معالجة وإنشاء الرواتب (The Engine)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Generate(int month, int year, string notes)
        {
            // أ) التحقق هل تم إصدار الرواتب لهذا الشهر سابقاً؟
            if (db.MonthlyPayrolls.Any(p => p.Month == month && p.Year == year))
            {
                TempData["ErrorMessage"] = $"عفواً، تم إصدار رواتب شهر {month}/{year} مسبقاً.";
                return RedirectToAction("Index");
            }

            // ب) جلب الموظفين النشطين فقط
            var activeEmployees = db.Employees
                                    .Include(e => e.Department)
                                    .Where(e => e.IsActive)
                                    .ToList();

            if (!activeEmployees.Any())
            {
                TempData["ErrorMessage"] = "لا يوجد موظفين نشطين لإصدار الرواتب لهم.";
                return RedirectToAction("Index");
            }

            // ج) إنشاء رأس المسير (Master)
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

            // د) الدوران على كل موظف وحساب راتبه
            foreach (var emp in activeEmployees)
            {
                // 1. تجميع العلاوات (ما عدا المواصلات والزيادة السنوية لأنها مفصلة)
                decimal allowances = emp.ManagerAllowance +
                                     emp.HeadOfDeptAllowance +
                                     emp.MasterDegreeAllowance +
                                     emp.PhdDegreeAllowance +
                                     emp.SpecializationAllowance;

                // 2. إنشاء القسيمة
                var slip = new PayrollSlip
                {
                    EmployeeId = emp.Id,
                    BasicSalary = emp.BasicSalary,
                    AllowancesTotal = allowances,
                    AnnualIncrementAmount = emp.CalculatedAnnualIncrementAmount, // الزيادة السنوية
                    TransportAllowance = emp.TransportAllowance,

                    // الاستقطاعات
                    EmployeePensionDeduction = emp.PensionAmountEmployee, // 7%
                    OtherDeductions = emp.OtherMonthlyDeduction,

                    // الإجماليات
                    GrossSalary = emp.TotalSalary,
                    NetSalary = emp.NetSalary,

                    // بيانات البنك
                    BankName = emp.BankName,
                    BankAccountNumber = emp.BankAccountNumber
                };

                // إضافة للأرقام الكلية للمسير
                totalGross += slip.GrossSalary;
                totalNet += slip.NetSalary;

                payroll.PayrollSlips.Add(slip);
            }

            payroll.TotalGrossAmount = totalGross;
            payroll.TotalNetAmount = totalNet;

            // هـ) الحفظ في قاعدة البيانات
            db.MonthlyPayrolls.Add(payroll);
            db.SaveChanges();

            AuditService.LogAction("Generate Payroll", "Payroll", $"Generated for {month}/{year} - Count: {activeEmployees.Count}");
            TempData["SuccessMessage"] = $"تم إصدار مسير رواتب شهر {month}/{year} بنجاح لـ {activeEmployees.Count} موظف.";

            return RedirectToAction("Details", new { id = payroll.Id });
        }

        // 4. عرض تفاصيل المسير (قائمة الموظفين)
        public ActionResult Details(int id)
        {
            var payroll = db.MonthlyPayrolls.Include(p => p.PayrollSlips.Select(s => s.Employee)).FirstOrDefault(p => p.Id == id);
            if (payroll == null) return HttpNotFound();
            return View(payroll);
        }

        // 5. الترحيل للمالية (Post to GL) - سنبرمجه لاحقاً بالتفصيل
        [HttpPost]
        [CustomAuthorize(Permission = "CanAdd")] // صلاحية إضافة قيد
        public ActionResult PostToJournal(int id)
        {
            var payroll = db.MonthlyPayrolls.Include(p => p.PayrollSlips).FirstOrDefault(p => p.Id == id);
            if (payroll == null) return HttpNotFound();

            if (payroll.IsPostedToJournal)
            {
                TempData["ErrorMessage"] = "تم ترحيل هذا المسير مسبقاً.";
                return RedirectToAction("Details", new { id = id });
            }

            // 1. البحث عن الحسابات في الدليل المحاسبي (بناءً على Seed Data)
            // مصروف الرواتب: 5101
            // النقدية بالبنوك: 1102 (أو حساب وسيط: رواتب مستحقة)
            // التزامات أخرى (تأمين): سنحتاج لحساب التزام، سنفترضه 2103 مؤقتاً أو ننشئه

            var salaryExpenseAccount = db.Accounts.FirstOrDefault(a => a.Code == "5101"); // مصروف الرواتب
            var bankAccount = db.Accounts.FirstOrDefault(a => a.Code == "1102"); // البنك (الدفع)

            // إذا لم توجد الحسابات، نوقف العملية
            if (salaryExpenseAccount == null || bankAccount == null)
            {
                TempData["ErrorMessage"] = "عفواً، حساب 'مصروف الرواتب' أو 'البنك' غير معرف في دليل الحسابات. يرجى التأكد من التكويد (5101, 1102).";
                return RedirectToAction("Details", new { id = id });
            }

            // 2. إنشاء القيد الرئيسي (Journal Entry Master)
            var journalEntry = new JournalEntry
            {
                EntryDate = DateTime.Now,
                Description = $"استحقاق رواتب شهر {payroll.Month}/{payroll.Year}",
                ReferenceNumber = $"PAY-{payroll.Year}-{payroll.Month}",
                IsPosted = true, // ترحيل مباشر
                CreatedBy = Session["FullName"]?.ToString() ?? "System",
                JournalEntryDetails = new List<JournalEntryDetail>()
            };

            // 3. الطرف المدين (Dr): مصروف الرواتب (بالمبلغ الإجمالي Gross)
            journalEntry.JournalEntryDetails.Add(new JournalEntryDetail
            {
                AccountId = salaryExpenseAccount.Id,
                Debit = payroll.TotalGrossAmount,
                Credit = 0,
                Description = "إجمالي الرواتب والأجور"
            });

            // 4. الطرف الدائن (Cr): 
            // أ) هيئة التأمين والمعاشات (حصة الموظف المستقطعة)
            decimal totalPension = payroll.PayrollSlips.Sum(s => s.EmployeePensionDeduction);

            // ملاحظة: يفضل أن يكون هناك حساب خاص بالالتزامات (21xx)، هنا سنضعه في حساب دائن عام للتوضيح
            // إذا لم يوجد حساب تأمين، سنضيفه للبنك مؤقتاً أو ننشئ حساباً وهمياً للالتزام، 
            // لكن الأصح محاسبياً فصله. سنفترض وجود حساب التزامات (دائنون)
            var liabilityAccount = db.Accounts.FirstOrDefault(a => a.Code.StartsWith("21")) ?? bankAccount;

            if (totalPension > 0)
            {
                journalEntry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = liabilityAccount.Id, // حساب هيئة التأمين
                    Debit = 0,
                    Credit = totalPension,
                    Description = "استقطاعات التأمين والمعاشات (حصة الموظف)"
                });
            }

            // ب) خصومات أخرى
            decimal totalOtherDeductions = payroll.PayrollSlips.Sum(s => s.OtherDeductions);
            if (totalOtherDeductions > 0)
            {
                // عادة تخصم من سلف الموظفين (أصل) أو إيرادات أخرى
                // سنضيفها لنفس حساب الالتزام للتبسيط الآن
                journalEntry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = liabilityAccount.Id,
                    Debit = 0,
                    Credit = totalOtherDeductions,
                    Description = "خصومات أخرى"
                });
            }

            // ج) صافي الرواتب (يصرف من البنك أو يسجل كرواتب مستحقة)
            journalEntry.JournalEntryDetails.Add(new JournalEntryDetail
            {
                AccountId = bankAccount.Id,
                Debit = 0,
                Credit = payroll.TotalNetAmount,
                Description = "صافي الرواتب المحول للبنك"
            });

            // 5. حفظ القيد وربطه بالمسير
            db.JournalEntries.Add(journalEntry);

            payroll.IsPostedToJournal = true;
            payroll.JournalEntry = journalEntry; // الربط المباشر إذا كانت العلاقة موجودة، أو عبر ID بعد الحفظ

            db.SaveChanges(); // الحفظ لتوليد ID للقيد

            payroll.JournalEntryId = journalEntry.Id;
            db.SaveChanges(); // تحديث المسير

            AuditService.LogAction("Post Payroll", "Payroll", $"Posted Payroll {payroll.Id} to Journal #{journalEntry.Id}");
            TempData["SuccessMessage"] = $"تم اعتماد الرواتب وترحيل القيد المحاسبي رقم #{journalEntry.Id} بنجاح.";

            return RedirectToAction("Details", new { id = id });
        }

        // 6. حذف المسير (تراجع) - مسموح فقط إذا لم يتم الترحيل
        [HttpPost]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int id)
        {
            var payroll = db.MonthlyPayrolls.Include(p => p.PayrollSlips).FirstOrDefault(p => p.Id == id);

            if (payroll == null) return HttpNotFound();

            // التحقق من الترحيل (لا يحذف إذا كان مرحلاً)
            if (payroll.IsPostedToJournal)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف مسير تم ترحيله مالياً. يجب عكس القيد أولاً.";
                return RedirectToAction("Index");
            }

            try
            {
                // 1. حذف التفاصيل (القسائم) أولاً
                if (payroll.PayrollSlips != null && payroll.PayrollSlips.Any())
                {
                    db.PayrollSlips.RemoveRange(payroll.PayrollSlips);
                }

                // 2. حذف الرأس (المسير)
                db.MonthlyPayrolls.Remove(payroll);

                db.SaveChanges();

                TempData["SuccessMessage"] = "تم حذف مسير الرواتب وجميع القسائم المرتبطة به بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء الحذف: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // أمر طباعة كشف الرواتب الشهري
        public ActionResult Print(int id)
        {
            var payroll = db.MonthlyPayrolls
                            .Include(p => p.PayrollSlips.Select(s => s.Employee.JobTitle))
                            .Include(p => p.PayrollSlips.Select(s => s.Employee.Department))
                            .FirstOrDefault(p => p.Id == id);

            if (payroll == null) return HttpNotFound();

            return View(payroll);
        }
    }
}