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
    public class AccountingReportsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. الأكشن الخاص بالعرض (الشاشة)
        public ActionResult TrialBalance(DateTime? from, DateTime? to)
        {
            var viewModel = GetTrialBalanceData(from, to);
            return View(viewModel);
        }

        // 2. الأكشن الخاص بالطباعة
        public ActionResult PrintTrialBalance(DateTime? from, DateTime? to)
        {
            var viewModel = GetTrialBalanceData(from, to);
            return View(viewModel);
        }

        // 3. دالة مساعدة (Private) لمنع تكرار الكود
        private TrialBalanceViewModel GetTrialBalanceData(DateTime? from, DateTime? to)
        {
            // تحديد تواريخ افتراضية
            if (!from.HasValue) from = new DateTime(DateTime.Now.Year, 1, 1);
            if (!to.HasValue) to = DateTime.Now;

            DateTime toDateEnd = to.Value.Date.AddDays(1).AddTicks(-1);
            DateTime fromDate = from.Value.Date;

            // جلب الحسابات
            var accounts = db.Accounts.Where(a => a.IsTransactional).OrderBy(a => a.Code).ToList();

            // جلب القيود المرحلة
            var allLines = db.JournalEntryLines
                .Include(l => l.JournalEntry)
                .Where(l => l.JournalEntry.IsPosted)
                .Select(l => new { l.AccountId, l.Debit, l.Credit, l.JournalEntry.EntryDate })
                .ToList();

            var reportRows = new List<TrialBalanceRow>();

            foreach (var account in accounts)
            {
                // الرصيد الافتتاحي
                var openingLines = allLines.Where(l => l.AccountId == account.Id && l.EntryDate < fromDate);
                decimal openDebit = openingLines.Sum(l => l.Debit);
                decimal openCredit = openingLines.Sum(l => l.Credit);

                if (account.OpeningBalance > 0) openDebit += account.OpeningBalance;
                else openCredit += Math.Abs(account.OpeningBalance);

                // حركة الفترة
                var periodLines = allLines.Where(l => l.AccountId == account.Id && l.EntryDate >= fromDate && l.EntryDate <= toDateEnd);
                decimal periodDebit = periodLines.Sum(l => l.Debit);
                decimal periodCredit = periodLines.Sum(l => l.Credit);

                if (openDebit == 0 && openCredit == 0 && periodDebit == 0 && periodCredit == 0) continue;

                reportRows.Add(new TrialBalanceRow
                {
                    AccountCode = account.Code,
                    AccountName = account.Name,
                    OpeningDebit = openDebit,
                    OpeningCredit = openCredit,
                    PeriodDebit = periodDebit,
                    PeriodCredit = periodCredit
                });
            }

            return new TrialBalanceViewModel { FromDate = from, ToDate = to, Rows = reportRows };
        }


        // ... داخل الكلاس AccountingReportsController ...

        // عرض قائمة الدخل
        public ActionResult IncomeStatement(DateTime? from, DateTime? to)
        {
            var viewModel = BuildIncomeStatement(from, to);
            return View(viewModel);
        }

        // طباعة قائمة الدخل
        public ActionResult PrintIncomeStatement(DateTime? from, DateTime? to)
        {
            var viewModel = BuildIncomeStatement(from, to);
            return View(viewModel);
        }

        // المنطق الحسابي (Private Helper)
        private IncomeStatementViewModel BuildIncomeStatement(DateTime? from, DateTime? to)
        {
            // 1. تحديد التواريخ
            if (!from.HasValue) from = new DateTime(DateTime.Now.Year, 1, 1);
            if (!to.HasValue) to = DateTime.Now;

            DateTime fromDate = from.Value.Date;
            DateTime toDateEnd = to.Value.Date.AddDays(1).AddTicks(-1);

            var viewModel = new IncomeStatementViewModel
            {
                FromDate = from,
                ToDate = to
            };

            // 2. جلب الحركات المرحلة خلال الفترة
            // نستخدم Debit/Credit (بالشيكل) لضمان توحيد العملة
            var lines = db.JournalEntryLines
                .Include(l => l.Account)
                .Where(l => l.JournalEntry.IsPosted &&
                            l.JournalEntry.EntryDate >= fromDate &&
                            l.JournalEntry.EntryDate <= toDateEnd)
                .ToList();

            // 3. معالجة الإيرادات (تبدأ بـ 4) - طبيعتها دائنة (Credit)
            // المعادلة: الرصيد = الدائن - المدين
            var revenueAccounts = lines
                .Where(l => l.Account.Code.StartsWith("4"))
                .GroupBy(l => l.Account)
                .Select(g => new IncomeStatementItem
                {
                    AccountCode = g.Key.Code,
                    AccountName = g.Key.Name,
                    Amount = g.Sum(x => x.Credit - x.Debit)
                })
                .Where(x => x.Amount != 0) // إخفاء الحسابات الصفرية
                .OrderBy(x => x.AccountCode)
                .ToList();

            viewModel.Revenues = revenueAccounts;
            viewModel.TotalRevenues = revenueAccounts.Sum(x => x.Amount);

            // 4. معالجة المصروفات (تبدأ بـ 5) - طبيعتها مدينة (Debit)
            // المعادلة: الرصيد = المدين - الدائن
            var expenseAccounts = lines
                .Where(l => l.Account.Code.StartsWith("5"))
                .GroupBy(l => l.Account)
                .Select(g => new IncomeStatementItem
                {
                    AccountCode = g.Key.Code,
                    AccountName = g.Key.Name,
                    Amount = g.Sum(x => x.Debit - x.Credit)
                })
                .Where(x => x.Amount != 0)
                .OrderBy(x => x.AccountCode)
                .ToList();

            viewModel.Expenses = expenseAccounts;
            viewModel.TotalExpenses = expenseAccounts.Sum(x => x.Amount);

            return viewModel;
        }

        // ... داخل AccountingReportsController ...

        // عرض الميزانية
        public ActionResult BalanceSheet(DateTime? asOfDate)
        {
            var viewModel = BuildBalanceSheet(asOfDate);
            return View(viewModel);
        }

        // طباعة الميزانية
        public ActionResult PrintBalanceSheet(DateTime? asOfDate)
        {
            var viewModel = BuildBalanceSheet(asOfDate);
            return View(viewModel);
        }

        // المنطق المالي
        private BalanceSheetViewModel BuildBalanceSheet(DateTime? asOfDate)
        {
            // الميزانية تكون "كما هي في تاريخ معين" (عادة اليوم)
            DateTime dateLimit = asOfDate.HasValue ? asOfDate.Value.Date.AddDays(1).AddTicks(-1) : DateTime.Now;

            var viewModel = new BalanceSheetViewModel
            {
                AsOfDate = dateLimit
            };

            // جلب كل الحركات المرحلة حتى التاريخ المحدد
            var lines = db.JournalEntryLines
                .Include(l => l.Account)
                .Where(l => l.JournalEntry.IsPosted && l.JournalEntry.EntryDate <= dateLimit)
                .ToList();

            // 1. الأصول (تبدأ بـ 1) -> طبيعتها مدينة (Debit - Credit)
            viewModel.Assets = lines
                .Where(l => l.Account.Code.StartsWith("1"))
                .GroupBy(l => l.Account)
                .Select(g => new BalanceSheetItem
                {
                    AccountCode = g.Key.Code,
                    AccountName = g.Key.Name,
                    Amount = g.Sum(x => x.Debit - x.Credit)
                })
                .Where(x => x.Amount != 0)
                .OrderBy(x => x.AccountCode)
                .ToList();

            viewModel.TotalAssets = viewModel.Assets.Sum(x => x.Amount);

            // 2. الخصوم (تبدأ بـ 2) -> طبيعتها دائنة (Credit - Debit)
            viewModel.Liabilities = lines
                .Where(l => l.Account.Code.StartsWith("2"))
                .GroupBy(l => l.Account)
                .Select(g => new BalanceSheetItem
                {
                    AccountCode = g.Key.Code,
                    AccountName = g.Key.Name,
                    Amount = g.Sum(x => x.Credit - x.Debit)
                })
                .Where(x => x.Amount != 0)
                .OrderBy(x => x.AccountCode)
                .ToList();

            viewModel.TotalLiabilities = viewModel.Liabilities.Sum(x => x.Amount);

            // 3. حقوق الملكية (تبدأ بـ 3) -> طبيعتها دائنة
            viewModel.Equity = lines
                .Where(l => l.Account.Code.StartsWith("3"))
                .GroupBy(l => l.Account)
                .Select(g => new BalanceSheetItem
                {
                    AccountCode = g.Key.Code,
                    AccountName = g.Key.Name,
                    Amount = g.Sum(x => x.Credit - x.Debit)
                })
                .Where(x => x.Amount != 0)
                .OrderBy(x => x.AccountCode)
                .ToList();

            viewModel.TotalEquity = viewModel.Equity.Sum(x => x.Amount);

            // 4. صافي الدخل المتراكم (الإيرادات 4 - المصروفات 5)
            // الإيرادات (دائن) - المصروفات (مدين)
            // نجمع (Credit - Debit) لكل الحسابات التي تبدأ بـ 4 أو 5
            decimal totalRevenue = lines.Where(l => l.Account.Code.StartsWith("4")).Sum(x => x.Credit - x.Debit);
            decimal totalExpense = lines.Where(l => l.Account.Code.StartsWith("5")).Sum(x => x.Debit - x.Credit);

            viewModel.NetIncome = totalRevenue - totalExpense;

            return viewModel;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}