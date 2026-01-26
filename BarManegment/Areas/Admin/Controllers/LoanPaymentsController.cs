using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")] // صلاحية عامة للعرض
    public class LoanPaymentsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // دالة مساعدة للحصول على معرف المستخدم الحالي
        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null) return -1;
            return (int)Session["UserId"];
        }

        // ============================================================
        // 1. الصفحة الرئيسية (بحث عن محامي)
        // ============================================================
        public ActionResult Index(string searchString)
        {
            // عرض المحامين المزاولين والمتدربين لأن القروض قد تكون للموظفين أيضاً مستقبلاً
            var query = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Where(g => g.ApplicationStatus.Name.Contains("محامي") || g.ApplicationStatus.Name.Contains("متدرب"));

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.ArabicName.Contains(searchString) ||
                                         l.MembershipId == searchString ||
                                         l.NationalIdNumber == searchString);
            }

            ViewBag.SearchString = searchString;
            return View(query.OrderBy(l => l.ArabicName).ToList());
        }

        // دالة AJAX للبحث عن المحامين (Select2)
        [HttpGet]
        public JsonResult SearchLawyers(string term)
        {
            if (string.IsNullOrEmpty(term))
                return Json(null, JsonRequestBehavior.AllowGet);

            var results = db.GraduateApplications
                .Where(g => g.ArabicName.Contains(term) ||
                            g.MembershipId.Contains(term) ||
                            g.NationalIdNumber.Contains(term))
                .Select(g => new
                {
                    id = g.Id,
                    text = g.ArabicName + " (" + (g.MembershipId ?? g.NationalIdNumber) + ")"
                })
                .Take(20)
                .ToList();

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        // أكشن التوجيه بعد اختيار المحامي من القائمة المنسدلة
        [HttpPost]
        public ActionResult GoToInstallments(int lawyerId)
        {
            return RedirectToAction("LawyerInstallments", new { id = lawyerId });
        }

        // ============================================================
        // 2. صفحة عرض الأقساط المستحقة للمحامي
        // ============================================================
        public ActionResult LawyerInstallments(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var lawyer = db.GraduateApplications.Find(id);
            if (lawyer == null) return HttpNotFound();

            // جلب الأقساط غير المدفوعة (IsPaid = false) بغض النظر عن حالة القسيمة
            var installments = db.LoanInstallments
                .Include(i => i.LoanApplication)
                .Include(i => i.LoanApplication.LoanType)
                .Include(i => i.PaymentVoucher)
                .Where(i => i.LoanApplication.LawyerId == id && !i.IsPaid)
                .OrderBy(i => i.DueDate)
                .ToList();

            ViewBag.Lawyer = lawyer;

            // نقل رسائل النجاح والخطأ من TempData إلى ViewBag للعرض
            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = TempData["SuccessMessage"];
            if (TempData["ErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ErrorMessage"];
            if (TempData["PrintReceiptUrl"] != null) ViewBag.PrintReceiptUrl = TempData["PrintReceiptUrl"];

            return View(installments);
        }

        // ============================================================
        // 3. شاشة سداد القسط (GET)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")] // صلاحية التحصيل
        public ActionResult CreateReceipt(int? id) // id هنا هو LoanInstallmentId
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var installment = db.LoanInstallments
                .Include(i => i.LoanApplication.Lawyer)
                .Include(i => i.LoanApplication.LoanType)
                .Include(i => i.PaymentVoucher)
                .FirstOrDefault(i => i.Id == id);

            if (installment == null) return HttpNotFound();

            // التحقق مرة أخرى لضمان عدم الدفع المكرر
            if (installment.IsPaid)
            {
                TempData["ErrorMessage"] = "هذا القسط تم سداده مسبقاً.";
                return RedirectToAction("LawyerInstallments", new { id = installment.LoanApplication.LawyerId });
            }

            // إعداد الـ ViewModel
            var viewModel = new CreateLoanReceiptViewModel
            {
                InstallmentId = installment.Id,
                LawyerName = installment.LoanApplication.Lawyer.ArabicName,
                Amount = installment.Amount,
                // نأخذ VoucherId إذا وجد، وإلا 0
                VoucherId = installment.PaymentVoucherId ?? 0,
                BankPaymentDate = DateTime.Now,
                Description = $"سداد قسط قرض {installment.LoanApplication.LoanType.Name} - قسط رقم {installment.InstallmentNumber} مستحق بتاريخ {installment.DueDate:yyyy/MM/dd}"
            };

            return View(viewModel);
        }

        // ============================================================
        // 4. تنفيذ السداد والحفظ (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateReceipt(CreateLoanReceiptViewModel viewModel)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserName = Session["FullName"] as string;

            if (currentUserId == -1) return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });

            // 1. التحقق الأولي من القسط
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
                    // 2. إنشاء سجل الإيصال (Receipt)
                    int currentYear = viewModel.BankPaymentDate.Year;

                    // حساب التسلسل اليدوي للإيصالات لهذه السنة
                    int lastSeq = db.Receipts
                        .Where(r => r.Year == currentYear)
                        .Select(r => (int?)r.SequenceNumber)
                        .Max() ?? 0;

                    // إذا لم تكن هناك قسيمة مرتبطة (حالة نادرة)، ننشئ واحدة وهمية أو نربط مباشرة حسب التصميم
                    // هنا سنفترض أن PaymentVoucherId قد يكون null في الموديل، ولكن يفضل وجود قسيمة.
                    // إذا كان التصميم يجبر وجود قسيمة، يجب التعامل مع ذلك. هنا سنعتمد على PaymentVoucherId الموجود في القسط.

                    if (installment.PaymentVoucherId == null)
                    {
                        // في حالة عدم وجود قسيمة (مثلاً تم إنشاء الأقساط بدون قسائم)، يمكن إنشاء قسيمة "فورية" هنا
                        // أو رمي استثناء إذا كان النظام يتطلب قسيمة مسبقة
                        // للتبسيط: سنفترض وجود قسيمة أو أن الحقل في Receipt يقبل null (حسب تعديلاتك الأخيرة)
                        // ولكن الأصح محاسبياً هو وجود قسيمة استحقاق.
                    }

                    var receipt = new Receipt
                    {
                        Year = currentYear,
                        SequenceNumber = lastSeq + 1,

                        // بيانات الدفع البنكي
                        BankPaymentDate = viewModel.BankPaymentDate,
                        BankReceiptNumber = viewModel.BankReceiptNumber,

                        // بيانات النظام والموظف
                        CreationDate = DateTime.Now,
                        IssuedByUserId = currentUserId,
                        IssuedByUserName = currentUserName,

                        // تخزين الوصف في الملاحظات
                        Notes = viewModel.Description,

                        // الربط
                        PaymentVoucherId = installment.PaymentVoucherId.Value
                        // ملاحظة: Id في جدول Receipts هو ForeignKey لـ PaymentVoucherId (علاقة 1:1)
                        // لذا يجب تعيين Id يدوياً إذا كانت الخاصية [Key, ForeignKey]
                        // أو تركها لـ EF إذا كانت علاقة Navigation
                    };

                    // هام: إذا كان Id هو الـ Key وهو ForeignKey، يجب تعيينه صراحة
                    receipt.Id = installment.PaymentVoucherId.Value;

                    db.Receipts.Add(receipt);
                    db.SaveChanges();

                    // 3. تحديث القسط (LoanInstallment)
                    installment.IsPaid = true;
                    installment.Status = "مدفوع";
                    installment.ReceiptId = receipt.Id; // ربط القسط بالإيصال
                    db.Entry(installment).State = EntityState.Modified;

                    // 4. تحديث القسيمة (PaymentVoucher) إذا وجدت
                    if (installment.PaymentVoucherId.HasValue)
                    {
                        var voucher = db.PaymentVouchers.Find(installment.PaymentVoucherId);
                        if (voucher != null)
                        {
                            voucher.IsPaid = true; // الخاصية التي أضفناها
                            voucher.Status = "Paid";
                            db.Entry(voucher).State = EntityState.Modified;
                        }
                    }

                    db.SaveChanges(); // حفظ التعديلات على القسط والقسيمة

                    // 5. إنشاء القيد المحاسبي (Accounting Entry)
                    // من ح/ البنك -> إلى ح/ ذمم القروض
                    bool entryCreated = false;
                    using (var accService = new AccountingService())
                    {
                        // دالة مخصصة لسداد القروض تميز نوع القرض والحساب الدائن
                        entryCreated = accService.GenerateEntryForLoanRepayment(
                            receipt.Id,
                            installment.LoanApplication.LoanTypeId,
                            currentUserId
                        );
                    }

                    if (!entryCreated)
                    {
                        TempData["WarningMessage"] = "تم السداد بنجاح، ولكن تعذر إنشاء القيد المحاسبي تلقائياً.";
                    }

                    transaction.Commit();

                    TempData["SuccessMessage"] = $"تم سداد القسط بنجاح. رقم الإيصال: {receipt.SequenceNumber}/{receipt.Year}";
                    TempData["PrintReceiptUrl"] = Url.Action("PrintLoanInstallmentReceipt", new { id = receipt.Id });

                    return RedirectToAction("LawyerInstallments", new { id = installment.LoanApplication.LawyerId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                    // إعادة تعبئة البيانات للفيو
                    var lawyer = db.GraduateApplications.Find(installment.LoanApplication.LawyerId);
                    viewModel.LawyerName = lawyer?.ArabicName ?? "";
                    return View(viewModel);
                }
            }
        }

        // ============================================================
        // 5. طباعة الإيصال (Print)
        // ============================================================
        public ActionResult PrintLoanInstallmentReceipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var receipt = db.Receipts.Find(id);
            if (receipt == null) return HttpNotFound();

            // جلب بيانات القسط المرتبط بهذا الإيصال لطباعتها
            var installment = db.LoanInstallments
                .Include(i => i.LoanApplication.LoanType)
                .Include(i => i.LoanApplication.Lawyer)
                .Include(i => i.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency)) // لجلب العملة
                .FirstOrDefault(i => i.ReceiptId == id);

            if (installment == null) return HttpNotFound("القسط المرتبط بالإيصال غير موجود.");

            // تحديد العملة والمبلغ
            string currencySymbol = "₪";
            if (installment.PaymentVoucher != null && installment.PaymentVoucher.VoucherDetails.Any())
            {
                var detail = installment.PaymentVoucher.VoucherDetails.FirstOrDefault();
                if (detail != null && detail.FeeType != null && detail.FeeType.Currency != null)
                {
                    currencySymbol = detail.FeeType.Currency.Symbol;
                }
            }

            // تعبئة ViewModel الطباعة
            var viewModel = new PrintLoanReceiptViewModel
            {
                ReceiptId = receipt.Id,
                ReceiptFullNumber = $"{receipt.SequenceNumber}/{receipt.Year}",
                PaymentDate = receipt.BankPaymentDate, // تاريخ البنك هو التاريخ المعتمد للدفع
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