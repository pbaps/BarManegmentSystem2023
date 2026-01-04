using BarManegment.Models;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using System.Net;
using BarManegment.Services; // ✅ ضروري لاستخدام الخدمة المالية

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class StampIssuanceController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/StampIssuance
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

        // POST: Admin/StampIssuance
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

            var feeType = db.FeeTypes.FirstOrDefault(f => f.Name == "رسوم طوابع");
            if (feeType == null)
            {
                TempData["ErrorMessage"] = "خطأ فادح: لم يتم تعريف 'رسوم طوابع' في أنواع الرسوم.";
                return RedirectToAction("Index");
            }

            decimal totalAmount = selectedBooks.Sum(b => b.Quantity * b.ValuePerStamp);
            var employeeId = (int)Session["UserId"];
            var employeeName = Session["FullName"] as string;

            // بدء معاملة (Transaction)
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // --- 1. إنشاء قسيمة الدفع (وتعيينها "مسدد" فوراً) ---
                    var voucher = new PaymentVoucher
                    {
                        GraduateApplicationId = null, // لأنها للمتعهد
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
                    db.SaveChanges(); // حفظ القسيمة للحصول على ID

                    // --- 2. إنشاء إيصال القبض (فوراً) ---
                    int currentYear = DateTime.Now.Year;
                    int lastSequenceNumber = db.Receipts.Where(r => r.Year == currentYear).Select(r => (int?)r.SequenceNumber).Max() ?? 0;
                    int newSequenceNumber = lastSequenceNumber + 1;

                    var receipt = new Receipt
                    {
                        Id = voucher.Id, // الربط (نفس الـ ID للقسيمة والإيصال)
                        BankPaymentDate = DateTime.Now,
                        BankReceiptNumber = "دفع نقدي (طوابع)",
                        CreationDate = DateTime.Now,
                        IssuedByUserId = employeeId,
                        IssuedByUserName = employeeName,
                        Year = currentYear,
                        SequenceNumber = newSequenceNumber
                    };
                    db.Receipts.Add(receipt);

                    // --- 3. إنشاء سجلات الصرف وتفعيل الطوابع ---
                    foreach (var book in selectedBooks)
                    {
                        var issuance = new StampBookIssuance
                        {
                            ContractorId = viewModel.SelectedContractorId,
                            StampBookId = book.Id,
                            PaymentVoucherId = voucher.Id,
                            IssuanceDate = DateTime.Now
                        };
                        db.StampBookIssuances.Add(issuance);

                        // أ. تحديث حالة الدفتر (الأب)
                        book.Status = "مع المتعهد"; // حالة الدفتر بعد الصرف

                        // ب. (الإجراء الآلي) تحديث جميع الطوابع الفردية
                        // استخدام SQL مباشر لتحسين الأداء بدلاً من تحميل آلاف الطوابع
                        db.Database.ExecuteSqlCommand(
                            "UPDATE Stamps SET Status = {0}, ContractorId = {1} WHERE StampBookId = {2}",
                            "مع المتعهد", viewModel.SelectedContractorId, book.Id
                        );
                    }

                    db.SaveChanges(); // حفظ الإيصال وتحديثات الطوابع
                    transaction.Commit(); // تأكيد العملية

                    // ============================================================
                    // === 💡 التكامل المالي: إنشاء قيد اليومية الآلي 💡 ===
                    // ============================================================
                    try
                    {
                        using (var accService = new AccountingService())
                        {
                            bool isEntryCreated = accService.GenerateEntryForReceipt(receipt.Id, employeeId);
                            if (isEntryCreated)
                            {
                                TempData["SuccessMessage"] = $"تم استلام المبلغ وصرف ({selectedBooks.Count}) دفاتر، وتم إنشاء القيد المحاسبي بنجاح.";
                            }
                            else
                            {
                                TempData["SuccessMessage"] = $"تم استلام المبلغ وصرف ({selectedBooks.Count}) دفاتر، ولكن تعذر إنشاء القيد الآلي.";
                            }
                        }
                    }
                    catch
                    {
                        TempData["SuccessMessage"] = $"تم استلام المبلغ وصرف ({selectedBooks.Count}) دفاتر (خطأ في النظام المحاسبي).";
                    }
                    // ============================================================

                    // إعادة التوجيه لصفحة طباعة إيصال المتعهد
                    return RedirectToAction("PrintIssuanceReceipt", new { id = receipt.Id });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "حدث خطأ فادح أثناء الحفظ: " + ex.Message;
                    return RedirectToAction("Index");
                }
            }
        }

        // GET: Admin/StampIssuance/PrintIssuanceReceipt/1012
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintIssuanceReceipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // 1. جلب الإيصال ومعه القسيمة والتفاصيل
            var receipt = db.Receipts
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency))
                .FirstOrDefault(r => r.Id == id);

            if (receipt == null || receipt.PaymentVoucher.PaymentMethod != "نقدي")
            {
                return HttpNotFound("الإيصال غير موجود أو غير صالح لهذه العملية.");
            }

            var voucher = receipt.PaymentVoucher;
            var currencySymbol = voucher.VoucherDetails.FirstOrDefault()?.FeeType.Currency?.Symbol ?? "₪";

            // 2. البحث عن اسم المتعهد
            var issuance = db.StampBookIssuances
                             .Include(i => i.Contractor)
                             .FirstOrDefault(i => i.PaymentVoucherId == voucher.Id);

            string contractorName = (issuance != null) ? issuance.Contractor.Name : "متعهد طوابع";

            // 3. بناء الـ ViewModel
            var viewModel = new StampIssuanceReceiptViewModel
            {
                ReceiptId = receipt.Id,
                ReceiptFullNumber = $"{receipt.SequenceNumber}/{receipt.Year}",
                PaymentDate = receipt.BankPaymentDate,
                ContractorName = contractorName,
                IssuedByUserName = receipt.IssuedByUserName,
                TotalAmount = voucher.TotalAmount,
                CurrencySymbol = currencySymbol,
                TotalAmountInWords = TafqeetHelper.ConvertToArabic(voucher.TotalAmount, currencySymbol),
                Details = voucher.VoucherDetails.Select(d => new StampIssuanceReceiptDetail
                {
                    Description = d.Description,
                    Amount = d.Amount
                }).ToList()
            };

            return View("PrintIssuanceReceipt", viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}