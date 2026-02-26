using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Tafqeet;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class LoanPaymentsController : BaseController
    {
        // 1. تعريف السياق مرة واحدة
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null) return -1;
            return (int)Session["UserId"];
        }

        // ... (دالة Index و LawyerInstallments و CreateReceipt GET كما هي تماماً) ...
        // سأركز على دالة POST التي بها المشكلة

        public ActionResult Index(string searchString)
        {
            var query = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Where(g => g.LoanApplications.Any(l => l.IsDisbursed));

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.ArabicName.Contains(searchString) ||
                                         l.MembershipId == searchString ||
                                         l.NationalIdNumber == searchString);
            }

            ViewBag.SearchString = searchString;
            return View(query.OrderBy(l => l.ArabicName).Take(50).ToList());
        }

        public ActionResult LawyerInstallments(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var lawyer = db.GraduateApplications.Find(id);
            if (lawyer == null) return HttpNotFound();

            var installments = db.LoanInstallments
                .Include(i => i.LoanApplication)
                .Include(i => i.LoanApplication.LoanType)
                .Include(i => i.PaymentVoucher)
                .Include(i => i.Receipt)
                .Where(i => i.LoanApplication.LawyerId == id)
                .OrderBy(i => i.DueDate)
                .ToList();

            ViewBag.Lawyer = lawyer;

            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = TempData["SuccessMessage"];
            if (TempData["ErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ErrorMessage"];
            if (TempData["PrintReceiptUrl"] != null) ViewBag.PrintReceiptUrl = TempData["PrintReceiptUrl"];

            return View(installments);
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateReceipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var installment = db.LoanInstallments
                .Include(i => i.LoanApplication.Lawyer)
                .Include(i => i.LoanApplication.LoanType)
                .Include(i => i.PaymentVoucher)
                .FirstOrDefault(i => i.Id == id);

            if (installment == null) return HttpNotFound();

            if (installment.IsPaid)
            {
                TempData["ErrorMessage"] = "هذا القسط تم سداده مسبقاً.";
                return RedirectToAction("LawyerInstallments", new { id = installment.LoanApplication.LawyerId });
            }

            var viewModel = new CreateLoanReceiptViewModel
            {
                InstallmentId = installment.Id,
                LawyerName = installment.LoanApplication.Lawyer.ArabicName,
                Amount = installment.Amount,
                VoucherId = installment.PaymentVoucherId ?? 0,
                BankPaymentDate = DateTime.Now,
                Description = $"سداد قسط قرض {installment.LoanApplication.LoanType.Name} - قسط رقم {installment.InstallmentNumber}"
            };

            return View(viewModel);
        }

        // ============================================================
        // 4. تنفيذ السداد والحفظ (POST) - المصححة بالكامل
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateReceipt(CreateLoanReceiptViewModel viewModel)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserName = Session["FullName"] as string ?? "System";

            if (currentUserId == -1) return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });

            // استخدام Find للأداء الأفضل والتحقق المباشر
            var installment = db.LoanInstallments
                .Include(i => i.LoanApplication)
                .Include(i => i.PaymentVoucher)
                .FirstOrDefault(i => i.Id == viewModel.InstallmentId);

            if (installment == null) return HttpNotFound();

            if (installment.IsPaid)
            {
                TempData["ErrorMessage"] = "تم سداد هذا القسط بالفعل.";
                return RedirectToAction("LawyerInstallments", new { id = installment.LoanApplication.LawyerId });
            }

            if (!ModelState.IsValid)
            {
                var lawyer = db.GraduateApplications.Find(installment.LoanApplication.LawyerId);
                viewModel.LawyerName = lawyer?.ArabicName ?? "";
                return View(viewModel);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int paymentVoucherIdToUse;

                    // 1. معالجة القسيمة
                    if (installment.PaymentVoucherId == null)
                    {
                        var loanFeeType = db.FeeTypes.FirstOrDefault(f => f.Name.Contains("قرض") || f.Name.Contains("سداد"));
                        if (loanFeeType == null) loanFeeType = db.FeeTypes.FirstOrDefault();

                        int defaultBankAccountId = loanFeeType?.BankAccountId ?? 1;

                        var newVoucher = new PaymentVoucher
                        {
                            GraduateApplicationId = installment.LoanApplication.LawyerId,
                            IssueDate = DateTime.Now,
                            ExpiryDate = DateTime.Now.AddDays(1),
                            Status = "مسدد",
                            TotalAmount = installment.Amount,
                            PaymentMethod = "إيداع بنكي",
                            IssuedByUserId = currentUserId,
                            IssuedByUserName = currentUserName,
                            CheckNumber = "سداد فوري",
                            ReferenceNumber = viewModel.BankReceiptNumber,
                            VoucherDetails = new List<VoucherDetail>()
                        };

                        newVoucher.VoucherDetails.Add(new VoucherDetail
                        {
                            FeeTypeId = loanFeeType?.Id ?? 1,
                            Amount = installment.Amount,
                            Description = viewModel.Description,
                            BankAccountId = defaultBankAccountId
                        });

                        db.PaymentVouchers.Add(newVoucher);
                        db.SaveChanges(); // حفظ القسيمة للحصول على ID

                        paymentVoucherIdToUse = newVoucher.Id;
                        installment.PaymentVoucherId = newVoucher.Id; // ربط القسط بالقسيمة
                    }
                    else
                    {
                        paymentVoucherIdToUse = installment.PaymentVoucherId.Value;
                        var existingVoucher = db.PaymentVouchers.Find(paymentVoucherIdToUse);
                        if (existingVoucher != null)
                        {
                            existingVoucher.Status = "مسدد";
                            existingVoucher.PaymentMethod = "إيداع بنكي";
                            existingVoucher.ReferenceNumber = viewModel.BankReceiptNumber;
                            db.Entry(existingVoucher).State = EntityState.Modified;
                        }
                    }

                    // 2. إنشاء الإيصال
                    int currentYear = viewModel.BankPaymentDate.Year;
                    // إصلاح منطق التسلسل لتجنب القيم الفارغة
                    var maxSeq = db.Receipts.Where(r => r.Year == currentYear).Select(r => (int?)r.SequenceNumber).Max();
                    int lastSeq = maxSeq ?? 0;

                    var receipt = new Receipt
                    {
                        // هام: إذا كانت العلاقة 1:1 والـ ID مشترك، نستخدمه. وإلا نتركه للترقيم التلقائي
                        // Id = paymentVoucherIdToUse, // ⚠️ قم بإلغاء هذا السطر إذا كان Id هو Identity
                        Year = currentYear,
                        SequenceNumber = lastSeq + 1,
                        BankPaymentDate = viewModel.BankPaymentDate,
                        BankReceiptNumber = viewModel.BankReceiptNumber,
                        CreationDate = DateTime.Now,
                        IssuedByUserId = currentUserId,
                        IssuedByUserName = currentUserName,
                        Notes = viewModel.Description,
                        PaymentVoucherId = paymentVoucherIdToUse
                    };

                    db.Receipts.Add(receipt);
                    db.SaveChanges(); // حفظ الإيصال للحصول على ID

                    // 3. تحديث القسط
                    installment.IsPaid = true;
                    installment.Status = "مدفوع";
                    installment.ReceiptId = receipt.Id;
                    db.Entry(installment).State = EntityState.Modified;
                    db.SaveChanges();

                    // 4. الترحيل المحاسبي (نمرر الـ db الحالي لتجنب مشاكل الاتصال)
                    bool entryCreated = false;

                    // ✅✅✅ التصحيح هنا: استخدام الكونستركتور الذي يقبل Context ✅✅✅
                    using (var accService = new AccountingService(db))
                    {
                        entryCreated = accService.GenerateEntryForLoanRepayment(
                            receipt.Id,
                            installment.LoanApplication.LoanTypeId,
                            currentUserId
                        );
                    }

                    transaction.Commit();

                    TempData["SuccessMessage"] = $"تم سداد القسط بنجاح. إيصال رقم {receipt.SequenceNumber}";
                    if (!entryCreated) TempData["WarningMessage"] = "تم السداد ولكن فشل القيد المحاسبي الآلي. راجع سجل الأخطاء.";

                    TempData["PrintReceiptUrl"] = Url.Action("PrintLoanInstallmentReceipt", new { id = receipt.Id });

                    return RedirectToAction("LawyerInstallments", new { id = installment.LoanApplication.LawyerId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "حدث خطأ أثناء المعالجة: " + ex.Message);

                    var lawyer = db.GraduateApplications.Find(installment.LoanApplication.LawyerId);
                    viewModel.LawyerName = lawyer?.ArabicName ?? "";
                    return View(viewModel);
                }
            }
        }

        public ActionResult PrintLoanInstallmentReceipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var receipt = db.Receipts.Find(id);
            if (receipt == null) return HttpNotFound();

            // استخدام Explicit Loading لضمان جلب البيانات حتى لو كان التتبع متوقفاً
            db.Entry(receipt).Reference(r => r.PaymentVoucher).Load();
            if (receipt.PaymentVoucher != null)
            {
                db.Entry(receipt.PaymentVoucher).Collection(v => v.VoucherDetails).Load();
                foreach (var detail in receipt.PaymentVoucher.VoucherDetails)
                {
                    db.Entry(detail).Reference(d => d.FeeType).Load();
                    if (detail.FeeType != null) db.Entry(detail.FeeType).Reference(f => f.Currency).Load();
                }
            }

            var installment = db.LoanInstallments
                .Include(i => i.LoanApplication.LoanType)
                .Include(i => i.LoanApplication.Lawyer)
                .FirstOrDefault(i => i.ReceiptId == id);

            if (installment == null) return HttpNotFound("القسط المرتبط بالإيصال غير موجود.");

            string currencySymbol = "₪";
            if (receipt.PaymentVoucher?.VoucherDetails.FirstOrDefault()?.FeeType?.Currency != null)
            {
                currencySymbol = receipt.PaymentVoucher.VoucherDetails.First().FeeType.Currency.Symbol;
            }

            var viewModel = new PrintLoanReceiptViewModel
            {
                ReceiptId = receipt.Id,
                ReceiptFullNumber = $"{receipt.SequenceNumber}/{receipt.Year}",
                PaymentDate = receipt.BankPaymentDate,
                BankReceiptNumber = receipt.BankReceiptNumber,
                LoanId = installment.LoanApplicationId,
                LoanTypeName = installment.LoanApplication.LoanType.Name,
                InstallmentNumber = installment.InstallmentNumber,
                LawyerName = installment.LoanApplication.Lawyer.ArabicName,
                EmployeeName = receipt.IssuedByUserName,
                AmountPaid = installment.Amount,
                CurrencySymbol = currencySymbol,
                AmountInWords = TafqeetHelper.ConvertToArabic(installment.Amount, currencySymbol)
            };

            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}