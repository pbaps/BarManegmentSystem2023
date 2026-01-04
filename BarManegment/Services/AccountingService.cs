using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace BarManegment.Services
{
    public class AccountingService : IDisposable
    {
        private readonly ApplicationDbContext db;

        public AccountingService()
        {
            db = new ApplicationDbContext();
        }

        // ============================================================
        // 1. دوال مساعدة (Helpers)
        // ============================================================
        private decimal GetExchangeRate(string currencySymbol)
        {
            if (string.IsNullOrEmpty(currencySymbol) || currencySymbol == "₪" || currencySymbol.ToUpper() == "NIS")
                return 1.0m;

            try
            {
                var currency = db.Currencies.FirstOrDefault(c => c.Symbol == currencySymbol);
                if (currency != null)
                {
                    var rateRecord = db.ExchangeRates
                        .Where(x => x.CurrencyId == currency.Id)
                        .OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
                        .FirstOrDefault();

                    if (rateRecord != null && rateRecord.Rate > 0) return rateRecord.Rate;
                }

                switch (currencySymbol.Trim().ToUpper())
                {
                    case "JD": return 5.35m;
                    case "$": case "USD": return 3.75m;
                    case "€": case "EUR": return 4.00m;
                    default: return 1.0m;
                }
            }
            catch { return 1.0m; }
        }

        private string GetNextEntryNumber(int fiscalYearId)
        {
            string nextEntryNo = "1";
            var lastEntry = db.JournalEntries.Where(j => j.FiscalYearId == fiscalYearId).OrderByDescending(j => j.Id).FirstOrDefault();
            if (lastEntry != null && int.TryParse(lastEntry.EntryNumber, out int lastNo))
                nextEntryNo = (lastNo + 1).ToString();
            else
                nextEntryNo = (db.JournalEntries.Count(j => j.FiscalYearId == fiscalYearId) + 1).ToString();
            return nextEntryNo;
        }

        private void BalanceEntry(JournalEntry entry)
        {
            decimal dTotal = entry.JournalEntryDetails.Sum(x => x.Debit);
            decimal cTotal = entry.JournalEntryDetails.Sum(x => x.Credit);

            if (dTotal != cTotal)
            {
                var diff = dTotal - cTotal;
                var lastLine = entry.JournalEntryDetails.Last();
                if (diff > 0) lastLine.Credit += diff;
                else lastLine.Debit += Math.Abs(diff);
            }

            entry.TotalDebit = entry.JournalEntryDetails.Sum(x => x.Debit);
            entry.TotalCredit = entry.JournalEntryDetails.Sum(x => x.Credit);
        }

        // ============================================================
        // 2. قيد سند قبض (Receipts)
        // ============================================================
        public bool GenerateEntryForReceipt(int receiptId, int userId)
        {
            try
            {
                var receipt = db.Receipts
                    .Include(r => r.PaymentVoucher.GraduateApplication)
                    .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency))
                    .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.BankAccount))
                    .FirstOrDefault(r => r.Id == receiptId);

                if (receipt == null) return false;
                if (db.JournalEntries.Any(j => j.SourceModule == "Receipts" && j.SourceId == receiptId)) return true;

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) return false;

                string debitAccountCode = (receipt.PaymentVoucher.PaymentMethod == "نقدي") ? "1101" : "1102";
                var debitAccount = db.Accounts.FirstOrDefault(a => a.Code == debitAccountCode)
                                   ?? db.Accounts.FirstOrDefault(a => a.Code.StartsWith(debitAccountCode));

                if (debitAccount == null) return false;

                var firstDetail = receipt.PaymentVoucher.VoucherDetails.FirstOrDefault();
                string currencySymbol = firstDetail?.FeeType?.Currency?.Symbol ?? "₪";
                decimal exchangeRate = GetExchangeRate(currencySymbol);
                int? currencyId = firstDetail?.FeeType?.Currency?.Id;

                var entry = new JournalEntry
                {
                    FiscalYearId = fiscalYear.Id,
                    EntryNumber = GetNextEntryNumber(fiscalYear.Id),
                    EntryDate = receipt.BankPaymentDate,
                    ReferenceNumber = receipt.BankReceiptNumber ?? receipt.SequenceNumber.ToString(),
                    Description = $"سند قبض {receipt.SequenceNumber} - {receipt.PaymentVoucher.GraduateApplication?.ArabicName ?? "متعهد"}",
                    SourceModule = "Receipts",
                    SourceId = receiptId,
                    IsPosted = true,
                    PostedDate = DateTime.Now,
                    ExchangeRate = exchangeRate,
                    CurrencyId = currencyId,
                    CreatedBy = "System Auto",
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                // المدين
                decimal totalAmountBase = Math.Round(receipt.PaymentVoucher.TotalAmount * exchangeRate, 2);
                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = debitAccount.Id, Debit = totalAmountBase, Credit = 0, Description = "تحصيل سند قبض" });

                // الدائن
                foreach (var detail in receipt.PaymentVoucher.VoucherDetails)
                {
                    string code = "42";
                    string name = detail.FeeType?.Name ?? "";

                    if (name.Contains("قرض") || name.Contains("سداد")) code = "1103";
                    else if (name.Contains("أمانات")) code = "2103";
                    else if (name.Contains("طوابع")) code = "4201";
                    else if (name.Contains("تصديق")) code = "4202";
                    else if (name.Contains("انتساب")) code = "4101";
                    else if (name.Contains("اشتراك")) code = "4102";

                    var creditAcc = db.Accounts.FirstOrDefault(a => a.Code == code) ?? db.Accounts.FirstOrDefault(a => a.Code.StartsWith("4"));

                    if (creditAcc != null)
                    {
                        decimal lineBase = Math.Round(detail.Amount * exchangeRate, 2);
                        entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = creditAcc.Id, Debit = 0, Credit = lineBase, Description = detail.Description });
                    }
                }

                BalanceEntry(entry);
                db.JournalEntries.Add(entry);
                db.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        // ============================================================
        // 3. قيد سند صرف (GeneralExpenses)
        // ============================================================
        public bool GenerateEntryForExpense(int expenseId, int userId)
        {
            try
            {
                var expense = db.GeneralExpenses
                    .Include(e => e.ExpenseAccount).Include(e => e.TreasuryAccount)
                    .FirstOrDefault(e => e.Id == expenseId);

                if (expense == null || expense.IsPosted) return false;

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) return false;

                string currencySymbol = "₪";
                if (expense.TreasuryAccount.Name.Contains("دينار")) currencySymbol = "JD";
                else if (expense.TreasuryAccount.Name.Contains("دولار")) currencySymbol = "$";

                decimal exchangeRate = GetExchangeRate(currencySymbol);
                decimal amountBase = Math.Round(expense.Amount * exchangeRate, 2);

                var entry = new JournalEntry
                {
                    FiscalYearId = fiscalYear.Id,
                    EntryNumber = GetNextEntryNumber(fiscalYear.Id),
                    EntryDate = expense.ExpenseDate,
                    ReferenceNumber = expense.VoucherNumber,
                    Description = $"سند صرف {expense.VoucherNumber} - {expense.PayeeName}",
                    SourceModule = "GeneralExpenses",
                    SourceId = expense.Id,
                    IsPosted = true,
                    PostedDate = DateTime.Now,
                    CreatedBy = "System Auto",
                    ExchangeRate = exchangeRate,
                    TotalDebit = amountBase,
                    TotalCredit = amountBase,
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = expense.ExpenseAccountId, CostCenterId = expense.CostCenterId, Debit = amountBase, Credit = 0, Description = expense.Description });
                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = expense.TreasuryAccountId, Debit = 0, Credit = amountBase, Description = $"صرف لـ {expense.PayeeName}" });

                db.JournalEntries.Add(entry);
                expense.IsPosted = true;
                db.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        // ============================================================
        // 4. قيد تسوية بيع طوابع (Stamp Sales)
        // ============================================================
        public bool GenerateEntryForStampSale(List<StampSale> sales, int userId)
        {
            try
            {
                if (!sales.Any()) return false;
                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) return false;

                decimal totalLawyerShare = sales.Sum(s => s.AmountToLawyer);
                decimal totalBarShare = sales.Sum(s => s.AmountToBar);
                decimal totalValue = sales.Sum(s => s.StampValue);

                var prepaidRevenueAccount = db.Accounts.FirstOrDefault(a => a.Code == "2102");
                var lawyerLiabilityAccount = db.Accounts.FirstOrDefault(a => a.Code == "2104") ?? db.Accounts.FirstOrDefault(a => a.Code == "2101");
                var actualRevenueAccount = db.Accounts.FirstOrDefault(a => a.Code == "4201");

                if (prepaidRevenueAccount == null || actualRevenueAccount == null) return false;

                var entry = new JournalEntry
                {
                    FiscalYearId = fiscalYear.Id,
                    EntryNumber = GetNextEntryNumber(fiscalYear.Id),
                    EntryDate = DateTime.Now,
                    ReferenceNumber = $"Stamp-{sales.First().Id}",
                    Description = $"تسوية بيع طوابع ({sales.Count}) - {sales.First().LawyerName}",
                    SourceModule = "StampSales",
                    SourceId = sales.First().Id,
                    IsPosted = true,
                    PostedDate = DateTime.Now,
                    CreatedBy = "System Auto",
                    TotalDebit = 0,
                    TotalCredit = 0,
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = prepaidRevenueAccount.Id, Debit = totalValue, Credit = 0, Description = "إقفال إيراد طوابع مؤجل" });

                if (totalLawyerShare > 0 && lawyerLiabilityAccount != null)
                    entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = lawyerLiabilityAccount.Id, Debit = 0, Credit = totalLawyerShare, Description = $"حصة المحامي {sales.First().LawyerName}" });

                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = actualRevenueAccount.Id, Debit = 0, Credit = totalBarShare, Description = "حصة النقابة المحققة" });

                BalanceEntry(entry);
                db.JournalEntries.Add(entry);
                db.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        // ============================================================
        // 5. قيد صرف قرض (Loan Disbursement)
        // ============================================================
        public bool GenerateEntryForLoanDisbursement(int loanId, int userId)
        {
            try
            {
                var loan = db.LoanApplications.Include(l => l.Lawyer).Include(l => l.LoanType).FirstOrDefault(l => l.Id == loanId);
                if (loan == null) return false;
                if (db.JournalEntries.Any(j => j.SourceModule == "LoanDisbursement" && j.SourceId == loanId)) return true;

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) return false;

                var loanReceivableAccount = db.Accounts.FirstOrDefault(a => a.Code == "1103") ?? db.Accounts.FirstOrDefault(a => a.Code.StartsWith("110"));
                var bankAccount = db.Accounts.FirstOrDefault(a => a.Code == "1102") ?? db.Accounts.FirstOrDefault(a => a.Code.StartsWith("1102"));

                if (loanReceivableAccount == null || bankAccount == null) return false;

                decimal exchangeRate = GetExchangeRate("₪");
                decimal amountBase = Math.Round(loan.Amount * exchangeRate, 2);

                var entry = new JournalEntry
                {
                    FiscalYearId = fiscalYear.Id,
                    EntryNumber = GetNextEntryNumber(fiscalYear.Id),
                    EntryDate = DateTime.Now,
                    ReferenceNumber = $"Loan-{loan.Id}",
                    Description = $"صرف قرض {loan.LoanType.Name} - {loan.Lawyer.ArabicName}",
                    SourceModule = "LoanDisbursement",
                    SourceId = loanId,
                    IsPosted = true,
                    PostedDate = DateTime.Now,
                    CreatedBy = "System Auto",
                    ExchangeRate = exchangeRate,
                    TotalDebit = amountBase,
                    TotalCredit = amountBase,
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = loanReceivableAccount.Id, Debit = amountBase, Credit = 0, Description = "ذمم قروض مستحقة" });
                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = bankAccount.Id, Debit = 0, Credit = amountBase, Description = "صرف القرض" });

                db.JournalEntries.Add(entry);
                db.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        // ============================================================
        // 6. تحصيل شيك (Collect Check) - ✅ حل المشكلة الأولى
        // ============================================================
        public bool CollectCheck(int checkId, int targetBankAccountId, DateTime collectionDate, int userId)
        {
            try
            {
                var check = db.ChecksPortfolio.Find(checkId);
                if (check == null || check.Status != CheckStatus.UnderCollection) return false;

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) return false;

                var checksAccount = db.Accounts.FirstOrDefault(a => a.Code == "1104"); // شيكات برسم التحصيل
                var bankAccount = db.Accounts.Find(targetBankAccountId);
                if (checksAccount == null || bankAccount == null) return false;

                decimal exchangeRate = GetExchangeRate(check.Currency?.Symbol);
                decimal amountBase = Math.Round(check.Amount * exchangeRate, 2);

                var entry = new JournalEntry
                {
                    FiscalYearId = fiscalYear.Id,
                    EntryNumber = GetNextEntryNumber(fiscalYear.Id),
                    EntryDate = collectionDate,
                    ReferenceNumber = check.CheckNumber,
                    Description = $"تحصيل شيك {check.CheckNumber}",
                    SourceModule = "CheckCollection",
                    SourceId = checkId,
                    IsPosted = true,
                    TotalDebit = amountBase,
                    TotalCredit = amountBase,
                    CreatedBy = "System Auto",
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = bankAccount.Id, Debit = amountBase, Credit = 0, Description = "إيداع شيك" });
                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = checksAccount.Id, Debit = 0, Credit = amountBase, Description = "تسوية شيك" });

                db.JournalEntries.Add(entry);
                check.Status = CheckStatus.Collected;
                check.ActionDate = collectionDate;
                db.SaveChanges();

                check.ActionJournalEntryId = entry.Id;
                db.Entry(check).State = EntityState.Modified;
                db.SaveChanges();

                return true;
            }
            catch { return false; }
        }

        // ============================================================
        // 7. ارتجاع شيك (Bounce Check) - ✅ حل المشكلة الثانية
        // ============================================================
        public bool BounceCheck(int checkId, string reason, int userId)
        {
            try
            {
                var check = db.ChecksPortfolio.Find(checkId);
                if (check == null) return false;

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) return false;

                var checksAccount = db.Accounts.FirstOrDefault(a => a.Code == "1104");
                var debtorsAccount = db.Accounts.FirstOrDefault(a => a.Code == "1105") ?? db.Accounts.FirstOrDefault(a => a.Code.StartsWith("110"));
                if (checksAccount == null || debtorsAccount == null) return false;

                decimal exchangeRate = GetExchangeRate(check.Currency?.Symbol);
                decimal amountBase = Math.Round(check.Amount * exchangeRate, 2);

                var entry = new JournalEntry
                {
                    FiscalYearId = fiscalYear.Id,
                    EntryNumber = GetNextEntryNumber(fiscalYear.Id),
                    EntryDate = DateTime.Now,
                    ReferenceNumber = check.CheckNumber,
                    Description = $"ارتجاع شيك {check.CheckNumber} - {reason}",
                    SourceModule = "CheckBounce",
                    SourceId = checkId,
                    IsPosted = true,
                    TotalDebit = amountBase,
                    TotalCredit = amountBase,
                    CreatedBy = "System Auto",
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = debtorsAccount.Id, Debit = amountBase, Credit = 0, Description = $"شيك مرتجع: {reason}" });
                entry.JournalEntryDetails.Add(new JournalEntryDetail { AccountId = checksAccount.Id, Debit = 0, Credit = amountBase, Description = "إلغاء شيك" });

                db.JournalEntries.Add(entry);
                check.Status = CheckStatus.Bounced;
                check.ActionDate = DateTime.Now;
                db.SaveChanges();

                check.ActionJournalEntryId = entry.Id;
                db.Entry(check).State = EntityState.Modified;
                db.SaveChanges();

                return true;
            }
            catch { return false; }
        }
        // ============================================================
        // 8. قيد صرف مساعدة مالية (Financial Aid Disbursement)
        // ============================================================
        public bool GenerateEntryForFinancialAid(int aidId, int bankAccountId, int userId)
        {
            try
            {
                var aid = db.LawyerFinancialAids.Include(a => a.Lawyer).Include(a => a.AidType).Include(a => a.Currency).FirstOrDefault(a => a.Id == aidId);
                if (aid == null) return false;

                // منع التكرار
                if (db.JournalEntries.Any(j => j.SourceModule == "FinancialAid" && j.SourceId == aidId)) return true;

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) return false;

                // تحديد الحسابات
                // 1. حساب البنك (الدائن)
                var creditAccount = db.Accounts.Find(bankAccountId); // الحساب الذي تم اختياره في الصرف

                // 2. حساب مصروف المساعدات (المدين) - نفترض الكود "5205" أو نبحث بالاسم
                var debitAccount = db.Accounts.FirstOrDefault(a => a.Name.Contains("مساعدات") && a.AccountType == AccountType.Expense)
                                   ?? db.Accounts.FirstOrDefault(a => a.Code == "5205");

                // إذا لم يوجد حساب مساعدات، نستخدم حساب المصروفات العام مؤقتاً
                if (debitAccount == null) debitAccount = db.Accounts.FirstOrDefault(a => a.Code.StartsWith("5"));

                if (creditAccount == null || debitAccount == null) return false;

                decimal exchangeRate = GetExchangeRate(aid.Currency?.Symbol);
                decimal amountBase = Math.Round(aid.Amount * exchangeRate, 2);

                var entry = new JournalEntry
                {
                    FiscalYearId = fiscalYear.Id,
                    EntryNumber = GetNextEntryNumber(fiscalYear.Id),
                    EntryDate = DateTime.Now,
                    ReferenceNumber = $"AID-{aid.Id}",
                    Description = $"صرف مساعدة ({aid.AidType?.Name}) للمحامي {aid.Lawyer.ArabicName}",
                    SourceModule = "FinancialAid",
                    SourceId = aidId,
                    IsPosted = true,
                    PostedDate = DateTime.Now,
                    CreatedBy = "System Auto",
                    CreatedByUserId = userId,
                    ExchangeRate = exchangeRate,
                    CurrencyId = aid.CurrencyId,
                    TotalDebit = amountBase,
                    TotalCredit = amountBase,
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                // الطرف المدين (المصروف)
                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = debitAccount.Id,
                    Debit = amountBase,
                    Credit = 0,
                    Description = $"مساعدة {aid.AidType?.Name}",
                    CurrencyId = aid.CurrencyId,
                    ExchangeRate = exchangeRate
                });

                // الطرف الدائن (البنك)
                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = creditAccount.Id,
                    Debit = 0,
                    Credit = amountBase,
                    Description = $"صرف للمحامي {aid.Lawyer.ArabicName}",
                    CurrencyId = aid.CurrencyId,
                    ExchangeRate = exchangeRate
                });

                db.JournalEntries.Add(entry);
                db.SaveChanges();
                return true;
            }
            catch { return false; }
        }
        public void Dispose()
        {
            if (db != null) db.Dispose();
        }
    }
}