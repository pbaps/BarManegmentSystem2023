using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services; // ضروري لخدمة التدقيق
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "FinancialReports")] // صلاحية التعامل مع القيود
    public class JournalEntriesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. عرض القيود (Index)
        // ============================================================
        // 1. عرض القيود (Index)
        // ============================================================
        public ActionResult Index(string searchString, string sourceFilter, DateTime? fromDate, DateTime? toDate, int? page)
        {
            var query = db.JournalEntries.AsNoTracking()
                .Include(j => j.FiscalYear) // تضمين السنة المالية
                .AsQueryable();

            // الفلترة بالبحث (رقم القيد، المرجع، البيان)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(j => j.EntryNumber.Contains(searchString) ||
                                         j.ReferenceNumber.Contains(searchString) ||
                                         j.Description.Contains(searchString));
            }

            // الفلترة بمصدر القيد (يدوي، سند قبض، رواتب...)
            if (!string.IsNullOrWhiteSpace(sourceFilter))
            {
                query = query.Where(j => j.SourceModule == sourceFilter);
            }

            // الفلترة بالتاريخ (من)
            if (fromDate.HasValue)
            {
                query = query.Where(j => j.EntryDate >= fromDate.Value);
            }

            // الفلترة بالتاريخ (إلى)
            if (toDate.HasValue)
            {
                var finalToDate = toDate.Value.AddDays(1);
                query = query.Where(j => j.EntryDate < finalToDate);
            }

            // ✅ المكان الصحيح هنا:
            // نقوم بالترتيب النهائي بعد تطبيق كل الفلاتر وقبل إرسالها للـ PagedList
            query = query.OrderByDescending(j => j.EntryDate)
                         .ThenByDescending(j => j.Id);

            int pageSize = 20;
            int pageNumber = (page ?? 1);

            // حفظ فلاتر البحث للواجهة
            ViewBag.CurrentFilter = searchString;
            ViewBag.SourceFilter = sourceFilter;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(query.ToPagedList(pageNumber, pageSize));
        }
        // ============================================================
        // 2. تفاصيل القيد (Details) - يستخدم للعرض والطباعة
        // ============================================================
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var journalEntry = db.JournalEntries.AsNoTracking()
                .Include(j => j.FiscalYear)
                .Include(j => j.JournalEntryDetails.Select(d => d.Account))
                .Include(j => j.JournalEntryDetails.Select(d => d.CostCenter))
                .FirstOrDefault(j => j.Id == id);

            if (journalEntry == null) return HttpNotFound();

            ViewBag.FiscalYearName = journalEntry.FiscalYear?.Name ?? "غير محدد";

            // تحويل المبلغ الإجمالي إلى كلمات (لأغراض الطباعة)
            ViewBag.AmountInWords = TafqeetHelper.ConvertToArabic(journalEntry.TotalDebit, "دينار");

            return View(journalEntry);
        }

        // ============================================================
        // 3. توجيه للطباعة (Print)
        // ============================================================
        public ActionResult Print(int? id)
        {
            // نوجه لنفس دالة التفاصيل، والواجهة ستتكفل بتنسيق الطباعة
            return RedirectToAction("Details", new { id = id });
        }

        // ============================================================
        // 4. إنشاء قيد يدوي (Create - GET)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            // 1. التحقق من وجود سنة مالية مفتوحة وحالية
            var currentYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
            if (currentYear == null)
            {
                TempData["ErrorMessage"] = "لا توجد سنة مالية مفتوحة حالياً. يرجى مراجعة إعدادات السنوات المالية.";
                return RedirectToAction("Index");
            }

            ViewBag.FiscalYearName = currentYear.Name;

            // 2. تعبئة قائمة الحسابات (فقط الحسابات الحركية التي تقبل قيود)
            var accounts = db.Accounts
                .Where(a => a.IsTransactional)
                .OrderBy(a => a.Code)
                .Select(a => new { a.Id, Text = a.Code + " - " + a.Name })
                .ToList();
            ViewBag.Accounts = new SelectList(accounts, "Id", "Text");

            // 3. تعبئة قائمة مراكز التكلفة
            var costCenters = db.CostCenters
                .OrderBy(c => c.Code)
                .Select(c => new { c.Id, Text = c.Name })
                .ToList();
            ViewBag.CostCenters = new SelectList(costCenters, "Id", "Text");

            // إرجاع الموديل بتاريخ اليوم
            return View(new JournalEntryViewModel { EntryDate = DateTime.Now });
        }

        // ============================================================
        // 5. حفظ القيد اليدوي (Save - POST via AJAX)
        // ============================================================
        [HttpPost]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult SaveEntry(JournalEntryViewModel model)
        {
            try
            {
                // أ. التحقق من صحة البيانات الأساسية
                if (model.Lines == null || model.Lines.Count < 2)
                    return Json(new { success = false, message = "يجب إدخال طرفي القيد (مدين ودائن) على الأقل." });

                var totalDebit = model.Lines.Sum(l => l.Debit);
                var totalCredit = model.Lines.Sum(l => l.Credit);

                // ب. التحقق من توازن القيد
                if (Math.Abs(totalDebit - totalCredit) > 0.01m)
                    return Json(new { success = false, message = $"القيد غير متوازن. الفرق: {Math.Abs(totalDebit - totalCredit)}" });

                // ج. التحقق من السنة المالية مرة أخرى (للحماية)
                var currentYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (currentYear == null) return Json(new { success = false, message = "السنة المالية مغلقة." });

                // د. توليد رقم القيد التسلسلي للسنة الحالية
                string nextEntryNo = "1";
                var lastEntry = db.JournalEntries
                    .Where(j => j.FiscalYearId == currentYear.Id)
                    .OrderByDescending(j => j.Id) // الترتيب بالـ ID هو الأدق لآخر إدخال
                    .FirstOrDefault();

                if (lastEntry != null && int.TryParse(lastEntry.EntryNumber, out int lastNo))
                {
                    nextEntryNo = (lastNo + 1).ToString();
                }
                else
                {
                    // في حال كان الجدول فارغاً لهذه السنة
                    nextEntryNo = (db.JournalEntries.Count(j => j.FiscalYearId == currentYear.Id) + 1).ToString();
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    // هـ. إنشاء رأس القيد
                    var entry = new JournalEntry
                    {
                        FiscalYearId = currentYear.Id,
                        EntryNumber = nextEntryNo,
                        EntryDate = model.EntryDate,
                        Description = model.Description,
                        ReferenceNumber = model.ReferenceNumber,
                        SourceModule = "Manual", // المصدر: يدوي
                        IsPosted = true,         // ترحيل فوري للقيود اليدوية
                        PostedDate = DateTime.Now,
                        TotalDebit = totalDebit,
                        TotalCredit = totalCredit,
                        CreatedBy = Session["FullName"]?.ToString() ?? "System",
                        PostedByUserId = (int?)Session["UserId"] ?? 1,
                        JournalEntryDetails = new List<JournalEntryDetail>()
                    };

                    // و. إضافة التفاصيل (الأسطر)
                    foreach (var line in model.Lines)
                    {
                        // تجاهل الأسطر الصفرية تماماً
                        if (line.Debit == 0 && line.Credit == 0) continue;

                        entry.JournalEntryDetails.Add(new JournalEntryDetail
                        {
                            AccountId = line.AccountId,
                            CostCenterId = line.CostCenterId,
                            Debit = line.Debit,
                            Credit = line.Credit,
                            Description = line.LineDescription ?? model.Description
                        });
                    }

                    db.JournalEntries.Add(entry);
                    db.SaveChanges();
                    transaction.Commit();

                    // ز. تسجيل العملية في سجل التدقيق
                    AuditService.LogAction("Create Journal Entry", "JournalEntries", $"Created Entry #{entry.EntryNumber} Amount: {totalDebit}");

                    return Json(new { success = true, message = "تم حفظ القيد وترحيله بنجاح." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في النظام: " + ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}