
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class LoanPaymentsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null) return -1;
            return (int)Session["UserId"];
        }

        // --- 1. البحث عن المحامي ---
        // GET: Admin/LoanPayments
        public ActionResult Index(string searchString)
        {
            var query = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Where(g => g.ApplicationStatus.Name == "محامي مزاول");

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.ArabicName.Contains(searchString) ||
                                         l.MembershipId == searchString ||
                                         (l.User != null && l.User.IdentificationNumber == searchString));
            }

            ViewBag.SearchString = searchString;
            return View(query.OrderBy(l => l.ArabicName).ToList());
        }

        // --- 2. عرض الأقساط المستحقة للمحامي ---
        // GET: Admin/LoanPayments/LawyerInstallments/5
        public ActionResult LawyerInstallments(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var lawyer = db.GraduateApplications.Find(id);
            if (lawyer == null) return HttpNotFound();

            // (جلب الأقساط "المستحقة" (التي قسائمها "صادرة") فقط)
            var installments = db.LoanInstallments
                .Include(i => i.LoanApplication.LoanType)
                .Include(i => i.PaymentVoucher)
                .Where(i => i.LoanApplication.LawyerId == id && i.PaymentVoucher.Status == "صادر")
                .OrderBy(i => i.DueDate)
                .ToList();

            ViewBag.Lawyer = lawyer;
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            ViewBag.ErrorMessage = TempData["ErrorMessage"];

            return View(installments);
        }

        // --- 3. شاشة إدخال بيانات إيصال البنك ---
        // GET: Admin/LoanPayments/CreateReceipt/5
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateReceipt(int? id) // (id = LoanInstallmentId)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var installment = db.LoanInstallments
                .Include(i => i.LoanApplication.Lawyer)
                .Include(i => i.PaymentVoucher)
                .FirstOrDefault(i => i.Id == id);

            if (installment == null || installment.PaymentVoucher.Status != "صادر")
            {
                TempData["ErrorMessage"] = "القسط غير موجود أو تم تسديده بالفعل.";
                return RedirectToAction("Index");
            }

            // (إعداد نموذج العرض)
            var viewModel = new CreateLoanReceiptViewModel
            {
                InstallmentId = installment.Id,
                LawyerName = installment.LoanApplication.Lawyer.ArabicName,
                Amount = installment.Amount,
                VoucherId = installment.PaymentVoucherId.Value,
                BankPaymentDate = DateTime.Now
            };

            return View(viewModel);
        }

        // --- 4. حفظ الإيصال وتحديث القسط ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateReceipt(CreateLoanReceiptViewModel viewModel)
        {
            var employeeId = GetCurrentUserId();
            var employeeName = Session["FullName"] as string;

            if (employeeId == -1 || string.IsNullOrEmpty(employeeName))
            {
                return RedirectToAction("Login", "AdminLogin");
            }

            var installment = db.LoanInstallments
                .Include(i => i.LoanApplication)
                .Include(i => i.PaymentVoucher)
                .FirstOrDefault(i => i.Id == viewModel.InstallmentId);

            if (installment == null || installment.PaymentVoucher.Status != "صادر")
            {
                ModelState.AddModelError("", "القسط غير موجود أو تم سداده مسبقاً.");
            }

            if (!ModelState.IsValid)
            {
                // (إعادة ملء البيانات الأساسية عند فشل النموذج)
                var lawyer = db.GraduateApplications.Find(installment?.LoanApplication.LawyerId ?? 0);
                viewModel.LawyerName = lawyer?.ArabicName ?? "غير معروف";
                viewModel.Amount = installment?.Amount ?? 0;
                return View(viewModel);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. إنشاء الإيصال
                    int currentYear = viewModel.BankPaymentDate.Year;
                    int lastSequenceNumber = db.Receipts.Where(r => r.Year == currentYear).Select(r => (int?)r.SequenceNumber).Max() ?? 0;
                    int newSequenceNumber = lastSequenceNumber + 1;

                    var receipt = new Receipt
                    {
                        Id = installment.PaymentVoucherId.Value,
                        BankPaymentDate = viewModel.BankPaymentDate,
                        BankReceiptNumber = viewModel.BankReceiptNumber,
                        CreationDate = DateTime.Now,
                        IssuedByUserId = employeeId,
                        IssuedByUserName = employeeName,
                        Year = currentYear,
                        SequenceNumber = newSequenceNumber
                    };
                    db.Receipts.Add(receipt);
                    db.SaveChanges(); // (حفظ الإيصال للحصول على ID)

                    // 2. تحديث القسيمة
                    installment.PaymentVoucher.Status = "مسدد";
                    db.Entry(installment.PaymentVoucher).State = EntityState.Modified;

                    // 3. تحديث القسط
                    installment.Status = "مدفوع";
                    installment.ReceiptId = receipt.Id; // (ربط الإيصال بالقسط)
                    db.Entry(installment).State = EntityState.Modified;

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = $"تم تسجيل إيصال سداد القسط بنجاح برقم: {newSequenceNumber}/{currentYear}.";
                    // ============================================================
                    // === 💡 التكامل المالي: قيد سداد القرض 💡 ===
                    // ============================================================
                    try
                    {
                        using (var accService = new AccountingService())
                        {
                            // هنا القيد سيكون: من ح/ الصندوق -> إلى ح/ ذمم القروض (الأصل)
                            // يعتمد ذلك على إعدادات FeeType المرتبطة بالقسط
                            bool isEntryCreated = accService.GenerateEntryForReceipt(receipt.Id, employeeId);
                            if (isEntryCreated)
                                TempData["SuccessMessage"] += " وتم إنشاء القيد المحاسبي.";
                            else
                                TempData["WarningMessage"] = "تم الحفظ، ولكن فشل إنشاء القيد (راجع ربط رسم سداد القروض بالحسابات).";
                        }
                    }
                    catch (Exception ex)
                    {
                        // لا نوقف العملية لأن السند تم حفظه، فقط ننبه المستخدم
                        TempData["WarningMessage"] = "تم الحفظ، ولكن حدث خطأ في النظام المحاسبي: " + ex.Message;
                    }
                    // ============================================================
                    // (إرسال رابط طباعة الإيصال الجديد)
                    TempData["PrintReceiptUrl"] = Url.Action("PrintLoanInstallmentReceipt", new { id = receipt.Id });

                    return RedirectToAction("LawyerInstallments", new { id = installment.LoanApplication.LawyerId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                    var lawyer = db.GraduateApplications.Find(installment?.LoanApplication.LawyerId ?? 0);
                    viewModel.LawyerName = lawyer?.ArabicName ?? "غير معروف";
                    viewModel.Amount = installment?.Amount ?? 0;
                    return View(viewModel);
                }
            }
        }

        // --- 5. طباعة إيصال سداد القسط ---
        // GET: Admin/LoanPayments/PrintLoanInstallmentReceipt/5
        public ActionResult PrintLoanInstallmentReceipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var receipt = db.Receipts.Find(id);
            if (receipt == null) return HttpNotFound();

            var installment = db.LoanInstallments
                .Include(i => i.LoanApplication.Lawyer)
                .Include(i => i.LoanApplication.LoanType)
                .Include(i => i.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency))
                .FirstOrDefault(i => i.ReceiptId == id);

            if (installment == null)
            {
                return HttpNotFound("لا يمكن العثور على القسط المرتبط بهذا الإيصال.");
            }

            var currencySymbol = installment.PaymentVoucher.VoucherDetails.First().FeeType.Currency.Symbol;

            var viewModel = new PrintLoanReceiptViewModel
            {
                ReceiptFullNumber = $"{receipt.SequenceNumber}/{receipt.Year}",
                LawyerName = installment.LoanApplication.Lawyer.ArabicName,
                PaymentDate = receipt.BankPaymentDate,
                BankReceiptNumber = receipt.BankReceiptNumber,
                LoanId = installment.LoanApplicationId,
                LoanTypeName = installment.LoanApplication.LoanType.Name,
                InstallmentNumber = installment.InstallmentNumber,
                AmountPaid = installment.Amount,
                CurrencySymbol = currencySymbol,
                AmountInWords = TafqeetHelper.ConvertToArabic(installment.Amount, currencySymbol),
                EmployeeName = receipt.IssuedByUserName
            };

            return View(viewModel);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}