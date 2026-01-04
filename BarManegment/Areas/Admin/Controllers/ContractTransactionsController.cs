using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Tafqeet;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class ContractTransactionsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private const string CONTRACT_FEE_NAME = "رسوم تصديق عقد";
        private const string CONTRACT_PASSPORT_AGENCY = "وكالة جواز سفر";
        private const string STATUS_PENDING_PAYMENT = "بانتظار الدفع";
        private const string STATUS_PENDING_CERTIFICATION = "بانتظار التصديق";
        private const string STATUS_COMPLETED = "مكتمل";
        private const string STATUS_EXEMPT = "معفى (مكتمل)";

        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null) return -1;
            return (int)Session["UserId"];
        }

        public ActionResult Index(string searchString)
        {
            var query = db.ContractTransactions.AsNoTracking()
                .Include(c => c.Lawyer)
                .Include(c => c.ContractType)
                .Include(c => c.Employee)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                query = query.Where(c =>
                    c.Id.ToString() == searchString ||
                    c.Lawyer.ArabicName.Contains(searchString) ||
                    c.ContractType.Name.Contains(searchString)
                );
            }
            ViewBag.CurrentFilter = searchString;
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            ViewBag.ErrorMessage = TempData["ErrorMessage"];
            ViewBag.PrintReceiptUrl = TempData["PrintReceiptUrl"];

            var transactions = query.OrderByDescending(c => c.TransactionDate).ToList();
            return View(transactions);
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            var viewModel = new ContractTransactionViewModel();
            LoadDropdowns(viewModel);

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            ViewBag.PrintVoucherUrl = TempData["PrintVoucherUrl"];
            ViewBag.PrintAgencyUrl = TempData["PrintAgencyUrl"];

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(ContractTransactionViewModel viewModel)
        {
            var employeeId = GetCurrentUserId();
            var employeeName = Session["FullName"] as string;

            if (employeeId == -1 || string.IsNullOrEmpty(employeeName))
                return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });

            GraduateApplication lawyer = null;
            if (!string.IsNullOrWhiteSpace(viewModel.LawyerIdentifier))
            {
                lawyer = db.GraduateApplications
                    .Include(g => g.User)
                    .Include(g => g.ApplicationStatus)
                    .FirstOrDefault(g =>
                        g.User.Username == viewModel.LawyerIdentifier ||
                        g.User.IdentificationNumber == viewModel.LawyerIdentifier ||
                        g.MembershipId == viewModel.LawyerIdentifier
                    );
            }

            if (lawyer == null)
            {
                ModelState.AddModelError("LawyerIdentifier", "لم يتم العثور على محامي بهذا المعرف.");
            }
            else
            {
                if (!LawyerStatusHelper.IsActiveLawyer(lawyer))
                {
                    string status = lawyer.ApplicationStatus?.Name ?? "غير معروف";
                    string msg = status == "بانتظار تجديد المزاولة"
                        ? $"المحامي حالته '{status}' وانتهت فترة السماح. يرجى تجديد الاشتراك أولاً."
                        : $"المحامي حالته '{status}' وغير فعال حالياً.";

                    ModelState.AddModelError("LawyerIdentifier", msg);
                }
            }

            if (viewModel.Parties == null || !viewModel.Parties.Any())
                ModelState.AddModelError("Parties", "يجب إضافة طرف واحد على الأقل للمعاملة.");

            var contractFeeType = db.FeeTypes.FirstOrDefault(f => f.Name == CONTRACT_FEE_NAME);
            if (contractFeeType == null && !viewModel.IsExempt)
                ModelState.AddModelError("", $"خطأ فادح: لم يتم العثور على نوع الرسم '{CONTRACT_FEE_NAME}'.");

            var selectedContractType = db.ContractTypes.Find(viewModel.ContractTypeId);
            bool isPassportAgency = (selectedContractType != null && selectedContractType.Name == CONTRACT_PASSPORT_AGENCY);

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
                        var newVoucher = new PaymentVoucher
                        {
                            GraduateApplicationId = lawyer.Id,
                            TotalAmount = viewModel.FinalFee,
                            Status = "صادر",
                            IssueDate = DateTime.Now,
                            ExpiryDate = DateTime.Now.AddDays(14),
                            IssuedByUserId = employeeId,
                            IssuedByUserName = employeeName
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

                    if (isPassportAgency && viewModel.Minors != null)
                    {
                        foreach (var minorVM in viewModel.Minors)
                        {
                            if (!string.IsNullOrWhiteSpace(minorVM.MinorName) && !string.IsNullOrWhiteSpace(minorVM.MinorIDNumber) && minorVM.MinorRelationshipId > 0)
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

                    AuditService.LogAction("Create Contract", "ContractTransactions", $"Contract #{newTransaction.Id} created by Lawyer {lawyer.ArabicName}.");

                    if (viewModel.IsExempt)
                    {
                        TempData["SuccessMessage"] = "تم حفظ المعاملة المعفاة بنجاح.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "تم إنشاء القسيمة بنجاح.";
                        TempData["PrintVoucherUrl"] = Url.Action("PrintContractVoucher", "ContractTransactions", new { id = newTransaction.PaymentVoucherId });
                    }

                    if (isPassportAgency)
                    {
                        // هنا نمرر ID المعاملة وليس القسيمة
                        TempData["PrintAgencyUrl"] = Url.Action("PrintPassportAgency", "ContractTransactions", new { id = newTransaction.Id });
                    }

                    return RedirectToAction("Create");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                    LoadDropdowns(viewModel);
                    return View(viewModel);
                }
            }
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult ConfirmCashPayment(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var voucher = db.PaymentVouchers.AsNoTracking()
                .Include(v => v.GraduateApplication)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                .FirstOrDefault(v => v.Id == id);

            if (voucher == null || voucher.Status != "صادر")
            {
                TempData["ErrorMessage"] = "القسيمة غير موجودة أو تم تسديدها بالفعل.";
                return RedirectToAction("Index");
            }
            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult ConfirmCashPayment(int PaymentVoucherId)
        {
            var employeeId = GetCurrentUserId();
            var employeeName = Session["FullName"] as string;

            if (employeeId == -1 || string.IsNullOrEmpty(employeeName))
                return RedirectToAction("Login", "AdminLogin");

            var paymentVoucher = db.PaymentVouchers
                .Include(v => v.GraduateApplication.ApplicationStatus)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType))
                .FirstOrDefault(v => v.Id == PaymentVoucherId);

            if (paymentVoucher == null || paymentVoucher.Status != "صادر")
            {
                TempData["ErrorMessage"] = "القسيمة غير موجودة أو تم سدادها بالفعل.";
                return RedirectToAction("Index");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int currentYear = DateTime.Now.Year;
                    int lastSequenceNumber = db.Receipts.Where(r => r.Year == currentYear).Select(r => (int?)r.SequenceNumber).Max() ?? 0;
                    int newSequenceNumber = lastSequenceNumber + 1;

                    var receipt = new Receipt
                    {
                        Id = paymentVoucher.Id,
                        BankPaymentDate = DateTime.Now,
                        BankReceiptNumber = "تحصيل نقدي",
                        CreationDate = DateTime.Now,
                        IssuedByUserId = employeeId,
                        IssuedByUserName = employeeName,
                        Year = currentYear,
                        SequenceNumber = newSequenceNumber
                    };
                    db.Receipts.Add(receipt);
                    db.SaveChanges();

                    paymentVoucher.Status = "مسدد";
                    db.Entry(paymentVoucher).State = EntityState.Modified;

                    var contractTransaction = db.ContractTransactions
                        .Include(c => c.ContractType)
                        .FirstOrDefault(c => c.PaymentVoucherId == paymentVoucher.Id);

                    if (contractTransaction != null)
                    {
                        contractTransaction.Status = STATUS_PENDING_CERTIFICATION;
                        db.Entry(contractTransaction).State = EntityState.Modified;

                        decimal lawyerShare = contractTransaction.FinalFee * contractTransaction.ContractType.LawyerPercentage;
                        decimal barShare = contractTransaction.FinalFee * contractTransaction.ContractType.BarSharePercentage;

                        db.FeeDistributions.Add(new FeeDistribution { ReceiptId = receipt.Id, ContractTransactionId = contractTransaction.Id, LawyerId = contractTransaction.LawyerId, Amount = lawyerShare, ShareType = "حصة محامي", IsSentToBank = false });
                        db.FeeDistributions.Add(new FeeDistribution { ReceiptId = receipt.Id, ContractTransactionId = contractTransaction.Id, LawyerId = null, Amount = barShare, ShareType = "حصة نقابة", IsSentToBank = true });
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    AuditService.LogAction("Cash Payment", "ContractTransactions", $"Received cash payment for Voucher #{PaymentVoucherId}. Receipt #{newSequenceNumber}.");
                    // ============================================================
                    // === 💡 التكامل المالي 💡 ===
                    // ============================================================
                    try
                    {
                        using (var accService = new AccountingService())
                        {
                            accService.GenerateEntryForReceipt(receipt.Id, employeeId);
                        }
                        TempData["SuccessMessage"] = $"تم تسجيل الإيصال النقدي {newSequenceNumber}/{currentYear} والقيد المحاسبي بنجاح.";
                    }
                    catch
                    {
                        TempData["SuccessMessage"] = $"تم تسجيل الإيصال {newSequenceNumber}، ولكن فشل القيد الآلي.";
                    }
                    // ============================================================
                    TempData["SuccessMessage"] = $"تم تسجيل الإيصال النقدي بنجاح برقم: {newSequenceNumber}/{currentYear}.";
                    TempData["PrintReceiptUrl"] = Url.Action("PrintContractReceipt", "ContractTransactions", new { id = receipt.Id });

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "حدث خطأ أثناء الحفظ: " + ex.Message;
                    return RedirectToAction("Index");
                }
            }
        }

        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UploadScan(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var transaction = db.ContractTransactions.Find(id);
            if (transaction == null) return HttpNotFound();

            if (transaction.Status != STATUS_PENDING_CERTIFICATION && transaction.Status != STATUS_EXEMPT)
            {
                TempData["ErrorMessage"] = "لا يمكن رفع الملف. المعاملة لم تدفع رسومها بعد أو مكتملة.";
                return RedirectToAction("Index");
            }
            return View(transaction);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public JsonResult UploadScan(int id, HttpPostedFileBase ScannedFile)
        {
            var transaction = db.ContractTransactions.Find(id);
            if (transaction == null) return Json(new { success = false, message = "المعاملة غير موجودة." });

            if (ScannedFile == null || ScannedFile.ContentLength == 0)
                return Json(new { success = false, message = "الرجاء اختيار ملف العقد المصدق." });

            try
            {
                string fileExtension = Path.GetExtension(ScannedFile.FileName);
                string fileName = $"Contract-{transaction.Id}-{Guid.NewGuid()}{fileExtension}";
                string path = Server.MapPath("~/Uploads/Contracts/");

                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string physicalPath = Path.Combine(path, fileName);
                ScannedFile.SaveAs(physicalPath);

                transaction.ScannedContractPath = $"/Uploads/Contracts/{fileName}";
                transaction.Status = STATUS_COMPLETED;
                transaction.CertificationDate = DateTime.Now;

                db.Entry(transaction).State = EntityState.Modified;
                db.SaveChanges();

                AuditService.LogAction("Upload Contract", "ContractTransactions", $"Uploaded scanned contract for Transaction #{id}.");

                TempData["SuccessMessage"] = "تم رفع الملف واكتمال المعاملة بنجاح.";
                return Json(new { success = true, redirectUrl = Url.Action("Index") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ أثناء رفع الملف: " + ex.Message });
            }
        }

        // --- 5. دوال الطباعة والعرض ---

        // 💡 التعديل الحاسم: إضافة معامل للبحث برقم المعاملة
        private ContractPrintViewModel GetPrintViewModel(int keyId, bool isReceipt = false, bool isTransactionId = false)
        {
            ContractTransaction transaction = null;
            PaymentVoucher voucher = null;
            Receipt receipt = null;

            if (isTransactionId)
            {
                // 1. البحث المباشر عن المعاملة (مطلوب لطباعة الوكالة)
                transaction = db.ContractTransactions
                    .Include(t => t.ContractType.Currency)
                    .Include(t => t.Lawyer.User)
                    .Include(t => t.Employee)
                    .Include(t => t.Parties.Select(p => p.Province))
                    .Include(t => t.Parties.Select(p => p.PartyRole))
                    .Include(t => t.Minors.Select(m => m.MinorRelationship))
                    .FirstOrDefault(t => t.Id == keyId);

                if (transaction != null && transaction.PaymentVoucherId.HasValue)
                {
                    voucher = db.PaymentVouchers
                        .Include(v => v.GraduateApplication.User)
                        .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                        .Include(v => v.VoucherDetails.Select(d => d.BankAccount))
                        .FirstOrDefault(v => v.Id == transaction.PaymentVoucherId);
                }
            }
            else if (isReceipt)
            {
                receipt = db.Receipts.Find(keyId);
                if (receipt == null) return null;
                voucher = db.PaymentVouchers
                    .Include(v => v.GraduateApplication.User)
                    .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                    .Include(v => v.VoucherDetails.Select(d => d.BankAccount))
                    .FirstOrDefault(v => v.Id == keyId);

                if (voucher != null)
                    transaction = db.ContractTransactions.Include(t => t.ContractType).Include(t => t.Lawyer.User).Include(t => t.Employee).FirstOrDefault(t => t.PaymentVoucherId == voucher.Id);
            }
            else
            {
                // البحث بالقسيمة (لطباعة القسيمة)
                voucher = db.PaymentVouchers
                    .Include(v => v.GraduateApplication.User)
                    .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                    .Include(v => v.VoucherDetails.Select(d => d.BankAccount))
                    .FirstOrDefault(v => v.Id == keyId);

                if (voucher != null)
                    transaction = db.ContractTransactions.Include(t => t.ContractType).Include(t => t.Lawyer.User).Include(t => t.Employee).FirstOrDefault(t => t.PaymentVoucherId == voucher.Id);
            }

            if (transaction == null) return null;

            string currencySymbol, amountInWords, employeeName;
            DateTime? issueDate, expiryDate;
            List<VoucherDetail> details;

            if (voucher != null)
            {
                currencySymbol = voucher.VoucherDetails.First().FeeType.Currency.Symbol;
                amountInWords = TafqeetHelper.ConvertToArabic(voucher.TotalAmount, currencySymbol);
                employeeName = voucher.IssuedByUserName;
                issueDate = voucher.IssueDate;
                expiryDate = voucher.ExpiryDate;
                details = voucher.VoucherDetails.ToList();
            }
            else
            {
                // حالة المعاملة المعفاة
                currencySymbol = transaction.ContractType.Currency?.Symbol ?? db.Currencies.First().Symbol;
                amountInWords = TafqeetHelper.ConvertToArabic(transaction.FinalFee, currencySymbol);
                employeeName = transaction.Employee.FullNameArabic;
                issueDate = transaction.TransactionDate;
                expiryDate = transaction.TransactionDate;
                details = new List<VoucherDetail>();
            }

            return new ContractPrintViewModel
            {
                TransactionId = transaction.Id,
                TransactionDate = transaction.TransactionDate,
                LawyerName = transaction.Lawyer.ArabicName,
                LawyerMembershipId = transaction.Lawyer.MembershipId,
                ContractTypeName = transaction.ContractType.Name,
                EmployeeName = employeeName,
                VoucherId = transaction.PaymentVoucherId ?? 0,
                IssueDate = issueDate,
                ExpiryDate = expiryDate,
                TotalAmount = transaction.FinalFee,
                CurrencySymbol = currencySymbol,
                TotalAmountInWords = amountInWords,
                ReceiptFullNumber = receipt != null ? $"{receipt.SequenceNumber}/{receipt.Year}" : "N/A",
                PaymentDate = receipt?.BankPaymentDate,
                BankReceiptNumber = receipt?.BankReceiptNumber,
                Parties = transaction.Parties.ToList(),
                Details = details,
                IsActingForSelf = transaction.IsActingForSelf,
                Minors = transaction.Minors.ToList(),
                AgentLegalCapacity = transaction.AgentLegalCapacity
            };
        }

        public ActionResult PrintContractVoucher(int id)
        {
            var viewModel = GetPrintViewModel(id, false);
            if (viewModel == null) return HttpNotFound("القسيمة أو المعاملة المرتبطة بها غير موجودة.");
            return View("PrintContractVoucher", viewModel);
        }

        public ActionResult PrintContractReceipt(int id)
        {
            var viewModel = GetPrintViewModel(id, true);
            if (viewModel == null) return HttpNotFound("الإيصال أو المعاملة المرتبطة بها غير موجودة.");
            return View("PrintContractReceipt", viewModel);
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintPassportAgency(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // 💡💡 هنا نستخدم المعامل الجديد للبحث برقم المعاملة 💡💡
            var viewModel = GetPrintViewModel(id.Value, isReceipt: false, isTransactionId: true);

            if (viewModel == null) return HttpNotFound();
            return View("PrintPassportAgency", viewModel);
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult ViewScannedContract(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var transaction = db.ContractTransactions.Find(id);

            if (transaction == null || string.IsNullOrEmpty(transaction.ScannedContractPath))
                return HttpNotFound("الملف غير موجود.");

            try
            {
                var physicalPath = Server.MapPath(transaction.ScannedContractPath);
                if (!System.IO.File.Exists(physicalPath)) return HttpNotFound("الملف غير موجود على الخادم.");

                var mimeType = MimeMapping.GetMimeMapping(physicalPath);
                return File(physicalPath, mimeType);
            }
            catch
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }

        // --- دوال مساعدة ---
        [HttpPost]
        public JsonResult CheckLawyerStatus(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return Json(new { success = false, message = "الرجاء إدخال المعرف." });

            var lawyer = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .FirstOrDefault(g => g.User.Username == identifier || g.User.IdentificationNumber == identifier || g.MembershipId == identifier);

            if (lawyer == null) return Json(new { success = false, message = "المحامي غير موجود." });

            bool isActive = LawyerStatusHelper.IsActiveLawyer(lawyer);
            string statusName = lawyer.ApplicationStatus.Name;

            return Json(new
            {
                success = true,
                name = lawyer.ArabicName,
                status = statusName,
                isActive = isActive
            });
        }

        [HttpGet]
        public JsonResult GetContractFee(int id)
        {
            var contractType = db.ContractTypes.Include(c => c.Currency).FirstOrDefault(c => c.Id == id);
            if (contractType == null) return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            return Json(new { success = true, fee = contractType.DefaultFee, currency = contractType.Currency.Symbol }, JsonRequestBehavior.AllowGet);
        }

        private void LoadDropdowns(ContractTransactionViewModel viewModel)
        {
            ViewBag.ContractTypeId = new SelectList(db.ContractTypes, "Id", "Name", viewModel.ContractTypeId);
            ViewBag.ExemptionReasonId = new SelectList(db.ContractExemptionReasons, "Id", "Reason", viewModel.ExemptionReasonId);
            ViewBag.ProvincesList = new SelectList(db.Provinces, "Id", "Name");
            ViewBag.PartyRolesList = new SelectList(db.PartyRoles, "Id", "Name");
            ViewBag.MinorRelationshipsList = new SelectList(db.MinorRelationships, "Id", "Name");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}