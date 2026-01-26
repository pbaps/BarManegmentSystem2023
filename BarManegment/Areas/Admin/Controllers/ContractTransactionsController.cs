using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Tafqeet; // تأكد من وجود هذه المكتبة أو الكلاس

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class ContractTransactionsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ثوابت للحالات
        private const string STATUS_PENDING_PAYMENT = "بانتظار الدفع";
        private const string STATUS_PENDING_CERTIFICATION = "بانتظار التصديق";
        private const string STATUS_COMPLETED = "مكتمل";
        private const string STATUS_EXEMPT = "معفى (مكتمل)";

        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null) return -1;
            return (int)Session["UserId"];
        }

        // دالة مساعدة لجلب الإعدادات
        private int? GetSettingOrFindByName<T>(string settingKey, string nameToFind) where T : class
        {
            var setting = db.SystemSettings.FirstOrDefault(s => s.SettingKey == settingKey);
            if (setting != null && setting.ValueInt.HasValue) return setting.ValueInt.Value;

            if (typeof(T) == typeof(FeeType))
            {
                var item = db.FeeTypes.FirstOrDefault(f => f.Name.Contains(nameToFind));
                return item?.Id;
            }
            if (typeof(T) == typeof(ContractType))
            {
                var item = db.ContractTypes.FirstOrDefault(c => c.Name.Contains(nameToFind));
                return item?.Id;
            }
            return null;
        }

        // 1. Index
        public ActionResult Index(string searchString)
        {
            var query = db.ContractTransactions.AsNoTracking()
                .Include(c => c.Lawyer)
                .Include(c => c.ContractType)
                .Include(c => c.Employee)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                query = query.Where(c => c.Id.ToString() == searchString ||
                                         c.Lawyer.ArabicName.Contains(searchString) ||
                                         c.ContractType.Name.Contains(searchString));
            }

            ViewBag.CurrentFilter = searchString;
            return View(query.OrderByDescending(c => c.TransactionDate).ToList());
        }

        // 2. Create (GET)
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            var viewModel = new ContractTransactionViewModel();
            LoadDropdowns(viewModel);
            return View(viewModel);
        }

        // 3. Create (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(ContractTransactionViewModel viewModel)
        {
            var employeeId = GetCurrentUserId();
            var employeeName = Session["FullName"] as string;

            if (employeeId == -1) return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });

            // التحقق من المحامي
            GraduateApplication lawyer = null;
            if (!string.IsNullOrWhiteSpace(viewModel.LawyerIdentifier))
            {
                lawyer = db.GraduateApplications
                    .Include(g => g.User).Include(g => g.ApplicationStatus)
                    .FirstOrDefault(g => g.User.Username == viewModel.LawyerIdentifier || g.User.IdentificationNumber == viewModel.LawyerIdentifier || g.MembershipId == viewModel.LawyerIdentifier);
            }

            if (lawyer == null) ModelState.AddModelError("LawyerIdentifier", "لم يتم العثور على محامي.");
            else if (!LawyerStatusHelper.IsActiveLawyer(lawyer)) ModelState.AddModelError("LawyerIdentifier", "المحامي غير فعال.");

            if (viewModel.Parties == null || !viewModel.Parties.Any()) ModelState.AddModelError("Parties", "يجب إضافة طرف واحد على الأقل.");

            // جلب إعدادات الرسوم
            int? feeTypeId = GetSettingOrFindByName<FeeType>("Contract_FeeTypeId", "رسوم تصديق عقد");
            var contractFeeType = db.FeeTypes.Find(feeTypeId);
            if (contractFeeType == null && !viewModel.IsExempt) ModelState.AddModelError("", "نوع الرسم غير موجود.");

            // التحقق إذا كان وكالة جواز سفر
            int? passportTypeId = GetSettingOrFindByName<ContractType>("Contract_PassportAgencyTypeId", "وكالة جواز سفر");
            var selectedContractType = db.ContractTypes.Find(viewModel.ContractTypeId);
            bool isPassportAgency = (selectedContractType != null && selectedContractType.Id == passportTypeId);

            if (!ModelState.IsValid)
            {
                LoadDropdowns(viewModel);
                return View(viewModel);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var newTransaction = new ContractTransaction
                    {
                        TransactionDate = viewModel.TransactionDate,
                        LawyerId = lawyer.Id,
                        ContractTypeId = viewModel.ContractTypeId,
                        FinalFee = viewModel.FinalFee,
                        DeclaredValue = viewModel.ContractValue, // حفظ القيمة المصرح بها
                        IsExempt = viewModel.IsExempt,
                        ExemptionReasonId = viewModel.IsExempt ? viewModel.ExemptionReasonId : null,
                        Notes = viewModel.Notes,
                        EmployeeId = employeeId,
                        Status = viewModel.IsExempt ? STATUS_EXEMPT : STATUS_PENDING_PAYMENT,
                        IsActingForSelf = viewModel.IsActingForSelf,
                        AgentLegalCapacity = viewModel.AgentLegalCapacity
                    };

                    if (viewModel.IsExempt)
                    {
                        newTransaction.CertificationDate = DateTime.Now;
                    }
                    else
                    {
                        // إنشاء قسيمة الدفع
                        var newVoucher = new PaymentVoucher
                        {
                            GraduateApplicationId = lawyer.Id,
                            TotalAmount = viewModel.FinalFee,
                            Status = "صادر",
                            IssueDate = DateTime.Now,
                            ExpiryDate = DateTime.Now.AddDays(14),
                            IssuedByUserId = employeeId,
                            IssuedByUserName = employeeName,
                            PaymentMethod = "نقدي" // القيمة الافتراضية
                        };

                        var newDetail = new VoucherDetail
                        {
                            PaymentVoucher = newVoucher,
                            FeeTypeId = contractFeeType.Id,
                            Amount = viewModel.FinalFee,
                            Description = $"رسوم تصديق: {selectedContractType.Name}",
                            BankAccountId = contractFeeType.BankAccountId
                        };
                        db.VoucherDetails.Add(newDetail);
                        newTransaction.PaymentVoucher = newVoucher;
                    }

                    // إضافة الأطراف
                    foreach (var partyVM in viewModel.Parties)
                    {
                        db.TransactionParties.Add(new TransactionParty
                        {
                            ContractTransaction = newTransaction,
                            PartyType = partyVM.PartyType,
                            PartyName = partyVM.PartyName,
                            PartyIDNumber = partyVM.PartyIDNumber,
                            ProvinceId = partyVM.ProvinceId,
                            PartyRoleId = partyVM.PartyRoleId
                        });
                    }

                    // إضافة القصر (إذا كانت وكالة جواز سفر)
                    if (isPassportAgency && viewModel.Minors != null)
                    {
                        foreach (var minorVM in viewModel.Minors)
                        {
                            if (!string.IsNullOrWhiteSpace(minorVM.MinorName))
                            {
                                db.PassportMinors.Add(new PassportMinor
                                {
                                    ContractTransaction = newTransaction,
                                    MinorName = minorVM.MinorName,
                                    MinorIDNumber = minorVM.MinorIDNumber,
                                    MinorRelationshipId = minorVM.MinorRelationshipId
                                });
                            }
                        }
                    }

                    db.ContractTransactions.Add(newTransaction);
                    db.SaveChanges();
                    transaction.Commit();

                    AuditService.LogAction("Create Contract", "ContractTransactions", $"Contract #{newTransaction.Id} created.");

                    if (viewModel.IsExempt)
                        TempData["SuccessMessage"] = "تم حفظ المعاملة المعفاة بنجاح.";
                    else
                    {
                        TempData["SuccessMessage"] = "تم إنشاء القسيمة بنجاح.";
                        TempData["PrintVoucherUrl"] = Url.Action("PrintContractVoucher", "ContractTransactions", new { id = newTransaction.PaymentVoucherId });
                    }

                    if (isPassportAgency)
                        TempData["PrintAgencyUrl"] = Url.Action("PrintPassportAgency", "ContractTransactions", new { id = newTransaction.Id });

                    return RedirectToAction("Create");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "خطأ: " + ex.Message);
                    LoadDropdowns(viewModel);
                    return View(viewModel);
                }
            }
        }

        // 4. Confirm Cash Payment (التحصيل النقدي)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult ConfirmCashPayment(int PaymentVoucherId)
        {
            // ... (نفس كود ConfirmCashPayment الذي اعتمدناه سابقاً في PaymentVouchersController) ...
            // يفضل استدعاء المنطق الموجود هناك أو نقله لخدمة مشتركة، ولكن للاختصار سأعتبر أنك تستخدم زر يوجه لـ PaymentVouchers/ConfirmCashPayment
            return RedirectToAction("ConfirmCashPayment", "PaymentVouchers", new { PaymentVoucherId = PaymentVoucherId });
        }

        // Action للزر في الواجهة (GET)
        public ActionResult ConfirmCashPayment(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            // توجيه للمتحكم المختص بالمدفوعات
            return RedirectToAction("ConfirmCashPayment", "PaymentVouchers", new { id = id });
        }

        // 5. Upload Scan
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public JsonResult UploadScan(int id, HttpPostedFileBase ScannedFile)
        {
            var transaction = db.ContractTransactions.Find(id);
            if (transaction == null) return Json(new { success = false, message = "المعاملة غير موجودة." });

            if (ScannedFile == null || ScannedFile.ContentLength == 0)
                return Json(new { success = false, message = "الرجاء اختيار ملف." });

            try
            {
                string fileExtension = Path.GetExtension(ScannedFile.FileName);
                string fileName = $"Contract-{transaction.Id}-{Guid.NewGuid()}{fileExtension}";
                string path = Server.MapPath("~/Uploads/Contracts/");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                ScannedFile.SaveAs(Path.Combine(path, fileName));

                transaction.ScannedContractPath = $"/Uploads/Contracts/{fileName}";
                transaction.Status = STATUS_COMPLETED;
                transaction.CertificationDate = DateTime.Now;

                db.Entry(transaction).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true, redirectUrl = Url.Action("Index") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ: " + ex.Message });
            }
        }

        // UploadScan GET
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UploadScan(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var transaction = db.ContractTransactions.Find(id);
            if (transaction == null) return HttpNotFound();
            return View(transaction);
        }

        // 6. Print Functions
        // الدالة المساعدة الموحدة
        private ContractPrintViewModel GetPrintViewModel(int keyId, bool isReceipt = false, bool isTransactionId = false)
        {
            ContractTransaction transaction = null;
            PaymentVoucher voucher = null;
            Receipt receipt = null;

            if (isTransactionId)
            {
                transaction = db.ContractTransactions
                    .Include(t => t.ContractType.Currency).Include(t => t.Lawyer.User).Include(t => t.Employee)
                    .Include(t => t.Parties.Select(p => p.Province)).Include(t => t.Parties.Select(p => p.PartyRole))
                    .Include(t => t.Minors.Select(m => m.MinorRelationship))
                    .FirstOrDefault(t => t.Id == keyId);

                if (transaction != null && transaction.PaymentVoucherId.HasValue)
                    voucher = db.PaymentVouchers.Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency)).FirstOrDefault(v => v.Id == transaction.PaymentVoucherId);
            }
            else if (isReceipt)
            {
                receipt = db.Receipts.Find(keyId);
                if (receipt != null)
                {
                    voucher = db.PaymentVouchers.Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency)).FirstOrDefault(v => v.Id == keyId);
                    if (voucher != null) transaction = db.ContractTransactions.Include(t => t.ContractType).Include(t => t.Lawyer.User).Include(t => t.Employee).FirstOrDefault(t => t.PaymentVoucherId == voucher.Id);
                }
            }
            else // Voucher ID
            {
                voucher = db.PaymentVouchers.Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency)).FirstOrDefault(v => v.Id == keyId);
                if (voucher != null) transaction = db.ContractTransactions.Include(t => t.ContractType).Include(t => t.Lawyer.User).Include(t => t.Employee).FirstOrDefault(t => t.PaymentVoucherId == voucher.Id);
            }

            if (transaction == null) return null;

            // تجهيز البيانات
            string currencySymbol = voucher?.VoucherDetails.FirstOrDefault()?.FeeType.Currency.Symbol ?? "₪";
            decimal amount = voucher?.TotalAmount ?? transaction.FinalFee;
            string amountWords = TafqeetHelper.ConvertToArabic(amount, currencySymbol);

            return new ContractPrintViewModel
            {
                TransactionId = transaction.Id,
                TransactionDate = transaction.TransactionDate,
                LawyerName = transaction.Lawyer.ArabicName,
                LawyerMembershipId = transaction.Lawyer.MembershipId,
                ContractTypeName = transaction.ContractType.Name,
                EmployeeName = transaction.Employee.FullNameArabic,
                VoucherId = voucher?.Id ?? 0,
                TotalAmount = amount,
                TotalAmountInWords = amountWords,
                CurrencySymbol = currencySymbol,
                ReceiptFullNumber = receipt != null ? $"{receipt.SequenceNumber}/{receipt.Year}" : "N/A",
                PaymentDate = receipt?.BankPaymentDate,
                BankReceiptNumber = receipt?.BankReceiptNumber,
                Parties = transaction.Parties.ToList(),
                Minors = transaction.Minors.ToList(),
                IsActingForSelf = transaction.IsActingForSelf,
                AgentLegalCapacity = transaction.AgentLegalCapacity,
                Details = voucher?.VoucherDetails.ToList() ?? new List<VoucherDetail>()
            };
        }

        public ActionResult PrintContractVoucher(int id)
        {
            var model = GetPrintViewModel(id, false, false);
            if (model == null) return HttpNotFound();
            return View("PrintContractVoucher", model);
        }

        public ActionResult PrintContractReceipt(int id)
        {
            var model = GetPrintViewModel(id, true, false);
            if (model == null) return HttpNotFound();
            return View("PrintContractReceipt", model);
        }

        public ActionResult PrintPassportAgency(int id)
        {
            var model = GetPrintViewModel(id, false, true); // True لأن الـ ID هو TransactionId
            if (model == null) return HttpNotFound();
            return View("PrintPassportAgency", model);
        }

        public ActionResult ViewScannedContract(int id)
        {
            var t = db.ContractTransactions.Find(id);
            if (t == null || string.IsNullOrEmpty(t.ScannedContractPath)) return HttpNotFound();
            return File(Server.MapPath(t.ScannedContractPath), MimeMapping.GetMimeMapping(t.ScannedContractPath));
        }

        // Helpers
        [HttpGet]
        public JsonResult GetContractFee(int id)
        {
            var type = db.ContractTypes.Include(c => c.Currency).FirstOrDefault(c => c.Id == id);
            if (type == null) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, fee = type.DefaultFee, currency = type.Currency.Symbol, isFixed = type.IsFixedFee, percent = type.Percentage }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CheckLawyerStatus(string identifier)
        {
            var lawyer = db.GraduateApplications.Include(g => g.ApplicationStatus)
                .FirstOrDefault(g => g.User.Username == identifier || g.User.IdentificationNumber == identifier || g.MembershipId == identifier);

            if (lawyer == null) return Json(new { success = false, message = "غير موجود" });
            return Json(new { success = true, name = lawyer.ArabicName, status = lawyer.ApplicationStatus.Name, isActive = LawyerStatusHelper.IsActiveLawyer(lawyer) });
        }

        private void LoadDropdowns(ContractTransactionViewModel model)
        {
            ViewBag.ContractTypeId = new SelectList(db.ContractTypes, "Id", "Name", model.ContractTypeId);
            ViewBag.ExemptionReasonId = new SelectList(db.ContractExemptionReasons, "Id", "Reason", model.ExemptionReasonId);
            ViewBag.ProvincesList = new SelectList(db.Provinces, "Id", "Name");
            ViewBag.PartyRolesList = new SelectList(db.PartyRoles, "Id", "Name");
            ViewBag.MinorRelationshipsList = new SelectList(db.MinorRelationships, "Id", "Name");
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}