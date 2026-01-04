using BarManegment.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using PagedList; // ✅ ضروري جداً
using BarManegment.Helpers;
using BarManegment.Areas.Admin.ViewModels;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class JournalEntriesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. عرض القيود (Index) - تم التصحيح ليدعم PagedList
        // ============================================================
        public ActionResult Index(string searchString, DateTime? fromDate, DateTime? toDate, int? page)
        {
            var query = db.JournalEntries.AsNoTracking()
                // .Include(j => j.CreatedByUser) // تفعيل إذا كانت العلاقة موجودة
                .AsQueryable();

            // البحث برقم القيد أو الوصف
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(j => j.EntryNumber.Contains(searchString) ||
                                         j.ReferenceNumber.Contains(searchString) ||
                                         j.Description.Contains(searchString));
            }

            // الفلترة بالتاريخ
            if (fromDate.HasValue)
            {
                query = query.Where(j => j.EntryDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var finalToDate = toDate.Value.AddDays(1);
                query = query.Where(j => j.EntryDate < finalToDate);
            }

            // الترتيب (ضروري جداً قبل الترقيم)
            query = query.OrderByDescending(j => j.EntryDate).ThenByDescending(j => j.Id);

            int pageSize = 20;
            int pageNumber = (page ?? 1);

            ViewBag.CurrentFilter = searchString;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            // ✅ الحل هنا: استخدام ToPagedList بدلاً من ToList
            return View(query.ToPagedList(pageNumber, pageSize));
        }

        // ============================================================
        // 2. تفاصيل القيد (Details)
        // ============================================================
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var journalEntry = db.JournalEntries.AsNoTracking()
                // .Include(j => j.CreatedByUser)
                .Include(j => j.JournalEntryDetails.Select(d => d.Account))
                .FirstOrDefault(j => j.Id == id);

            if (journalEntry == null) return HttpNotFound();

            // جلب اسم السنة المالية يدوياً
            var fiscalYearName = "غير محدد";
            if (journalEntry.FiscalYearId.HasValue)
            {
                fiscalYearName = db.FiscalYears
                    .Where(y => y.Id == journalEntry.FiscalYearId)
                    .Select(y => y.Name)
                    .FirstOrDefault() ?? "غير معروف";
            }

            ViewBag.FiscalYearName = fiscalYearName;

            return View(journalEntry);
        }

        // ============================================================
        // 3. طباعة القيد (Print)
        // ============================================================
        public ActionResult Print(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var journalEntry = db.JournalEntries.AsNoTracking()
                // .Include(j => j.CreatedByUser)
                .Include(j => j.JournalEntryDetails.Select(d => d.Account))
                .FirstOrDefault(j => j.Id == id);

            if (journalEntry == null) return HttpNotFound();

            var fiscalYearName = "غير محدد";
            if (journalEntry.FiscalYearId.HasValue)
            {
                fiscalYearName = db.FiscalYears.Where(y => y.Id == journalEntry.FiscalYearId).Select(y => y.Name).FirstOrDefault();
            }
            ViewBag.FiscalYearName = fiscalYearName;

            return View(journalEntry);
        }

        // 4. إنشاء قيد يدوي (Create - GET)
        public ActionResult Create()
        {
            // التحقق من السنة المالية
            var currentYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
            if (currentYear == null)
            {
                TempData["ErrorMessage"] = "لا توجد سنة المالية مفتوحة.";
                return RedirectToAction("Index");
            }

            ViewBag.FiscalYearName = currentYear.Name;

            // تعبئة القوائم (الحسابات)
            var accounts = db.Accounts.Where(a => a.IsTransactional)
                             .Select(a => new { a.Id, Text = a.Code + " - " + a.Name }).ToList();
            ViewBag.Accounts = new SelectList(accounts, "Id", "Text");

            var costCenters = db.CostCenters.Select(c => new { c.Id, Text = c.Name }).ToList();
            ViewBag.CostCenters = new SelectList(costCenters, "Id", "Text");

            return View(new JournalEntryViewModel { EntryDate = DateTime.Now });
        }

        // 5. حفظ القيد اليدوي (Create - POST via AJAX usually)
        [HttpPost]
        public ActionResult SaveEntry(JournalEntryViewModel model)
        {
            // ... (نفس كود الحفظ السابق الذي أرسلته لك في الإجابات السابقة)
            // سأضعه هنا للاكتمال إذا أردت
            if (!ModelState.IsValid) return Json(new { success = false, message = "بيانات غير صحيحة" });

            var totalDebit = model.Lines.Sum(l => l.Debit);
            var totalCredit = model.Lines.Sum(l => l.Credit);

            if (totalDebit != totalCredit) return Json(new { success = false, message = "القيد غير متوازن" });

            var currentYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
            if (currentYear == null) return Json(new { success = false, message = "السنة المالية مغلقة" });

            // توليد رقم القيد
            string nextEntryNo = "1";
            // ... (منطق التوليد) ...

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var entry = new JournalEntry
                    {
                        FiscalYearId = currentYear.Id,
                        EntryNumber = nextEntryNo, // أو منطق التوليد
                        EntryDate = model.EntryDate,
                        Description = model.Description,
                        ReferenceNumber = model.ReferenceNumber,
                        SourceModule = "Manual",
                        IsPosted = false,
                        TotalDebit = totalDebit,
                        TotalCredit = totalCredit,
                        CreatedBy = Session["FullName"]?.ToString() ?? "System",
                        JournalEntryDetails = new System.Collections.Generic.List<JournalEntryDetail>()
                    };

                    foreach (var line in model.Lines)
                    {
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

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}