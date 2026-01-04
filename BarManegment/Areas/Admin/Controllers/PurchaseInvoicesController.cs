using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Services;
using System.Net;

namespace BarManegment.Areas.Admin.Controllers
{
    [Authorize]
    [CustomAuthorize(Permission = "CanView")]
    public class PurchaseInvoicesController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View(db.PurchaseInvoices.Include(p => p.Supplier).OrderByDescending(p => p.InvoiceDate).ToList());
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            ViewBag.SupplierId = new SelectList(db.Suppliers.Where(s => s.IsActive), "Id", "Name");
            // جلب الأصناف وعرضها في القائمة (يمكن تحسينه عبر AJAX للأداء العالي)
            ViewBag.ItemsList = db.Items.Where(i => i.IsActive).Select(i => new { i.Id, i.Name }).ToList();
            return View(new PurchaseInvoiceViewModel { InvoiceDate = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PurchaseInvoiceViewModel model)
        {
            if (ModelState.IsValid && model.Items != null && model.Items.Any(i => i.Quantity > 0))
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. حفظ الفاتورة (الرأس)
                        var invoice = new PurchaseInvoice
                        {
                            SupplierId = model.SupplierId,
                            SupplierInvoiceNumber = model.SupplierInvoiceNumber,
                            InvoiceDate = model.InvoiceDate,
                            PaymentMethod = model.PaymentMethod,
                            Notes = model.Notes,
                            CreatedByUserId = (int?)Session["UserId"] ?? 1, // تأمين في حال فقدان الجلسة
                            Items = new List<PurchaseInvoiceItem>()
                        };

                        decimal totalInvoiceAmount = 0;

                        // 2. معالجة الأصناف
                        foreach (var itemVM in model.Items)
                        {
                            if (itemVM.Quantity <= 0) continue;

                            var itemEntity = db.Items.Find(itemVM.ItemId);
                            if (itemEntity != null)
                            {
                                // تحديث المخزون (Weighted Average Cost)
                                // المتوسط المرجح = (القيمة القديمة + القيمة الجديدة) / الكمية الجديدة
                                decimal oldTotalValue = itemEntity.CurrentQuantity * itemEntity.AverageCost;
                                decimal newPurchaseValue = itemVM.Quantity * itemVM.UnitPrice;
                                int newTotalQty = itemEntity.CurrentQuantity + itemVM.Quantity;

                                if (newTotalQty > 0)
                                {
                                    itemEntity.AverageCost = (oldTotalValue + newPurchaseValue) / newTotalQty;
                                }
                                itemEntity.CurrentQuantity = newTotalQty;
                                db.Entry(itemEntity).State = EntityState.Modified;

                                // إضافة سطر الفاتورة
                                invoice.Items.Add(new PurchaseInvoiceItem
                                {
                                    ItemId = itemVM.ItemId,
                                    Quantity = itemVM.Quantity,
                                    UnitPrice = itemVM.UnitPrice
                                });

                                totalInvoiceAmount += newPurchaseValue;
                            }
                        }

                        invoice.TotalAmount = totalInvoiceAmount;
                        invoice.IsPosted = true; // تعتبر مرحلة مخزنياً ومالياً
                        db.PurchaseInvoices.Add(invoice);
                        db.SaveChanges(); // حفظ للحصول على ID الفاتورة

                        // 3. إنشاء القيد المحاسبي الآلي
                        // المدين: ح/ المخزون (1201 أو ما شابه)
                        // الدائن: ح/ المورد أو الصندوق/البنك

                        var inventoryAccount = db.Accounts.FirstOrDefault(a => a.Name.Contains("مخزون") || a.Code.StartsWith("12"));
                        var supplier = db.Suppliers.Include(s => s.Account).FirstOrDefault(s => s.Id == model.SupplierId);

                        // تحديد الطرف الدائن
                        Account creditAccount = null;
                        if (model.PaymentMethod == "آجل")
                        {
                            creditAccount = supplier?.Account ?? db.Accounts.FirstOrDefault(a => a.Name.Contains("موردين") || a.Code.StartsWith("22"));
                        }
                        else
                        {
                            creditAccount = db.Accounts.FirstOrDefault(a => a.Code == "110101") ?? db.Accounts.FirstOrDefault(a => a.Code.StartsWith("11")); // صندوق
                        }

                        if (inventoryAccount != null && creditAccount != null)
                        {
                            // جلب السنة المالية
                            var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                            int fiscalYearId = fiscalYear?.Id ?? 1; // قيمة افتراضية

                            // توليد رقم القيد (كسلسلة نصية)
                            string nextEntryNo = "1";
                            if (db.JournalEntries.Any(j => j.FiscalYearId == fiscalYearId))
                            {
                                var lastEntry = db.JournalEntries
                                                  .Where(j => j.FiscalYearId == fiscalYearId)
                                                  .OrderByDescending(j => j.Id)
                                                  .FirstOrDefault();

                                if (lastEntry != null && int.TryParse(lastEntry.EntryNumber, out int lastNo))
                                    nextEntryNo = (lastNo + 1).ToString();
                                else
                                    nextEntryNo = (db.JournalEntries.Count(j => j.FiscalYearId == fiscalYearId) + 1).ToString();
                            }

                            var entry = new JournalEntry
                            {
                                FiscalYearId = fiscalYearId,
                                EntryNumber = nextEntryNo,
                                EntryDate = invoice.InvoiceDate,
                                Description = $"فاتورة شراء رقم {invoice.Id} - {supplier?.Name ?? "مورد"}",
                                ReferenceNumber = invoice.SupplierInvoiceNumber,
                                SourceModule = "PurchaseInvoices",
                                SourceId = invoice.Id,
                                IsPosted = true,
                                PostedDate = DateTime.Now,
                                PostedByUserId = (int?)Session["UserId"] ?? 1,
                                TotalDebit = totalInvoiceAmount,
                                TotalCredit = totalInvoiceAmount,
                                JournalEntryDetails = new List<JournalEntryDetail>() // استخدام الاسم الجديد
                            };

                            // السطر المدين (من ح/ المخزون)
                            entry.JournalEntryDetails.Add(new JournalEntryDetail
                            {
                                AccountId = inventoryAccount.Id,
                                Debit = totalInvoiceAmount,
                                Credit = 0,
                                Description = "توريد بضاعة للمخزن"
                            });

                            // السطر الدائن (إلى ح/ المورد أو الصندوق)
                            entry.JournalEntryDetails.Add(new JournalEntryDetail
                            {
                                AccountId = creditAccount.Id,
                                Debit = 0,
                                Credit = totalInvoiceAmount,
                                Description = $"فاتورة شراء {invoice.SupplierInvoiceNumber}"
                            });

                            db.JournalEntries.Add(entry);
                            db.SaveChanges(); // حفظ القيد

                            // ربط الفاتورة بالقيد
                            invoice.JournalEntryId = entry.Id;
                            db.Entry(invoice).State = EntityState.Modified;
                            db.SaveChanges();
                        }

                        transaction.Commit();

                        // تسجيل العملية
                        AuditService.LogAction("Create Purchase Invoice", "PurchaseInvoices", $"Invoice #{invoice.Id}, Amount: {totalInvoiceAmount}");

                        TempData["SuccessMessage"] = "تم حفظ فاتورة الشراء وتحديث المخزون بنجاح.";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        AuditService.LogAction("Error", "PurchaseInvoices", $"Create Failed: {ex.Message}");
                        ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                    }
                }
            }

            ViewBag.SupplierId = new SelectList(db.Suppliers.Where(s => s.IsActive), "Id", "Name", model.SupplierId);
            ViewBag.ItemsList = db.Items.Where(i => i.IsActive).Select(i => new { i.Id, i.Name }).ToList();
            return View(model);
        }

        // التفاصيل والطباعة
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var invoice = db.PurchaseInvoices
                .Include(p => p.Supplier)
                .Include(p => p.Items.Select(i => i.Item))
                .FirstOrDefault(p => p.Id == id);

            if (invoice == null) return HttpNotFound();
            return View(invoice);
        }

        // الحذف (إلغاء التوريد وعكس القيد)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var invoice = db.PurchaseInvoices.Include(p => p.Items).FirstOrDefault(p => p.Id == id);
                    if (invoice == null) return HttpNotFound();

                    // 1. عكس أثر المخزون (إنقاص الكميات)
                    foreach (var invItem in invoice.Items)
                    {
                        var stockItem = db.Items.Find(invItem.ItemId);
                        if (stockItem != null)
                        {
                            stockItem.CurrentQuantity -= invItem.Quantity;
                            // ملاحظة: لا نعدل متوسط التكلفة عند الحذف لتجنب تعقيد الحسابات
                            db.Entry(stockItem).State = EntityState.Modified;
                        }
                    }

                    // 2. حذف القيد المالي المرتبط (إن وجد)
                    if (invoice.JournalEntryId.HasValue)
                    {
                        // جلب القيد مع تفاصيله باستخدام الاسم الجديد
                        var entry = db.JournalEntries
                                      .Include(j => j.JournalEntryDetails)
                                      .FirstOrDefault(j => j.Id == invoice.JournalEntryId);

                        if (entry != null)
                        {
                            // ⚠️ هام: الحذف يتم من الجدول الأصلي JournalEntryDetails
                            db.JournalEntryDetails.RemoveRange(entry.JournalEntryDetails);
                            db.JournalEntries.Remove(entry);
                        }
                    }

                    // 3. حذف تفاصيل الفاتورة والفاتورة نفسها
                    db.PurchaseInvoiceItems.RemoveRange(invoice.Items);
                    db.PurchaseInvoices.Remove(invoice);

                    db.SaveChanges();
                    transaction.Commit();

                    AuditService.LogAction("Delete Purchase Invoice", "PurchaseInvoices", $"Deleted Invoice #{id}");
                    TempData["SuccessMessage"] = "تم إلغاء التوريد وحذف الفاتورة والقيد بنجاح.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "تعذر الحذف: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }
    }
}