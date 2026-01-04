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
                .Where(a => a.IsTransactional)
                .Select(a => new { a.Id, Text = a.Code + " - " + a.Name })
                .OrderBy(a => a.Text)
                .ToList();

            ViewBag.AccountId = new SelectList(accounts, "Id", "Text", accountId);

            // إذا لم يتم اختيار حساب، نعرض الصفحة فارغة
            if (!accountId.HasValue)
            {
                return View(new GeneralLedgerViewModel { FromDate = DateTime.Now.AddMonths(-1), ToDate = DateTime.Now });
            }

            var viewModel = BuildLedger(accountId.Value, from, to);
            return View(viewModel);
        }

        // GET: طباعة كشف الحساب
        public ActionResult Print(int accountId, DateTime? from, DateTime? to)
        {
            var viewModel = BuildLedger(accountId, from, to);
            return View(viewModel);
        }

        // المنطق المحاسبي لبناء الكشف
        private GeneralLedgerViewModel BuildLedger(int accountId, DateTime? from, DateTime? to)
        {
            // تواريخ افتراضية
            if (!from.HasValue) from = new DateTime(DateTime.Now.Year, 1, 1);
            if (!to.HasValue) to = DateTime.Now;

            DateTime fromDate = from.Value.Date;
            DateTime toDateEnd = to.Value.Date.AddDays(1).AddTicks(-1);

            var account = db.Accounts.Find(accountId);
            if (account == null) return new GeneralLedgerViewModel();

            // 1. حساب الرصيد الافتتاحي (كل ما قبل تاريخ البداية)
            // نجمع الحركات المرحلة فقط
            var openingLines = db.JournalEntryLines
                .Where(l => l.AccountId == accountId &&
                            l.JournalEntry.IsPosted &&
                            l.JournalEntry.EntryDate < fromDate)
                .Select(l => new { l.Debit, l.Credit })
                .ToList();

            decimal openingBalance = (account.OpeningBalance) + openingLines.Sum(x => x.Debit - x.Credit);

            // 2. جلب حركات الفترة
            var periodLines = db.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.AccountId == accountId &&
                            l.JournalEntry.IsPosted &&
                            l.JournalEntry.EntryDate >= fromDate &&
                            l.JournalEntry.EntryDate <= toDateEnd)
                .OrderBy(l => l.JournalEntry.EntryDate)
                .ThenBy(l => l.JournalEntry.Id) // لضمان الترتيب في نفس اليوم
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

                string docType = TranslateSourceModule(line.JournalEntry.SourceModule);

                transactions.Add(new GeneralLedgerRow
                {
                    Date = line.JournalEntry.EntryDate,
                    DocumentType = docType,
                    ReferenceNumber = line.JournalEntry.ReferenceNumber,
                    Description = line.Description ?? line.JournalEntry.Description,
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

        // ترجمة مصدر القيد للعربية
        private string TranslateSourceModule(string source)
        {
            switch (source)
            {
                case "Receipts": return "سند قبض";
                case "GeneralExpenses": return "سند صرف";
                case "JournalEntries": return "قيد يومية";
                case "OpeningBalance": return "قيد افتتاحي";
                default: return "قيد تسوية";
            }
        }
    }
}