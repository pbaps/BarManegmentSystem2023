using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    // الصلاحية تعتمد على ما أضفته في Configuration.cs
    [CustomAuthorize(Permission = "CanView")]
    public class StampInventoryController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

 


        // GET: Admin/StampInventory (لوحة التحكم بالطوابع)
        public ActionResult Index()
        {
            // 1. بيانات المتعهدين
            var contractors = db.StampContractors.Where(c => c.IsActive).ToList();

            // 2. بيانات الكتب (المخزون)
            var books = db.StampBooks.OrderByDescending(b => b.DateAdded).ToList();

            // 3. القسائم الجاهزة للصرف (مدفوعة ولم تصرف بعد)
            // أ. نجلب أرقام القسائم التي تم الصرف مقابلها سابقاً
            var usedVoucherIds = db.StampBookIssuances
                                   .Select(i => i.PaymentVoucherId)
                                   .Distinct();

            // ب. نجلب القسائم المدفوعة (Receipt != null) وغير المربوطة بخريج (GraduateApplicationId == null) والتي لم تصرف بعد
            var pendingVouchers = db.PaymentVouchers
                .Include(v => v.Receipt)
                .Where(v => v.GraduateApplicationId == null &&
                            v.Receipt != null &&
                            !usedVoucherIds.Contains(v.Id))
                .OrderByDescending(v => v.IssueDate)
                .ToList();

            var viewModel = new StampDashboardViewModel
            {
                Contractors = contractors,
                AvailableBooks = books.Where(b => b.Status == "في المخزن").OrderBy(b => b.StartSerial).ToList(),
                IssuedBooks = books.Where(b => b.Status != "في المخزن").OrderByDescending(b => b.DateAdded).ToList(),
                PaidVouchersAwaitingIssuance = pendingVouchers
            };

            // تنبيه انخفاض المخزون
            if (viewModel.AvailableBooks.Count() <= 10)
            {
                TempData["WarningMessage"] = $"تنبيه: مخزون الدفاتر منخفض جداً (باقي {viewModel.AvailableBooks.Count()} دفتر فقط).";
            }

            return View(viewModel);
        }

        // POST: Admin/StampInventory/CreateContractor
        // ============================================================
        // إدارة المتعهدين (Contractors)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateContractor(StampContractor model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                db.StampContractors.Add(model);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم إضافة المتعهد بنجاح.";
            }
            else
            {
                TempData["ErrorMessage"] = "بيانات المتعهد غير مكتملة.";
            }
            return RedirectToAction("Index", new { tab = "contractors" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult EditContractor(StampContractor model)
        {
            if (model.Id == 0 || string.IsNullOrEmpty(model.Name))
            {
                TempData["ErrorMessage"] = "البيانات غير مكتملة.";
                return RedirectToAction("Index", new { tab = "contractors" });
            }

            var contractor = db.StampContractors.Find(model.Id);
            if (contractor == null)
            {
                TempData["ErrorMessage"] = "المتعهد غير موجود.";
                return RedirectToAction("Index", new { tab = "contractors" });
            }

            contractor.Name = model.Name;
            contractor.Phone = model.Phone;
            contractor.NationalId = model.NationalId;
            contractor.Governorate = model.Governorate;
            contractor.Location = model.Location;

            db.Entry(contractor).State = EntityState.Modified;
            db.SaveChanges();

            TempData["SuccessMessage"] = "تم تعديل بيانات المتعهد بنجاح.";
            return RedirectToAction("Index", new { tab = "contractors" });
        }


        // ============================================================
        // إدارة المخزون (Stamp Books)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateStampBook(long StartSerial, int StampsPerBook, int NumberOfBooks, decimal ValuePerStamp, string CouncilDecisionRef)
        {
            if (StampsPerBook <= 0 || ValuePerStamp <= 0 || StartSerial <= 0 || NumberOfBooks <= 0)
            {
                TempData["ErrorMessage"] = "بيانات غير صحيحة (القيم يجب أن تكون أكبر من صفر).";
                return RedirectToAction("Index");
            }

            long totalStamps = (long)StampsPerBook * NumberOfBooks;
            long finalEndSerial = StartSerial + totalStamps - 1;

            // التحقق من تداخل الأرقام
            bool conflict = db.Stamps.Any(s => s.SerialNumber >= StartSerial && s.SerialNumber <= finalEndSerial);
            if (conflict)
            {
                TempData["ErrorMessage"] = $"خطأ: الأرقام التسلسلية من {StartSerial} إلى {finalEndSerial} موجودة مسبقاً.";
                return RedirectToAction("Index");
            }

            try
            {
                // استخدام Transaction لضمان الحفظ الكامل
                using (var transaction = db.Database.BeginTransaction())
                {
                    long currentStart = StartSerial;

                    for (int i = 0; i < NumberOfBooks; i++)
                    {
                        long currentEnd = currentStart + StampsPerBook - 1;

                        // 1. إنشاء الدفتر
                        var book = new StampBook
                        {
                            StartSerial = currentStart,
                            EndSerial = currentEnd,
                            Quantity = StampsPerBook,
                            ValuePerStamp = ValuePerStamp,
                            CouncilDecisionRef = CouncilDecisionRef,
                            DateAdded = DateTime.Now,
                            Status = "في المخزن"
                        };
                        db.StampBooks.Add(book);
                        db.SaveChanges(); // للحصول على ID الدفتر

                        // 2. إنشاء الطوابع داخل الدفتر (Batch Insert لتحسين الأداء يفضل استخدام مكتبة، لكن هنا سنستخدم الطريقة العادية)
                        // ملاحظة: إذا كان العدد كبيراً جداً، يفضل استخدام AddRange كل 1000 عنصر.
                        var stampsList = new List<Stamp>();
                        for (long s = currentStart; s <= currentEnd; s++)
                        {
                            stampsList.Add(new Stamp
                            {
                                StampBookId = book.Id,
                                SerialNumber = s,
                                Value = ValuePerStamp,
                                Status = "في المخزن",
                                IsPaidToLawyer = false
                            });
                        }
                        db.Stamps.AddRange(stampsList);

                        currentStart = currentEnd + 1;
                    }

                    db.SaveChanges();
                    transaction.Commit();
                }

                TempData["SuccessMessage"] = $"تم إضافة {NumberOfBooks} دفاتر بنجاح (إجمالي {totalStamps} طابع).";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء الحفظ: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // ============================================================
        // صرف الدفاتر (Issuance)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult IssueBook(StampDashboardViewModel model)
        {
            int contractorId = model.SelectedContractorId;
            int bookId = model.SelectedBookId;

            // تحويل VoucherId من string إلى int (لأنه قادم من حقل مخفي في المودال قد يكون نصاً)
            if (!int.TryParse(model.VoucherId, out int voucherId))
            {
                TempData["ErrorMessage"] = "رقم القسيمة غير صالح.";
                return RedirectToAction("Index");
            }

            // 1. التحقق من القسيمة
            var voucher = db.PaymentVouchers.Include(v => v.Receipt).FirstOrDefault(v => v.Id == voucherId);
            if (voucher == null || voucher.Receipt == null)
            {
                TempData["ErrorMessage"] = "القسيمة غير موجودة أو غير مدفوعة.";
                return RedirectToAction("Index");
            }

            // 2. التحقق من الدفتر
            var book = db.StampBooks.Find(bookId);
            if (book == null || book.Status != "في المخزن")
            {
                TempData["ErrorMessage"] = "الدفتر المختار غير متاح.";
                return RedirectToAction("Index");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 3. تسجيل الصرف
                    var issuance = new StampBookIssuance
                    {
                        ContractorId = contractorId,
                        StampBookId = bookId,
                        PaymentVoucherId = voucherId,
                        IssuanceDate = DateTime.Now
                    };
                    db.StampBookIssuances.Add(issuance);

                    // 4. تحديث حالة الدفتر
                    book.Status = "مع المتعهد"; // تم التغيير من "تم صرفه" ليكون أوضح
                    db.Entry(book).State = EntityState.Modified;

                    // 5. تحديث حالة الطوابع الفردية (SQL مباشر للأداء)
                    db.Database.ExecuteSqlCommand(
                        "UPDATE Stamps SET Status = 'مع المتعهد', ContractorId = {0} WHERE StampBookId = {1}",
                        contractorId, bookId);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = $"تم صرف الدفتر ({book.StartSerial}-{book.EndSerial}) بنجاح.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "خطأ أثناء الصرف: " + ex.Message;
                }
            }

            return RedirectToAction("Index");
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