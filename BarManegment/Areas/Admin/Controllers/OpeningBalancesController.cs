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
    [CustomAuthorize(Permission = "FinancialSetup")] // أو FinancialReports حسب رغبتك
    public class OpeningBalancesController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // 1. التوجيه الافتراضي
        public ActionResult Index()
        {
            return RedirectToAction("Create");
        }

        // 2. عرض شاشة الإدخال (GET)
        // ✅ هذه الدالة ترسل قائمة الحسابات كما يطلب الـ View
        // 2. عرض شاشة الإدخال (GET)
        public ActionResult Create()
        {
            var currentYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
            if (currentYear == null)
            {
                TempData["ErrorMessage"] = "لا توجد سنة المالية مفتوحة.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.FiscalYearName = currentYear.Name;

            // فحص هل تم إدخال القيد مسبقاً؟
            bool isSavedBefore = db.JournalEntries.Any(j => j.FiscalYearId == currentYear.Id && j.SourceModule == "OpeningBalance");
            ViewBag.IsSaved = isSavedBefore; // سنستخدم هذا في الواجهة

            // جلب الحسابات
            var accounts = db.Accounts
                             .Where(a => a.IsTransactional)
                             .OrderBy(a => a.Code)
                             .AsNoTracking()
                             .ToList();

            // إذا كان محفوظاً مسبقاً، سنعرض القيم المحفوظة بدلاً من الأصفار (اختياري، للتحسين)
            if (isSavedBefore)
            {
                // كود إضافي لجلب القيم الحالية وعرضها (يمكنك تجاهله إذا أردت فقط زر الحذف)
            }

            return View(accounts);
        }

        // 3. حفظ الأرصدة وترحيلها (POST)
        // ✅ تستقبل البيانات من الفورم وتعالجها
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveOpeningBalances(DateTime entryDate, List<OpeningBalanceItemDto> items)
        {
            if (items == null || !items.Any())
            {
                TempData["ErrorMessage"] = "لا توجد بيانات للحفظ.";
                return RedirectToAction("Create");
            }

            // 1. تصفية الحسابات الصفرية (التي لم يتم إدخال قيم لها)
            var activeItems = items.Where(x => x.Debit > 0 || x.Credit > 0).ToList();

            if (!activeItems.Any())
            {
                TempData["ErrorMessage"] = "يجب إدخال قيم لأحد الحسابات على الأقل.";
                return RedirectToAction("Create");
            }

            // 2. التحقق من التوازن
            decimal totalDebit = activeItems.Sum(l => l.Debit);
            decimal totalCredit = activeItems.Sum(l => l.Credit);

            if (totalDebit != totalCredit)
            {
                TempData["ErrorMessage"] = $"القيد غير متوازن! الفرق: {Math.Abs(totalDebit - totalCredit)}";
                return RedirectToAction("Create");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var currentYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                    if (currentYear == null) throw new Exception("السنة المالية مغلقة.");

                    // 3. حذف القيد الافتتاحي القديم إن وجد (لتجنب التكرار عند التعديل)
                    var oldEntry = db.JournalEntries
                        .Include(j => j.JournalEntryDetails)
                        .FirstOrDefault(j => j.FiscalYearId == currentYear.Id && j.SourceModule == "OpeningBalance");

                    if (oldEntry != null)
                    {
                        // تصفير الأرصدة في جدول الحسابات أولاً
                        foreach (var detail in oldEntry.JournalEntryDetails)
                        {
                            var acc = db.Accounts.Find(detail.AccountId);
                            if (acc != null) acc.OpeningBalance = 0;
                        }

                        db.JournalEntryDetails.RemoveRange(oldEntry.JournalEntryDetails);
                        db.JournalEntries.Remove(oldEntry);
                        db.SaveChanges();
                    }

                    // 4. إنشاء القيد الجديد
                    var entry = new JournalEntry
                    {
                        FiscalYearId = currentYear.Id,
                        EntryNumber = "OP-" + currentYear.Name, // رقم مميز
                        EntryDate = entryDate,
                        Description = "القيد الافتتاحي للسنة المالية " + currentYear.Name,
                        SourceModule = "OpeningBalance",
                        IsPosted = true,
                        PostedDate = DateTime.Now,
                        PostedByUserId = (int?)Session["UserId"] ?? 1,
                        TotalDebit = totalDebit,
                        TotalCredit = totalCredit,
                        JournalEntryDetails = new List<JournalEntryDetail>()
                    };

                    // 5. إضافة التفاصيل وتحديث رصيد الحساب
                    foreach (var item in activeItems)
                    {
                        // أ. إضافة سطر القيد
                        entry.JournalEntryDetails.Add(new JournalEntryDetail
                        {
                            AccountId = item.AccountId,
                            Debit = item.Debit,
                            Credit = item.Credit,
                            Description = "رصيد افتتاحي"
                        });

                        // ب. تحديث حقل "OpeningBalance" في جدول Accounts
                        var account = db.Accounts.Find(item.AccountId);
                        if (account != null)
                        {
                            // الرصيد الافتتاحي = المدين - الدائن (بشكل عام)
                            account.OpeningBalance = item.Debit - item.Credit;
                            db.Entry(account).State = EntityState.Modified;
                        }
                    }

                    db.JournalEntries.Add(entry);
                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "تم حفظ وترحيل الأرصدة الافتتاحية بنجاح.";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "خطأ أثناء الحفظ: " + ex.Message;
                    return RedirectToAction("Create");
                }
            }
        }
        // =========================================================
        // حذف القيد الافتتاحي (بشروط صارمة جداً)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteOpeningBalance()
        {
            // 1. التحقق من الصلاحية (فقط المسؤول العام رقم 1)
            // افترضنا أن UserTypeId = 1 هو المسؤول
            int userTypeId = (int?)Session["UserTypeId"] ?? 0;
            if (userTypeId != 1)
            {
                TempData["ErrorMessage"] = "عذراً، عملية حذف القيد الافتتاحي مسموحة للمسؤول العام فقط.";
                return RedirectToAction("Create");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 2. جلب السنة المالية الحالية
                    var currentYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                    if (currentYear == null) throw new Exception("السنة المالية مغلقة أو غير محددة.");

                    // 3. جلب القيد الافتتاحي الموجود
                    var openingEntry = db.JournalEntries
                        .Include(j => j.JournalEntryDetails)
                        .FirstOrDefault(j => j.FiscalYearId == currentYear.Id && j.SourceModule == "OpeningBalance");

                    if (openingEntry == null) throw new Exception("لا يوجد قيد افتتاحي لحذفه.");

                    // 4. (الشرط الأهم) التحقق من وجود بيانات أخرى
                    // هل يوجد أي قيد آخر في النظام لهذه السنة غير القيد الافتتاحي؟
                    bool hasOtherData = db.JournalEntries
                        .Any(j => j.FiscalYearId == currentYear.Id && j.Id != openingEntry.Id);

                    if (hasOtherData)
                    {
                        throw new Exception("تنبيه هام: لا يمكن حذف القيد الافتتاحي لأن هناك قيوداً مالية أو سندات تم إدخالها بالفعل في هذه السنة. يجب حذف جميع الحركات المالية أولاً.");
                    }

                    // 5. تصفير أرصدة الحسابات قبل الحذف
                    foreach (var detail in openingEntry.JournalEntryDetails)
                    {
                        var account = db.Accounts.Find(detail.AccountId);
                        if (account != null)
                        {
                            account.OpeningBalance = 0; // إعادة الرصيد لصفر
                            db.Entry(account).State = EntityState.Modified;
                        }
                    }

                    // 6. الحذف النهائي
                    db.JournalEntryDetails.RemoveRange(openingEntry.JournalEntryDetails);
                    db.JournalEntries.Remove(openingEntry);

                    db.SaveChanges();
                    transaction.Commit();

                    // تسجيل في Audit Log
                    AuditService.LogAction("Delete Opening Balance", "OpeningBalances", $"Deleted Entry #{openingEntry.EntryNumber}");

                    TempData["SuccessMessage"] = "تم حذف القيد الافتتاحي وتصفير الأرصدة بنجاح. يمكنك الآن الإدخال من جديد.";
                    return RedirectToAction("Create");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = ex.Message;
                    return RedirectToAction("Create");
                }
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }

    // =========================================================
    // DTO لاستقبال البيانات من الجدول في الـ View
    // =========================================================
    public class OpeningBalanceItemDto
    {
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}