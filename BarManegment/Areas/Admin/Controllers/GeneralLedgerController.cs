using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "FinancialReports")]
    public class GeneralLedgerController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: عرض كشف الحساب
        public ActionResult Index(int? accountId, DateTime? from, DateTime? to)
        {
            // تعبئة القائمة المنسدلة للحسابات (فرعية فقط)
            var accounts = db.Accounts
                .Where(a => !a.ChildAccounts.Any()) // فقط الحسابات النهائية
                .Select(a => new { a.Id, Text = a.Code + " - " + a.Name })
                .OrderBy(a => a.Text)
                .ToList();

            ViewBag.AccountId = new SelectList(accounts, "Id", "Text", accountId);

            // تواريخ افتراضية ذكية: بداية الشهر الحالي ونهايته
            var defaultFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var defaultTo = DateTime.Now;

            if (!accountId.HasValue)
            {
                return View(new GeneralLedgerViewModel { FromDate = defaultFrom, ToDate = defaultTo });
            }

            var viewModel = BuildLedger(accountId.Value, from ?? defaultFrom, to ?? defaultTo);
            return View(viewModel);
        }

        // GET: طباعة كشف الحساب
        public ActionResult Print(int accountId, DateTime? from, DateTime? to)
        {
            if (!from.HasValue) from = new DateTime(DateTime.Now.Year, 1, 1);
            if (!to.HasValue) to = DateTime.Now;

            var viewModel = BuildLedger(accountId, from.Value, to.Value);
            return View(viewModel);
        }

        // المنطق المحاسبي لبناء الكشف (محسن للأداء)
        private GeneralLedgerViewModel BuildLedger(int accountId, DateTime from, DateTime to)
        {
            DateTime toDateEnd = to.Date.AddDays(1).AddTicks(-1); // نهاية اليوم

            var account = db.Accounts.Find(accountId);
            if (account == null) return new GeneralLedgerViewModel();

            // 1. حساب الرصيد الافتتاحي (SQL-Side Calculation)
            // هذا أسرع بكثير من جلب البيانات للذاكرة
            var openingStats = db.JournalEntryDetails
                .Where(l => l.AccountId == accountId &&
                            l.JournalEntry.IsPosted &&
                            l.JournalEntry.EntryDate < from)
                .GroupBy(l => 1)
                .Select(g => new
                {
                    TotalDebit = g.Sum(x => (decimal?)x.Debit) ?? 0,
                    TotalCredit = g.Sum(x => (decimal?)x.Credit) ?? 0
                })
                .FirstOrDefault();

            decimal openingBalance = account.OpeningBalance + ((openingStats?.TotalDebit ?? 0) - (openingStats?.TotalCredit ?? 0));

            // 2. جلب حركات الفترة
            var periodLines = db.JournalEntryDetails
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId &&
                            l.JournalEntry.IsPosted &&
                            l.JournalEntry.EntryDate >= from &&
                            l.JournalEntry.EntryDate <= toDateEnd)
                .OrderBy(l => l.JournalEntry.EntryDate)
                .ThenBy(l => l.JournalEntry.Id) // لضمان الترتيب المنطقي
                .Select(l => new
                {
                    l.JournalEntry.EntryDate,
                    l.JournalEntry.SourceModule,
                    l.JournalEntry.ReferenceNumber,
                    l.JournalEntry.Description, // شرح القيد العام
                    DetailDescription = l.Description, // شرح السطر التفصيلي
                    l.Debit,
                    l.Credit
                })
                .ToList();

            // 3. بناء الجدول مع الرصيد المتحرك
            var transactions = new List<GeneralLedgerRow>();
            decimal runningBalance = openingBalance;

            decimal totalPeriodDebit = 0;
            decimal totalPeriodCredit = 0;

            foreach (var line in periodLines)
            {
                runningBalance += (line.Debit - line.Credit);
                totalPeriodDebit += line.Debit;
                totalPeriodCredit += line.Credit;

                // نفضل الشرح التفصيلي (الخاص بالسطر) إذا وجد، وإلا نأخذ شرح القيد العام
                string desc = !string.IsNullOrEmpty(line.DetailDescription) ? line.DetailDescription : line.Description;

                transactions.Add(new GeneralLedgerRow
                {
                    Date = line.EntryDate,
                    DocumentType = TranslateSourceModule(line.SourceModule),
                    ReferenceNumber = line.ReferenceNumber,
                    Description = desc,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    Balance = runningBalance
                });
            }

            return new GeneralLedgerViewModel
            {
                AccountId = account.Id,
                AccountName = account.Name,
                AccountCode = account.Code,
                FromDate = from,
                ToDate = to,
                OpeningBalance = openingBalance,
                Transactions = transactions,
                TotalDebit = totalPeriodDebit,
                TotalCredit = totalPeriodCredit,
                ClosingBalance = runningBalance
            };
        }

        // ترجمة مصدر القيد للعربية (محدثة)
        private string TranslateSourceModule(string source)
        {
            if (string.IsNullOrEmpty(source)) return "قيد";

            switch (source)
            {
                case "Receipts": return "سند قبض";
                case "GeneralExpenses": return "سند صرف";
                case "JournalEntries": return "قيد يومية";
                case "OpeningBalance": return "قيد افتتاحي";
                case "StampSales": return "مبيعات طوابع";
                case "LoanDisbursement": return "صرف قرض";
                case "CheckCollection": return "تحصيل شيك";
                case "CheckBounce": return "شيك مرتجع";
                case "FinancialAid": return "مساعدة مالية";
                default: return source;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}