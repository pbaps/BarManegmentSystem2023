using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Tafqeet;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "Receipts")]
    public class ReceiptsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. عرض سجل الإيصالات (Index)
        // ============================================================
        public ActionResult Index(string searchString, string typeFilter, string paymentMethod, int? page, int? pageSize)
        {
            var query = db.Receipts.AsNoTracking()
                .Include(r => r.PaymentVoucher.GraduateApplication)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(r => r.SequenceNumber.ToString() == searchString ||
                                         r.BankReceiptNumber.Contains(searchString) ||
                                         r.PaymentVoucher.GraduateApplication.ArabicName.Contains(searchString) ||
                                         r.IssuedByUserName.Contains(searchString) ||
                                         r.PaymentVoucher.CheckNumber.Contains(searchString));
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
            int pSize = pageSize ?? 20;
            int pageNumber = page ?? 1;

            ViewBag.CurrentSort = searchString;
            ViewBag.TypeFilter = typeFilter;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.PageSize = pSize;

            return View(query.ToPagedList(pageNumber, pSize));
        }

        // ============================================================
        // 2. تفاصيل الإيصال (Details)
        // ============================================================
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var receipt = db.Receipts.AsNoTracking()
                .Include(r => r.PaymentVoucher.GraduateApplication.ApplicationStatus)
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency))
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.BankAccount))
                .FirstOrDefault(r => r.Id == id);

            if (receipt == null) return HttpNotFound();

            bool isContractorReceipt = db.StampBookIssuances.Any(i => i.PaymentVoucherId == receipt.Id);

            if (isContractorReceipt)
            {
                return RedirectToAction("PrintContractorReceipt", new { id = id });
            }

            string currency = receipt.PaymentVoucher.VoucherDetails.FirstOrDefault()?.FeeType?.Currency?.Symbol ?? "₪";
            ViewBag.AmountInWords = TafqeetHelper.ConvertToArabic(receipt.PaymentVoucher.TotalAmount, currency);
            ViewBag.CurrencySymbol = currency;

            return View(receipt);
        }

        // ============================================================
        // 3. تحصيل قسيمة (Create - GET)
        // ============================================================
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

            if (voucher.Status == "مسدد")
            {
                TempData["InfoMessage"] = "هذه القسيمة مسددة مسبقاً.";
                return RedirectToAction("Details", new { id = voucher.Id });
            }

            string displayName = voucher.GraduateApplication?.ArabicName;
            if (string.IsNullOrEmpty(displayName))
            {
                var issuance = db.StampBookIssuances.Include(i => i.Contractor).FirstOrDefault(i => i.PaymentVoucherId == voucher.Id);
                displayName = issuance?.Contractor?.Name ?? voucher.CheckNumber ?? "جهة خارجية";
            }

            var viewModel = new CreateReceiptViewModel
            {
                PaymentVoucherId = voucher.Id,
                TraineeName = displayName,
                CurrentTraineeStatus = voucher.GraduateApplication?.ApplicationStatus?.Name ?? "N/A",
                TotalAmount = voucher.TotalAmount,
                CurrencySymbol = voucher.VoucherDetails.FirstOrDefault()?.FeeType?.Currency?.Symbol ?? "₪",
                BankPaymentDate = DateTime.Now,
                Details = voucher.VoucherDetails.Select(d => new ReceiptVoucherDetail
                {
                    FeeTypeId = d.FeeTypeId,
                    FeeTypeName = d.FeeType?.Name ?? "رسم",
                    Amount = d.Amount,
                    CurrencySymbol = d.FeeType?.Currency?.Symbol ?? "",
                    BankAccountId = d.BankAccountId,
                    BankName = d.BankAccount?.BankName ?? "نقدي"
                }).ToList()
            };

            return View(viewModel);
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateContractorReceipt(int? voucherId)
        {
            return RedirectToAction("Create", new { voucherId = voucherId });
        }

        // ============================================================
        // 4. حفظ التحصيل وإنشاء القيد (Create - POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(CreateReceiptViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var voucher = db.PaymentVouchers
                            .Include(v => v.GraduateApplication)
                            .Include(v => v.VoucherDetails.Select(d => d.FeeType)) // ✅ ضروري لجلب الحساب المربوط
                            .Include(v => v.VoucherDetails.Select(d => d.BankAccount))
                            .FirstOrDefault(v => v.Id == viewModel.PaymentVoucherId);

                        if (voucher == null || voucher.Status == "مسدد") throw new Exception("القسيمة غير صالحة.");

                        var currentYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                        if (currentYear == null) throw new Exception("لا توجد سنة مالية مفتوحة.");

                        int nextSeq = (db.Receipts.Where(r => r.Year == currentYear.StartDate.Year).Max(r => (int?)r.SequenceNumber) ?? 0) + 1;
                        var receipt = new Receipt
                        {
                            Id = voucher.Id,
                            Year = currentYear.StartDate.Year,
                            SequenceNumber = nextSeq,
                            BankReceiptNumber = viewModel.BankReceiptNumber,
                            BankPaymentDate = viewModel.BankPaymentDate,
                            CreationDate = DateTime.Now,
                            IssuedByUserId = (int)Session["UserId"],
                            IssuedByUserName = Session["FullName"]?.ToString() ?? "System"
                        };
                        db.Receipts.Add(receipt);

                        voucher.Status = "مسدد";

                        if (voucher.GraduateApplicationId.HasValue && viewModel.ActivateTrainee)
                            ProcessTraineeActivation(voucher.GraduateApplication);

                        ProcessStampStatus(voucher.Id);
                        ProcessExamApplicationPayment(voucher);

                        // --- إنشاء القيد المحاسبي ---
                        var entry = new JournalEntry
                        {
                            FiscalYearId = currentYear.Id,
                            EntryNumber = "R-" + receipt.SequenceNumber,
                            EntryDate = receipt.BankPaymentDate,
                            Description = $"سند قبض رقم {receipt.SequenceNumber} - {GetVoucherDisplayName(voucher)}",
                            SourceModule = "Receipts",
                            ReferenceNumber = receipt.BankReceiptNumber,
                            IsPosted = true,
                            PostedDate = DateTime.Now,
                            PostedByUserId = receipt.IssuedByUserId,
                            TotalDebit = voucher.TotalAmount,
                            TotalCredit = voucher.TotalAmount,
                            JournalEntryDetails = new List<JournalEntryDetail>()
                        };

                        // 1. الطرف المدين (البنك/الصندوق)
                        int debitAccountId = voucher.VoucherDetails.FirstOrDefault()?.BankAccount?.RelatedAccountId ?? 0;
                        if (debitAccountId == 0) debitAccountId = db.Accounts.FirstOrDefault(a => a.Code == "1102")?.Id ?? 0;

                        entry.JournalEntryDetails.Add(new JournalEntryDetail
                        {
                            AccountId = debitAccountId,
                            Debit = voucher.TotalAmount,
                            Credit = 0,
                            Description = "تحصيل رسوم"
                        });

                        // 2. الطرف الدائن (الإيرادات) - ✅ هنا التعديل الجوهري
                        foreach (var det in voucher.VoucherDetails)
                        {
                            // 💡 فحص الحساب المربوط بنوع الرسم أولاً
                            int revAccId = det.FeeType?.RevenueAccountId ?? 0;

                            // 💡 إذا لم يكن مربوطاً، استخدام الحساب الافتراضي
                            if (revAccId == 0)
                            {
                                revAccId = db.Accounts.FirstOrDefault(a => a.Code.StartsWith("4"))?.Id ?? 0;
                            }

                            entry.JournalEntryDetails.Add(new JournalEntryDetail
                            {
                                AccountId = revAccId, // سيأخذ الحساب المربوط (مثل 4105)
                                Debit = 0,
                                Credit = det.Amount,
                                Description = det.FeeType?.Name
                            });
                        }
                        db.JournalEntries.Add(entry);

                        db.SaveChanges();
                        transaction.Commit();
                        AuditService.LogAction("ReceiptCreated", "Receipts", $"تم إنشاء إيصال رقم {nextSeq} للقسيمة {voucher.Id}");

                        return RedirectToAction("Details", new { id = receipt.Id });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "خطأ في الحفظ: " + ex.Message);
                    }
                }
            }
            return View(viewModel);
        }

        // --- وظائف مساعدة داخلية ---
        private void ProcessExamApplicationPayment(PaymentVoucher voucher)
        {
            var examFeeDetail = voucher.VoucherDetails.FirstOrDefault(d => d.Description.Contains("امتحان القبول"));
            if (examFeeDetail != null)
            {
                string desc = examFeeDetail.Description;
                int openParen = desc.LastIndexOf('(');
                int closeParen = desc.LastIndexOf(')');

                if (openParen != -1 && closeParen > openParen)
                {
                    string nationalId = desc.Substring(openParen + 1, closeParen - openParen - 1);
                    var examApp = db.ExamApplications.FirstOrDefault(a => a.NationalIdNumber == nationalId && a.Status == "بانتظار دفع الرسوم");

                    if (examApp != null)
                    {
                        examApp.Status = "قيد المراجعة";
                        db.Entry(examApp).State = EntityState.Modified;
                    }
                }
            }
        }

        private string GetVoucherDisplayName(PaymentVoucher v)
        {
            return v.GraduateApplication?.ArabicName ?? db.StampBookIssuances.Include(i => i.Contractor).FirstOrDefault(i => i.PaymentVoucherId == v.Id)?.Contractor?.Name ?? v.CheckNumber ?? "جهة خارجية";
        }
        private void ProcessTraineeActivation(GraduateApplication trainee)
        {
            var activeStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب مقيد");
            if (activeStatus != null) { trainee.ApplicationStatusId = activeStatus.Id; if (string.IsNullOrEmpty(trainee.TraineeSerialNo)) trainee.TraineeSerialNo = "T-" + DateTime.Now.Year + "-" + trainee.Id; }
        }
        private void ProcessStampStatus(int voucherId)
        {
            var books = db.StampBookIssuances.Where(i => i.PaymentVoucherId == voucherId).Select(i => i.StampBook).ToList();
            foreach (var book in books) { if (book != null) book.Status = "مع المتعهد"; }
        }

        public ActionResult PrintReceipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var receipt = db.Receipts.Include(r => r.PaymentVoucher.GraduateApplication).Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency)).FirstOrDefault(r => r.Id == id);
            if (receipt == null) return HttpNotFound();

            string currency = receipt.PaymentVoucher.VoucherDetails.FirstOrDefault()?.FeeType?.Currency?.Symbol ?? "₪";
            ViewBag.AmountInWords = TafqeetHelper.ConvertToArabic(receipt.PaymentVoucher.TotalAmount, currency);
            return View(receipt);
        }

        public ActionResult PrintContractorReceipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var receipt = db.Receipts.Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency)).Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.BankAccount)).FirstOrDefault(r => r.Id == id);
            if (receipt == null) return HttpNotFound();

            var voucher = receipt.PaymentVoucher;
            var issuance = db.StampBookIssuances.Include(i => i.Contractor).FirstOrDefault(i => i.PaymentVoucherId == voucher.Id);
            string currency = voucher.VoucherDetails.FirstOrDefault()?.FeeType?.Currency?.Symbol ?? "₪";
            ViewBag.AmountInWords = TafqeetHelper.ConvertToArabic(voucher.TotalAmount, currency);

            var printModel = new PrintVoucherViewModel
            {
                VoucherId = receipt.SequenceNumber,
                TraineeName = issuance?.Contractor?.Name ?? voucher.CheckNumber ?? "متعهد",
                IssueDate = receipt.BankPaymentDate,
                TotalAmount = voucher.TotalAmount,
                PaymentMethod = voucher.PaymentMethod,
                IssuedByUserName = receipt.IssuedByUserName,
                Details = voucher.VoucherDetails.Select(d => new VoucherPrintDetail
                {
                    FeeTypeName = d.Description ?? d.FeeType.Name,
                    Amount = d.Amount,
                    CurrencySymbol = currency,
                    BankName = d.BankAccount?.BankName
                }).ToList()
            };
            return View("~/Areas/Admin/Views/PaymentVouchers/PrintStampContractorVoucher.cshtml", printModel);
        }

        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var receipt = db.Receipts.Include(r => r.PaymentVoucher.GraduateApplication).FirstOrDefault(r => r.Id == id);
            return receipt == null ? HttpNotFound() : (ActionResult)View(receipt);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var receipt = db.Receipts.Include(r => r.PaymentVoucher).FirstOrDefault(r => r.Id == id);
                    if (receipt != null)
                    {
                        int currentSequence = receipt.SequenceNumber;
                        receipt.PaymentVoucher.Status = "صادر";
                        var entry = db.JournalEntries.FirstOrDefault(j => j.SourceModule == "Receipts" && j.EntryNumber.Contains(receipt.SequenceNumber.ToString()));

                        if (entry != null)
                        {
                            db.JournalEntryDetails.RemoveRange(entry.JournalEntryDetails);
                            db.JournalEntries.Remove(entry);
                        }

                        db.Receipts.Remove(receipt);
                        db.SaveChanges();
                        transaction.Commit();
                        AuditService.LogAction("ReceiptDeleted", "Receipts", $"تم إلغاء الإيصال رقم {currentSequence} وإعادة القسيمة للحالة 'صادر'");
                    }
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = ex.Message;
                    return RedirectToAction("Index");
                }
            }
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}