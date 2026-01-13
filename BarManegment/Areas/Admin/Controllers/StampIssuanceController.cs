using BarManegment.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using System.Net;
using BarManegment.Services;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class StampIssuanceController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var availableBooks = db.StampBooks
                .Where(b => b.Status == "في المخزن")
                .OrderBy(b => b.StartSerial)
                .ToList();

            var contractors = db.StampContractors.Where(c => c.IsActive).ToList();

            var viewModel = new StampIssuanceViewModel
            {
                AvailableBooksList = availableBooks,
                ContractorsList = new SelectList(contractors, "Id", "Name")
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Index(StampIssuanceViewModel viewModel)
        {
            if (viewModel.SelectedBookIds == null || !viewModel.SelectedBookIds.Any())
            {
                TempData["ErrorMessage"] = "يجب اختيار دفتر واحد على الأقل.";
                return RedirectToAction("Index");
            }

            var selectedBooks = db.StampBooks
                .Where(b => viewModel.SelectedBookIds.Contains(b.Id) && b.Status == "في المخزن")
                .ToList();

            if (selectedBooks.Count != viewModel.SelectedBookIds.Count)
            {
                TempData["ErrorMessage"] = "خطأ: بعض الدفاتر المختارة لم تعد متاحة.";
                return RedirectToAction("Index");
            }

            var feeType = db.FeeTypes.FirstOrDefault(f => f.Name.Contains("طوابع"));
            if (feeType == null)
            {
                TempData["ErrorMessage"] = "خطأ فادح: لم يتم تعريف 'رسوم طوابع' في أنواع الرسوم.";
                return RedirectToAction("Index");
            }

            decimal totalAmount = selectedBooks.Sum(b => b.Quantity * b.ValuePerStamp);
            var employeeId = (int)(Session["UserId"] ?? 1);
            var employeeName = Session["FullName"] as string ?? "System";

            int createdReceiptId = 0;
            string contractorName = "";

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // جلب اسم المتعهد للتدقيق
                    var contractor = db.StampContractors.Find(viewModel.SelectedContractorId);
                    contractorName = contractor?.Name ?? "متعهد غير معروف";

                    // 1. القسيمة
                    var voucher = new PaymentVoucher
                    {
                        GraduateApplicationId = null,
                        PaymentMethod = "نقدي",
                        IssueDate = DateTime.Now,
                        ExpiryDate = DateTime.Now,
                        Status = "مسدد",
                        TotalAmount = totalAmount,
                        IssuedByUserId = employeeId,
                        IssuedByUserName = employeeName,
                        VoucherDetails = new List<VoucherDetail>()
                    };

                    foreach (var book in selectedBooks)
                    {
                        voucher.VoucherDetails.Add(new VoucherDetail
                        {
                            FeeTypeId = feeType.Id,
                            Amount = book.Quantity * book.ValuePerStamp,
                            BankAccountId = feeType.BankAccountId,
                            Description = $"دفتر طوابع (من {book.StartSerial} إلى {book.EndSerial})"
                        });
                    }
                    db.PaymentVouchers.Add(voucher);
                    db.SaveChanges();

                    // 2. الإيصال
                    var currentYearRecord = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                    int year = currentYearRecord != null ? currentYearRecord.StartDate.Year : DateTime.Now.Year;
                    int lastSeq = db.Receipts.Where(r => r.Year == year).Max(r => (int?)r.SequenceNumber) ?? 0;

                    var receipt = new Receipt
                    {
                        Id = voucher.Id,
                        BankPaymentDate = DateTime.Now,
                        BankReceiptNumber = "دفع نقدي (طوابع)",
                        CreationDate = DateTime.Now,
                        IssuedByUserId = employeeId,
                        IssuedByUserName = employeeName,
                        Year = year,
                        SequenceNumber = lastSeq + 1
                    };
                    db.Receipts.Add(receipt);

                    // 3. تحديث الدفاتر والطوابع
                    foreach (var book in selectedBooks)
                    {
                        db.StampBookIssuances.Add(new StampBookIssuance
                        {
                            ContractorId = viewModel.SelectedContractorId,
                            StampBookId = book.Id,
                            PaymentVoucherId = voucher.Id,
                            IssuanceDate = DateTime.Now
                        });

                        book.Status = "مع المتعهد";
                        db.Database.ExecuteSqlCommand(
                            "UPDATE Stamps SET Status = 'مع المتعهد', ContractorId = {0} WHERE StampBookId = {1}",
                            viewModel.SelectedContractorId, book.Id
                        );
                    }

                    db.SaveChanges();
                    transaction.Commit();
                    createdReceiptId = receipt.Id;

                    // ✅ إضافة سجل التدقيق (Audit Log) لعملية الصرف الناجحة
                    AuditService.LogAction("StampIssuance", "StampIssuance",
                        $"تم صرف {selectedBooks.Count} دفاتر للمتعهد {contractorName} بمبلغ {totalAmount} شيكل. إيصال رقم #{receipt.SequenceNumber}");

                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    // ❌ إضافة سجل تدقيق للفشل
                    AuditService.LogAction("StampIssuanceError", "StampIssuance", $"فشل صرف الطوابع: {ex.Message}");

                    TempData["ErrorMessage"] = "خطأ أثناء الحفظ: " + ex.Message;
                    return RedirectToAction("Index");
                }
            }

            // ✅ 4. إنشاء القيد المحاسبي
            if (createdReceiptId > 0)
            {
                using (var accService = new AccountingService())
                {
                    bool isEntryCreated = accService.GenerateEntryForReceipt(createdReceiptId, employeeId);
                    if (isEntryCreated)
                        TempData["SuccessMessage"] = "تم الصرف وترحيل القيد بنجاح.";
                    else
                        TempData["WarningMessage"] = "تم الصرف بنجاح، ولكن فشل ترحيل القيد المحاسبي.";
                }
                return RedirectToAction("PrintIssuanceReceipt", new { id = createdReceiptId });
            }

            return RedirectToAction("Index");
        }

        public ActionResult PrintIssuanceReceipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var receipt = db.Receipts
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency))
                .FirstOrDefault(r => r.Id == id);

            if (receipt == null) return HttpNotFound();

            var voucher = receipt.PaymentVoucher;
            var currencySymbol = voucher.VoucherDetails.FirstOrDefault()?.FeeType.Currency?.Symbol ?? "₪";
            var issuance = db.StampBookIssuances.Include(i => i.Contractor).FirstOrDefault(i => i.PaymentVoucherId == voucher.Id);

            var viewModel = new StampIssuanceReceiptViewModel
            {
                ReceiptId = receipt.Id,
                ReceiptFullNumber = $"{receipt.SequenceNumber}/{receipt.Year}",
                PaymentDate = receipt.BankPaymentDate,
                ContractorName = issuance?.Contractor?.Name ?? "متعهد طوابع",
                IssuedByUserName = receipt.IssuedByUserName,
                TotalAmount = voucher.TotalAmount,
                CurrencySymbol = currencySymbol,
                TotalAmountInWords = TafqeetHelper.ConvertToArabic(voucher.TotalAmount, currencySymbol),
                Details = voucher.VoucherDetails.Select(d => new StampIssuanceReceiptDetail { Description = d.Description, Amount = d.Amount }).ToList()
            };

            return View(viewModel);
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}