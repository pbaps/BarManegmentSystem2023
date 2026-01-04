using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class LawyerFinancialBoxController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/LawyerFinancialBox/Index (صفحة البحث)
        // GET: Admin/LawyerFinancialBox/Index
        public ActionResult Index(string searchString)
        {
            // إذا كان البحث فارغاً، ارجع صفحة فارغة (أو آخر 10 محامين مثلاً)
            if (string.IsNullOrWhiteSpace(searchString))
            {
                return View(new List<GraduateApplication>());
            }

            // البحث عن المحامين (بالاسم أو الرقم الوطني أو رقم العضوية)
            var lawyers = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Where(g => g.ArabicName.Contains(searchString) ||
                            g.NationalIdNumber.Contains(searchString) ||
                            g.MembershipId == searchString)
                .Where(g => g.ApplicationStatus.Name.Contains("محامي")) // (اختياري: حصر البحث بالمحامين فقط)
                .OrderBy(g => g.ArabicName)
                .Take(20) // (تحديد النتائج لتجنب التحميل الزائد)
                .ToList();

            ViewBag.CurrentSearch = searchString;
            return View(lawyers);
        }

        // GET: Admin/LawyerFinancialBox/Details/5 (الصفحة الرئيسية للصندوق)
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var lawyer = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .FirstOrDefault(g => g.Id == id);

            if (lawyer == null) return HttpNotFound();

            var viewModel = new LawyerFinancialBoxViewModel
            {
                LawyerId = lawyer.Id,
                LawyerName = lawyer.ArabicName,
                NationalId = lawyer.NationalIdNumber,
                Iban = lawyer.Iban ?? "غير مدخل",
                BankName = lawyer.BankName ?? "-"
            };

            // --- 1. جلب مستحقات التصديقات (FeeDistributions) ---
            var contractShares = db.FeeDistributions
                .Include(d => d.ContractTransaction.ContractType)
                .Include(d => d.Receipt)
                .Where(d => d.LawyerId == id && d.ShareType == "حصة محامي")
                .ToList();

            // --- 2. جلب مستحقات الطوابع (StampSales) ---
            var stampShares = db.StampSales
                .Include(s => s.Stamp)
                .Where(s => s.GraduateApplicationId == id)
                .ToList();

            // --- 3. جلب القروض والأقساط (LoanInstallments) ---
            // (نفترض وجود علاقة LoanApplication)
            var loans = db.LoanInstallments
                .Include(l => l.LoanApplication.LoanType)
                .Where(l => l.LoanApplication.LawyerId == id)
                .ToList();


            // --- 4. توحيد البيانات في القائمة (Data Mapping) ---

            // أ. إضافة التصديقات
            foreach (var item in contractShares)
            {
                string status = item.IsSentToBank ? "تم التحويل للبنك" : (item.IsOnHold ? "محجوز" : "بانتظار الدفع");
                string color = item.IsSentToBank ? "success" : (item.IsOnHold ? "danger" : "warning");

                viewModel.Transactions.Add(new FinancialTransactionItem
                {
                    OriginalId = item.Id,
                    SourceType = "تصديق عقد",
                    SourceTable = "FeeDistribution",
                    Date = item.Receipt?.BankPaymentDate ?? DateTime.MinValue,
                    Description = $"تصديق {item.ContractTransaction?.ContractType?.Name} - إيصال رقم {item.ReceiptId}",
                    CreditAmount = item.Amount,
                    DebitAmount = 0,
                    Status = status,
                    StatusColor = color,
                    IsHoldable = !item.IsSentToBank, // يمكن حجزه فقط إذا لم يرسل للبنك
                    IsOnHold = item.IsOnHold
                });
            }

            // ب. إضافة الطوابع
            foreach (var item in stampShares)
            {
                string status = item.IsPaidToLawyer ? "تم التحويل للبنك" : (item.IsOnHold ? "محجوز" : "بانتظار الدفع");
                string color = item.IsPaidToLawyer ? "success" : (item.IsOnHold ? "danger" : "warning");

                viewModel.Transactions.Add(new FinancialTransactionItem
                {
                    OriginalId = item.Id,
                    SourceType = "طوابع",
                    SourceTable = "StampSale",
                    Date = item.SaleDate,
                    Description = $"بيع طابع رقم {item.Stamp?.SerialNumber} - {item.StampValue} ₪",
                    CreditAmount = item.AmountToLawyer,
                    DebitAmount = 0,
                    Status = status,
                    StatusColor = color,
                    IsHoldable = !item.IsPaidToLawyer,
                    IsOnHold = item.IsOnHold
                });
            }

            // ج. إضافة القروض (كمديونية)
            foreach (var item in loans)
            {
                bool isPaid = (item.Status == "مسدد");
                viewModel.Transactions.Add(new FinancialTransactionItem
                {
                    OriginalId = item.Id,
                    SourceType = "قرض",
                    SourceTable = "Loan",
                    Date = item.DueDate,
                    Description = $"قسط رقم {item.InstallmentNumber} - {item.LoanApplication.LoanType.Name}",
                    CreditAmount = 0,
                    DebitAmount = item.Amount,
                    Status = item.Status,
                    StatusColor = isPaid ? "success" : "danger",
                    IsHoldable = false,
                    IsOnHold = false
                });
            }

            // --- 5. حساب الملخصات ---
            // المستحقات المعلقة (تصديقات + طوابع غير محولة وغير محجوزة)
            viewModel.PendingBalance = contractShares.Where(x => !x.IsSentToBank && !x.IsOnHold).Sum(x => x.Amount) +
                                       stampShares.Where(x => !x.IsPaidToLawyer && !x.IsOnHold).Sum(x => x.AmountToLawyer);

            // المستحقات المحولة (تاريخياً)
            viewModel.TransferredBalance = contractShares.Where(x => x.IsSentToBank).Sum(x => x.Amount) +
                                           stampShares.Where(x => x.IsPaidToLawyer).Sum(x => x.AmountToLawyer);

            // المبالغ المحجوزة
            viewModel.HeldBalance = contractShares.Where(x => x.IsOnHold && !x.IsSentToBank).Sum(x => x.Amount) +
                                    stampShares.Where(x => x.IsOnHold && !x.IsPaidToLawyer).Sum(x => x.AmountToLawyer);

            // المديونية (أقساط غير مسددة)
            viewModel.TotalLoanDebt = loans.Where(x => x.Status != "مسدد").Sum(x => x.Amount);


            // ترتيب الحركات حسب التاريخ (الأحدث أولاً)
            viewModel.Transactions = viewModel.Transactions.OrderByDescending(x => x.Date).ToList();

            return View(viewModel);
        }

        // POST: تغيير حالة الحجز (Hold/Unhold)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ToggleHold(int id, string type, string reason, int lawyerId)
        {
            if (type == "FeeDistribution")
            {
                var item = db.FeeDistributions.Find(id);
                if (item != null && !item.IsSentToBank)
                {
                    item.IsOnHold = !item.IsOnHold; // عكس الحالة
                    item.HoldReason = item.IsOnHold ? reason : null;
                    db.SaveChanges();
                }
            }
            else if (type == "StampSale")
            {
                var item = db.StampSales.Find(id);
                if (item != null && !item.IsPaidToLawyer)
                {
                    item.IsOnHold = !item.IsOnHold; // عكس الحالة
                    item.HoldReason = item.IsOnHold ? reason : null;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Details", new { id = lawyerId });
        }

        // GET: تصدير كشف حساب Excel
        public ActionResult ExportStatement(int id)
        {
            // (يمكن نسخ منطق Details هنا وإنشاء ملف Excel يحتوي على جدول Transactions)
            // للتبسيط سأقوم بإعادة توجيه للـ Details حالياً
            return RedirectToAction("Details", new { id = id });
        }
    }
}