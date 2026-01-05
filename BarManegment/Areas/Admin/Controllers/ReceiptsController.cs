using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Collections.Generic;
using BarManegment.Services;
using PagedList;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class ReceiptsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. Index
        public ActionResult Index(string searchString, string typeFilter, string paymentMethod, int? page, int? pageSize)
        {
            var query = db.Receipts.AsNoTracking()
                .Include(r => r.PaymentVoucher.GraduateApplication)
                .Include(r => r.PaymentVoucher.VoucherDetails)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(r => r.SequenceNumber.ToString() == searchString ||
                                         r.BankReceiptNumber.Contains(searchString) ||
                                         r.PaymentVoucher.GraduateApplication.ArabicName.Contains(searchString) ||
                                         r.IssuedByUserName.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                if (paymentMethod == "Cash") query = query.Where(r => r.PaymentVoucher.PaymentMethod == "نقدي");
                else if (paymentMethod == "Bank") query = query.Where(r => r.PaymentVoucher.PaymentMethod != "نقدي");
            }

            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                if (typeFilter == "Lawyer") query = query.Where(r => r.PaymentVoucher.GraduateApplicationId != null);
                else if (typeFilter == "Contractor") query = query.Where(r => r.PaymentVoucher.GraduateApplicationId == null);
            }

            query = query.OrderByDescending(r => r.CreationDate);
            int pSize = pageSize ?? 10;
            int pageNumber = page ?? 1;

            ViewBag.CurrentSort = searchString;
            ViewBag.TypeFilter = typeFilter;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.PageSize = pSize;

            return View(query.ToPagedList(pageNumber, pSize));
        }

        // 2. Details
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var isContract = db.ContractTransactions.Any(c => c.PaymentVoucherId == id);
            if (isContract) return RedirectToAction("PrintContractReceipt", "ContractTransactions", new { id = id });

            var isLoan = db.LoanInstallments.Any(l => l.ReceiptId == id);
            if (isLoan) return RedirectToAction("PrintLoanInstallmentReceipt", "LoanPayments", new { id = id });

            var receipt = db.Receipts.Include(r => r.PaymentVoucher).FirstOrDefault(r => r.Id == id);
            if (receipt != null && receipt.PaymentVoucher.GraduateApplicationId == null)
            {
                return RedirectToAction("PrintContractorReceipt", "Receipts", new { id = id });
            }

            receipt = db.Receipts.AsNoTracking()
                .Include(r => r.PaymentVoucher.GraduateApplication.ApplicationStatus)
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency))
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.BankAccount))
                .FirstOrDefault(r => r.Id == id);

            if (receipt == null) return HttpNotFound();

            string currency = receipt.PaymentVoucher.VoucherDetails.FirstOrDefault()?.FeeType?.Currency?.Symbol ?? "₪";
            ViewBag.AmountInWords = TafqeetHelper.ConvertToArabic(receipt.PaymentVoucher.TotalAmount, currency);
            ViewBag.CurrencySymbol = currency;

            return View(receipt);
        }

        // 3. Create (GET)
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(int? voucherId)
        {
            if (voucherId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var voucher = db.PaymentVouchers
                .Include(v => v.GraduateApplication.ApplicationStatus)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                .Include(v => v.VoucherDetails.Select(d => d.BankAccount))
                .FirstOrDefault(v => v.Id == voucherId);

            if (voucher == null) return HttpNotFound();
            if (voucher.Status == "مسدد") { TempData["InfoMessage"] = "هذه القسيمة مسددة بالفعل."; return RedirectToAction("Details", new { id = voucher.Id }); }

            string name = voucher.GraduateApplication?.ArabicName ?? "متعهد / جهة خارجية";
            if (voucher.GraduateApplication == null)
            {
                var issuance = db.StampBookIssuances.Include(i => i.Contractor).FirstOrDefault(i => i.PaymentVoucherId == voucher.Id);
                if (issuance != null) name = issuance.Contractor.Name;
            }

            string status = voucher.GraduateApplication?.ApplicationStatus?.Name ?? "N/A";
            bool activateTrainee = (status.Contains("مقبول") || status == "بانتظار دفع الرسوم");

            var detailsList = voucher.VoucherDetails.Select(d => new ReceiptVoucherDetail
            {
                FeeTypeId = d.FeeTypeId,
                FeeTypeName = d.FeeType?.Name ?? "رسم",
                Amount = d.Amount,
                CurrencySymbol = d.FeeType?.Currency?.Symbol ?? "",
                BankAccountId = d.BankAccountId,
                BankName = d.BankAccount?.BankName ?? "",
                BankReceiptNumber = ""
            }).ToList();

            var viewModel = new CreateReceiptViewModel
            {
                PaymentVoucherId = voucher.Id,
                TraineeName = name,
                TotalAmount = voucher.TotalAmount,
                CurrencySymbol = voucher.VoucherDetails.FirstOrDefault()?.FeeType?.Currency?.Symbol ?? "",
                BankPaymentDate = DateTime.Now,
                CurrentTraineeStatus = status,
                ActivateTrainee = activateTrainee,
                Details = detailsList
            };

            return View(viewModel);
        }

        // 4. Create (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(CreateReceiptViewModel viewModel)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });

            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var voucher = db.PaymentVouchers
                            .Include(v => v.GraduateApplication.ApplicationStatus)
                            .Include(v => v.VoucherDetails.Select(d => d.FeeType))
                            .FirstOrDefault(v => v.Id == viewModel.PaymentVoucherId);

                        if (voucher == null || voucher.Status == "مسدد") throw new Exception("القسيمة غير موجودة أو مسددة.");

                        int currentYear = viewModel.BankPaymentDate.Year;
                        int newSequence = (db.Receipts.Where(r => r.Year == currentYear).Select(r => (int?)r.SequenceNumber).Max() ?? 0) + 1;

                        var receipt = new Receipt
                        {
                            Id = voucher.Id,
                            Year = currentYear,
                            SequenceNumber = newSequence,
                            BankReceiptNumber = viewModel.BankReceiptNumber,
                            BankPaymentDate = viewModel.BankPaymentDate,
                            CreationDate = DateTime.Now,
                            Notes = viewModel.Notes,
                            IssuedByUserId = (int)Session["UserId"],
                            IssuedByUserName = Session["FullName"] as string
                        };

                        db.Receipts.Add(receipt);
                        voucher.Status = "مسدد";

                        // معالجة حالة المتدرب/المحامي
                        var applicant = voucher.GraduateApplication;
                        if (applicant != null)
                        {
                            var feeTypesPaid = voucher.VoucherDetails.Select(d => d.FeeType.Name).ToList();

                            if (viewModel.ActivateTrainee)
                            {
                                var activeStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب مقيد");
                                if (activeStatus != null)
                                {
                                    applicant.ApplicationStatusId = activeStatus.Id;
                                    applicant.TrainingStartDate = viewModel.BankPaymentDate;
                                    if (string.IsNullOrEmpty(applicant.TraineeSerialNo)) applicant.TraineeSerialNo = GenerateTraineeSerial(currentYear);
                                    db.Entry(applicant).State = EntityState.Modified;
                                }
                            }
                            else if (feeTypesPaid.Any(f => f.Contains("تجديد مزاولة")))
                            {
                                var renewal = db.PracticingLawyerRenewals.FirstOrDefault(r => r.PaymentVoucherId == voucher.Id) ?? new PracticingLawyerRenewal { GraduateApplicationId = applicant.Id, RenewalYear = currentYear, PaymentVoucherId = voucher.Id };
                                renewal.IsActive = true; renewal.PaymentDate = viewModel.BankPaymentDate; renewal.ReceiptId = receipt.Id;
                                if (renewal.Id == 0) db.PracticingLawyerRenewals.Add(renewal); else db.Entry(renewal).State = EntityState.Modified;

                                var practicingStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول");
                                if (practicingStatus != null) { applicant.ApplicationStatusId = practicingStatus.Id; db.Entry(applicant).State = EntityState.Modified; }
                            }

                            if (feeTypesPaid.Any(f => f.Contains("يمين")))
                            {
                                var oathRequest = db.OathRequests.Where(o => o.GraduateApplicationId == applicant.Id).OrderByDescending(o => o.RequestDate).FirstOrDefault();
                                if (oathRequest != null && (oathRequest.Status == "بانتظار دفع رسوم اليمين" || oathRequest.Status == "قيد المراجعة"))
                                {
                                    oathRequest.Status = "بانتظار تحديد موعد اليمين";
                                    db.Entry(oathRequest).State = EntityState.Modified;
                                    AuditService.LogAction("Update Oath Request", "Receipts", $"Updated Oath Request status to 'Pending Scheduling' for Trainee {applicant.ArabicName}");
                                }
                            }
                        }

                        // معالجة الطوابع - (تصحيح اسم الجدول إلى Stamp)
                        var issuances = db.StampBookIssuances.Where(i => i.PaymentVoucherId == voucher.Id).ToList();
                        if (issuances.Any())
                        {
                            foreach (var issuance in issuances)
                            {
                                // ✅ تم التعديل: Stamp بدلاً من Stamps
                                db.Database.ExecuteSqlCommand("UPDATE Stamp SET Status = 'مع المتعهد' WHERE StampBookId = {0}", issuance.StampBookId);
                                var book = db.StampBooks.Find(issuance.StampBookId); if (book != null) { book.Status = "مع المتعهد"; db.Entry(book).State = EntityState.Modified; }
                            }
                        }

                        db.SaveChanges(); // حفظ الإيصال أولاً

                        // إنشاء القيد المحاسبي
                        using (var accountingService = new AccountingService())
                        {
                            bool entryCreated = accountingService.GenerateEntryForReceipt(receipt.Id, (int)Session["UserId"]);
                            if (!entryCreated)
                            {
                                throw new Exception("فشل إنشاء القيد المحاسبي. يرجى التأكد من وجود الحسابات (الصندوق 1101 / البنك 1102) والسنة المالية.");
                            }
                        }

                        transaction.Commit();

                        TempData["SuccessMessage"] = $"تم تسجيل الدفع (إيصال {newSequence}) وإنشاء القيد المحاسبي بنجاح.";

                        if (issuances.Any()) return RedirectToAction("PrintContractorReceipt", "PaymentVouchers", new { id = receipt.Id, area = "Admin" });
                        return RedirectToAction("Details", new { id = receipt.Id });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "خطأ: " + ex.Message);
                    }
                }
            }
            return View(viewModel);
        }

        // 5. PrintContractorReceipt
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintContractorReceipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var receipt = db.Receipts
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency))
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType))
                .FirstOrDefault(r => r.Id == id);

            if (receipt == null || receipt.PaymentVoucher == null) return HttpNotFound("الإيصال غير موجود.");

            var voucher = receipt.PaymentVoucher;
            var issuance = db.StampBookIssuances.Include(i => i.Contractor).FirstOrDefault(i => i.PaymentVoucherId == voucher.Id);
            string contractorName = issuance?.Contractor?.Name ?? "متعهد (عام)";
            string currencySymbol = voucher.VoucherDetails.FirstOrDefault()?.FeeType?.Currency?.Symbol ?? "؟";
            string amountInWords = TafqeetHelper.ConvertToArabic(voucher.TotalAmount, currencySymbol);

            var viewModel = new StampIssuanceReceiptViewModel
            {
                ReceiptId = receipt.Id,
                ReceiptFullNumber = $"{receipt.SequenceNumber}/{receipt.Year}",
                PaymentDate = receipt.BankPaymentDate,
                ContractorName = contractorName,
                IssuedByUserName = receipt.IssuedByUserName,
                TotalAmount = voucher.TotalAmount,
                TotalAmountInWords = amountInWords,
                CurrencySymbol = currencySymbol,
                BankReceiptNumber = receipt.BankReceiptNumber,
                Details = voucher.VoucherDetails.Select(d => new StampIssuanceReceiptDetail
                {
                    Description = d.Description,
                    Amount = d.Amount
                }).ToList()
            };

            return View("PrintContractorReceipt", viewModel);
        }

        private string GenerateTraineeSerial(int year)
        {
            string yearSuffix = "/" + year;
            var lastSerial = db.GraduateApplications
                .Where(g => g.TraineeSerialNo != null && g.TraineeSerialNo.EndsWith(yearSuffix))
                .Select(g => g.TraineeSerialNo)
                .AsEnumerable()
                .OrderByDescending(s => {
                    var parts = s.Split('/');
                    return parts.Length > 0 && int.TryParse(parts[0], out int n) ? n : 0;
                })
                .FirstOrDefault();

            int nextNum = 1;
            if (!string.IsNullOrEmpty(lastSerial))
            {
                var parts = lastSerial.Split('/');
                if (parts.Length > 0 && int.TryParse(parts[0], out int currentNum))
                {
                    nextNum = currentNum + 1;
                }
            }
            return $"{nextNum.ToString("D5")}/{year}";
        }

        public ActionResult PrintContractReceipt(int id) => RedirectToAction("Details", new { id = id });

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}