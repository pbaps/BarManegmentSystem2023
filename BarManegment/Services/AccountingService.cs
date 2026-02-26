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
        private readonly bool _shouldDispose; // متغير لتحديد هل نغلق قاعدة البيانات أم لا

        // ✅ 1. تعريف كائن القفل (Lock Object) لحل مشكلة تكرار أرقام القيود
        private static readonly object _entryLock = new object();

 

 

        // 1. الكونستركتور الافتراضي (ينشئ اتصال جديد)
        public AccountingService()
        {
            db = new ApplicationDbContext();
            _shouldDispose = true; // نحن أنشأناه، لذا نحن نغلقه
        }

        // 2. ✅ الكونستركتور الجديد (يستقبل اتصال موجود)
        public AccountingService(ApplicationDbContext context)
        {
            db = context;
            _shouldDispose = false; // هام جداً: لا تغلق الاتصال لأن الكنترولر يملكه
        }

        // ============================================================
        // 1. Helpers (دوال مساعدة)
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

        // ✅ دالة توليد رقم القيد (Thread-Safe)
        private string GetNextEntryNumber(int fiscalYearId)
        {
            lock (_entryLock)
            {
                using (var context = new ApplicationDbContext())
                {
                    var lastEntry = context.JournalEntries
                                           .Where(j => j.FiscalYearId == fiscalYearId)
                                           .OrderByDescending(j => j.Id)
                                           .FirstOrDefault();

                    if (lastEntry != null && long.TryParse(lastEntry.EntryNumber, out long lastNo))
                    {
                        return (lastNo + 1).ToString();
                    }
                    return "1";
                }
            }
        }

        // ✅ دالة موازنة القيد (تمنع إخفاء الأخطاء الكبيرة)
        private void BalanceEntry(JournalEntry entry)
        {
            if (entry.JournalEntryDetails == null || !entry.JournalEntryDetails.Any()) return;

            decimal dTotal = entry.JournalEntryDetails.Sum(x => x.Debit);
            decimal cTotal = entry.JournalEntryDetails.Sum(x => x.Credit);

            decimal diff = dTotal - cTotal;

            if (Math.Abs(diff) > 1.0m)
            {
                throw new Exception($"خطأ محاسبي: القيد غير متزن بفرق ({diff}). يرجى مراجعة إعدادات الحسابات والعملات.");
            }

            if (diff != 0)
            {
                var lastLine = entry.JournalEntryDetails.Last();
                if (diff > 0) lastLine.Credit += diff;
                else lastLine.Debit += Math.Abs(diff);
            }

            entry.TotalDebit = entry.JournalEntryDetails.Sum(x => x.Debit);
            entry.TotalCredit = entry.JournalEntryDetails.Sum(x => x.Credit);
        }


 


        // دالة مساعدة لجلب رقم الحساب من الإعدادات أو استخدام كود احتياطي
        private int GetAccountIdFromSettings(string settingKey, string fallbackCode)
        {
            // 1. البحث في الإعدادات أولاً
            var setting = db.SystemSettings.FirstOrDefault(s => s.SettingKey == settingKey);
            if (setting != null && setting.ValueInt.HasValue)
            {
                return setting.ValueInt.Value;
            }

            // 2. البحث بالكود الاحتياطي
            var account = db.Accounts.FirstOrDefault(a => a.Code == fallbackCode);
            if (account != null) return account.Id;

            throw new Exception($"لم يتم العثور على الحساب المطلوب. يرجى ضبط الإعداد: {settingKey} أو التأكد من وجود حساب بالكود {fallbackCode}");
        }

        // ============================================================
        // 2. Generate Entry For Receipt (مصححة وديناميكية)
        // ============================================================
        public bool GenerateEntryForReceipt(int receiptId, int userId)
        {
            try
            {
                var receipt = db.Receipts
                    .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.RevenueAccount))
                    .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.BankAccount))
                    .Include(r => r.PaymentVoucher.GraduateApplication)
                    .FirstOrDefault(r => r.Id == receiptId);

                if (receipt == null) return false;

                if (db.JournalEntries.Any(j => j.SourceModule == "Receipts" && j.SourceId == receiptId)) return true;

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null)
                {
                    AuditService.LogAction("AccountingError", "GenerateEntryForReceipt", "لا توجد سنة مالية مفتوحة.");
                    return false;
                }

                int debitAccountId;
                string paymentMethod = receipt.PaymentVoucher.PaymentMethod;

                if (paymentMethod == "نقدي")
                {
                    var mainBox = db.Accounts.FirstOrDefault(a => a.Code == "1101");
                    if (mainBox == null) throw new Exception("حساب الصندوق الرئيسي (1101) غير موجود في الدليل.");
                    debitAccountId = mainBox.Id;
                }
                else
                {
                    var firstDetail = receipt.PaymentVoucher.VoucherDetails.FirstOrDefault();

                    if (firstDetail?.BankAccount != null)
                    {
                        var bankAcc = db.Accounts.FirstOrDefault(a => a.Name == firstDetail.BankAccount.BankName)
                                      ?? db.Accounts.FirstOrDefault(a => a.Code.StartsWith("1102"));

                        if (bankAcc == null) throw new Exception("لا يوجد حساب محاسبي مرتبط بهذا البنك.");
                        debitAccountId = bankAcc.Id;
                    }
                    else
                    {
                        var defaultBank = db.Accounts.FirstOrDefault(a => a.Code.StartsWith("1102"));
                        if (defaultBank == null) throw new Exception("لم يتم تحديد حساب بنكي.");
                        debitAccountId = defaultBank.Id;
                    }
                }

                decimal exchangeRate = GetExchangeRate(receipt.PaymentVoucher.VoucherDetails.FirstOrDefault()?.FeeType?.Currency?.Symbol);

                var entry = new JournalEntry
                {
                    FiscalYearId = fiscalYear.Id,
                    EntryNumber = GetNextEntryNumber(fiscalYear.Id),
                    EntryDate = receipt.BankPaymentDate, // ✅ تم التصحيح: إزالة "?? DateTime.Now"
                    ReferenceNumber = receipt.BankReceiptNumber ?? receipt.SequenceNumber.ToString(),
                    Description = $"سند قبض {receipt.SequenceNumber} - {receipt.PaymentVoucher.GraduateApplication?.ArabicName ?? "إيراد عام"}",
                    SourceModule = "Receipts",
                    SourceId = receiptId,
                    IsPosted = true,
                    PostedDate = DateTime.Now,
                    ExchangeRate = exchangeRate,
                    CurrencyId = receipt.PaymentVoucher.VoucherDetails.FirstOrDefault()?.FeeType?.CurrencyId,
                    CreatedBy = "System Auto",
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                decimal totalAmountLocal = 0;

                foreach (var detail in receipt.PaymentVoucher.VoucherDetails)
                {
                    decimal lineExchange = GetExchangeRate(detail.FeeType?.Currency?.Symbol);
                    decimal lineAmount = Math.Round(detail.Amount * lineExchange, 2);
                    totalAmountLocal += lineAmount;

                    int? creditAccountId = detail.FeeType?.RevenueAccountId;

                    if (creditAccountId == null)
                    {
                        string code = "4201";
                        string name = (detail.FeeType?.Name ?? "").ToLower();
                        if (name.Contains("انتساب")) code = "4101";
                        else if (name.Contains("اشتراك")) code = "4102";

                        var fallbackAcc = db.Accounts.FirstOrDefault(a => a.Code == code) ?? db.Accounts.FirstOrDefault(a => a.Code.StartsWith("4"));
                        if (fallbackAcc == null) throw new Exception($"نوع الرسم '{detail.FeeType?.Name}' غير مربوط بحساب ولا يوجد حساب افتراضي.");
                        creditAccountId = fallbackAcc.Id;
                    }

                    entry.JournalEntryDetails.Add(new JournalEntryDetail
                    {
                        AccountId = creditAccountId.Value,
                        Debit = 0,
                        Credit = lineAmount,
                        Description = detail.Description ?? detail.FeeType?.Name
                    });
                }

                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = debitAccountId,
                    Debit = totalAmountLocal,
                    Credit = 0,
                    Description = "تحصيل سند قبض"
                });

                BalanceEntry(entry);
                db.JournalEntries.Add(entry);
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                AuditService.LogAction("AccountingError", "GenerateEntryForReceipt", $"Error: {ex.Message}");
                return false;
            }
        }

        // ============================================================
        // 3. Generate Entry For Expense
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

                BalanceEntry(entry);
                db.JournalEntries.Add(entry);
                expense.IsPosted = true;
                db.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        // ============================================================
        // 4. Generate Entry For Stamp Sale (محدثة وديناميكية)
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

                // ✅ التغيير هنا: جلب الحسابات من الإعدادات بدلاً من الأكواد الثابتة
                // تأكد من إضافة هذه المفاتيح في جدول SystemSettings لاحقاً
                int prepaidAccId = GetAccountIdFromSettings("Stamp_PrepaidAccount", "2102");      // إيراد طوابع مؤجل
                int lawyerLiabAccId = GetAccountIdFromSettings("Stamp_LawyerShareAccount", "2104"); // أمانات محامين
                int revenueAccId = GetAccountIdFromSettings("Stamp_RevenueAccount", "4201");      // إيرادات طوابع

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
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                // المدين: إقفال الإيراد المؤجل
                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = prepaidAccId,
                    Debit = totalValue,
                    Credit = 0,
                    Description = "إقفال إيراد طوابع مؤجل"
                });

                // الدائن: حصة المحامي
                if (totalLawyerShare > 0)
                {
                    entry.JournalEntryDetails.Add(new JournalEntryDetail
                    {
                        AccountId = lawyerLiabAccId,
                        Debit = 0,
                        Credit = totalLawyerShare,
                        Description = $"حصة المحامي {sales.First().LawyerName}"
                    });
                }

                // الدائن: حصة النقابة
                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = revenueAccId,
                    Debit = 0,
                    Credit = totalBarShare,
                    Description = "حصة النقابة المحققة"
                });

                BalanceEntry(entry);
                db.JournalEntries.Add(entry);
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                AuditService.LogAction("AccountingError", "StampSale", ex.Message);
                return false;
            }
        }
 
        // ============================================================
        // 5. Generate Entry For Loan Disbursement (محدثة وديناميكية)
        // ============================================================
        public bool GenerateEntryForLoanDisbursement(int loanId, int userId)
        {
            try
            {
                // ✅ تضمين نوع القرض وحسابه المحاسبي
                var loan = db.LoanApplications
                    .Include(l => l.Lawyer)
                    .Include(l => l.LoanType.ReceivableAccount) // تأكد من تضمين الحساب
                    .FirstOrDefault(l => l.Id == loanId);

                if (loan == null) return false;
                if (db.JournalEntries.Any(j => j.SourceModule == "LoanDisbursement" && j.SourceId == loanId)) return true;

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) return false;

                // 1. تحديد حساب الذمم (من نوع القرض)
                int receivableAccountId;
                if (loan.LoanType.ReceivableAccountId.HasValue)
                {
                    receivableAccountId = loan.LoanType.ReceivableAccountId.Value;
                }
                else
                {
                    // Fallback: استخدام الكود القديم
                    var defaultRec = db.Accounts.FirstOrDefault(a => a.Code == "1103") ?? db.Accounts.FirstOrDefault(a => a.Code.StartsWith("110"));
                    if (defaultRec == null) throw new Exception("لم يتم تحديد حساب ذمم القروض في نوع القرض ولا يوجد حساب افتراضي.");
                    receivableAccountId = defaultRec.Id;
                }

                // 2. تحديد حساب البنك (الدائن)
                // يفضل أن يكون الديناميكية هنا بجعل المستخدم يختار البنك عند الصرف، 
                // ولكن للتبسيط سنستخدم حساب البنك الافتراضي من الإعدادات
                int bankAccountId = GetAccountIdFromSettings("Default_Bank_Payment_Account", "1102");

                decimal exchangeRate = GetExchangeRate("₪"); // أو عملة القرض إذا كانت متغيرة
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
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                // من ح/ ذمم القروض (المدين)
                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = receivableAccountId,
                    Debit = amountBase,
                    Credit = 0,
                    Description = $"ذمة قرض - {loan.Lawyer.ArabicName}"
                });

                // إلى ح/ البنك (الدائن)
                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = bankAccountId,
                    Debit = 0,
                    Credit = amountBase,
                    Description = $"صرف القرض رقم {loan.Id}"
                });

                BalanceEntry(entry);
                db.JournalEntries.Add(entry);
                db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                AuditService.LogAction("AccountingError", "LoanDisbursement", ex.Message);
                return false;
            }
        }
        // ============================================================
        // 6. Collect Check
        // ============================================================
        public bool CollectCheck(int checkId, int targetBankAccountId, DateTime collectionDate, int userId)
        {
            try
            {
                var check = db.ChecksPortfolio.Find(checkId);
                if (check == null || check.Status != CheckStatus.UnderCollection) return false;

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) return false;

                var checksAccount = db.Accounts.FirstOrDefault(a => a.Code == "1104");
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
        // 7. Bounce Check
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
        // 8. Generate Entry For Financial Aid
        // ============================================================
        public bool GenerateEntryForFinancialAid(int aidId, int bankAccountId, int userId)
        {
            try
            {
                var aid = db.LawyerFinancialAids.Include(a => a.Lawyer).Include(a => a.AidType).Include(a => a.Currency).FirstOrDefault(a => a.Id == aidId);
                if (aid == null) return false;

                if (db.JournalEntries.Any(j => j.SourceModule == "FinancialAid" && j.SourceId == aidId)) return true;

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) return false;

                var creditAccount = db.Accounts.Find(bankAccountId);
                var debitAccount = db.Accounts.FirstOrDefault(a => a.Name.Contains("مساعدات") && a.AccountType == AccountType.Expense)
                                    ?? db.Accounts.FirstOrDefault(a => a.Code == "5205");

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
                    ExchangeRate = exchangeRate,
                    CurrencyId = aid.CurrencyId,
                    TotalDebit = amountBase,
                    TotalCredit = amountBase,
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = debitAccount.Id,
                    Debit = amountBase,
                    Credit = 0,
                    Description = $"مساعدة {aid.AidType?.Name}",
                    CurrencyId = aid.CurrencyId,
                    ExchangeRate = exchangeRate
                });

                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = creditAccount.Id,
                    Debit = 0,
                    Credit = amountBase,
                    Description = $"صرف للمحامي {aid.Lawyer.ArabicName}",
                    CurrencyId = aid.CurrencyId,
                    ExchangeRate = exchangeRate
                });

                BalanceEntry(entry);
                db.JournalEntries.Add(entry);
                db.SaveChanges();
                return true;
            }
            catch { return false; }
        }



        // ============================================================
        // 6. إنشاء قيد سداد القسط (متوافق مع صفحة الإعدادات الجديدة)
        // ============================================================
 
        public bool GenerateEntryForLoanRepayment(int receiptId, int loanTypeId, int userId)
        {
            try
            {
                // إذا كنا نستخدم نفس الـ Context، فالبيانات قد تكون في الذاكرة (Local) ولم تذهب للقاعدة بعد
                // لذا نستخدم Find أولاً لأنه يبحث في الذاكرة
                var receipt = db.Receipts.Find(receiptId);

                // إذا لم نجدها في الذاكرة، أو نحتاج تحميل العلاقات (Include لا يعمل مع Find)
                // نستخدم التوجيه الصريح لتحميل العلاقات إذا كانت فارغة
                if (receipt != null)
                {
                    if (receipt.PaymentVoucher == null)
                        db.Entry(receipt).Reference(r => r.PaymentVoucher).Load();

                    if (receipt.PaymentVoucher != null && (receipt.PaymentVoucher.VoucherDetails == null || !receipt.PaymentVoucher.VoucherDetails.Any()))
                        db.Entry(receipt.PaymentVoucher).Collection(v => v.VoucherDetails).Load();
                }

                if (receipt == null || receipt.PaymentVoucher == null)
                    throw new Exception($"الإيصال رقم {receiptId} غير موجود أو غير مرتبط بقسيمة.");

                var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (fiscalYear == null) throw new Exception("لا توجد سنة مالية مفتوحة.");

                // ... (باقي كود الدالة كما هو تماماً بدون تغيير) ...

                // (نسخ الكود من الرد السابق الخاص بتحديد الحسابات وإنشاء القيد)
                // ...
                // ...

                // تحديد الحسابات
                int debitAccountId;
                int creditAccountId;

                if (receipt.PaymentVoucher.PaymentMethod == "نقدي")
                {
                    debitAccountId = GetAccountIdFromSettings("GL_MainBoxAccount", "1101");
                }
                else
                {
                    debitAccountId = GetAccountIdFromSettings("GL_DefaultBankAccount", "1102");
                }

                var loanType = db.LoanTypes.Find(loanTypeId);
                if (loanType != null && loanType.ReceivableAccountId.HasValue)
                {
                    creditAccountId = loanType.ReceivableAccountId.Value;
                }
                else
                {
                    creditAccountId = GetAccountIdFromSettings("GL_LoanReceivableAccount", "1103");
                }

                decimal amount = receipt.PaymentVoucher.VoucherDetails.Sum(d => d.Amount);

                var entry = new JournalEntry
                {
                    FiscalYearId = fiscalYear.Id,
                    EntryNumber = GetNextEntryNumber(fiscalYear.Id),
                    EntryDate = receipt.BankPaymentDate,
                    Description = $"سداد قسط قرض - {receipt.PaymentVoucher.GraduateApplication?.ArabicName} - إيصال {receipt.SequenceNumber}",
                    ReferenceNumber = receipt.BankReceiptNumber ?? receipt.SequenceNumber.ToString(),
                    SourceModule = "LoanRepayment",
                    SourceId = receiptId,
                    IsPosted = true,
                    PostedDate = DateTime.Now,
                    CreatedBy = "System Auto",
                    TotalDebit = amount,
                    TotalCredit = amount,
                    JournalEntryDetails = new List<JournalEntryDetail>()
                };

                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = debitAccountId,
                    Debit = amount,
                    Credit = 0,
                    Description = "قبض قسط قرض"
                });

                entry.JournalEntryDetails.Add(new JournalEntryDetail
                {
                    AccountId = creditAccountId,
                    Debit = 0,
                    Credit = amount,
                    Description = $"سداد قسط رقم {receipt.Notes}"
                });

                BalanceEntry(entry);
                db.JournalEntries.Add(entry);
                db.SaveChanges(); // الحفظ هنا سيتم ضمن الترانزاكشن الخارجية

                return true;
            }
            catch (Exception ex)
            {
                AuditService.LogAction("AccountingError", "LoanRepayment", $"Error Receipt #{receiptId}: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            // ✅ التصحيح: نغلق قاعدة البيانات فقط إذا كنا نحن من أنشأناها
            // إذا تم تمريرها من الكنترولر (_shouldDispose == false)، لا نغلقها هنا
            if (_shouldDispose && db != null)
            {
                db.Dispose();
            }
        }
    }
}