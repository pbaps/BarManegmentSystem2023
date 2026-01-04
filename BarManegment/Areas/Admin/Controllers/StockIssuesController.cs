using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using System.Net;
using BarManegment.Services;

namespace BarManegment.Areas.Admin.Controllers
{
    [Authorize]
    [CustomAuthorize(Permission = "CanView")]
    public class StockIssuesController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View(db.StockIssues.OrderByDescending(s => s.IssueDate).ToList());
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            ViewBag.EmployeeId = new SelectList(db.Users.Where(u => u.IsActive), "Id", "FullNameArabic");

            // عرض القائمة مع الكميات المتوفرة
            ViewBag.ItemsList = db.Items.Where(i => i.IsActive && i.CurrentQuantity > 0)
                                        .Select(i => new { i.Id, Name = i.Name + " (متوفر: " + i.CurrentQuantity + ")" })
                                        .ToList();

            return View(new StockIssueViewModel { IssueDate = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(StockIssueViewModel model)
        {
            if (ModelState.IsValid && model.Items != null && model.Items.Any())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. إنشاء سجل الصرف المخزني
                        var issue = new StockIssue
                        {
                            IssueDate = model.IssueDate,
                            DepartmentName = model.DepartmentName,
                            EmployeeId = model.EmployeeId,
                            Notes = model.Notes,
                            IssuedByUserId = (int?)Session["UserId"] ?? 1,
                            Items = new List<StockIssueItem>()
                        };

                        decimal totalCost = 0;

                        // 2. معالجة الأصناف
                        foreach (var itemVM in model.Items)
                        {
                            if (itemVM.Quantity <= 0) continue;

                            var itemEntity = db.Items.Find(itemVM.ItemId);

                            // التحقق من توفر الكمية
                            if (itemEntity.CurrentQuantity < itemVM.Quantity)
                            {
                                throw new Exception($"الكمية غير متوفرة للصنف: {itemEntity.Name}");
                            }

                            // حفظ التكلفة (المتوسط المرجح) لحظة الصرف
                            decimal lineCost = itemVM.Quantity * itemEntity.AverageCost;
                            totalCost += lineCost;

                            issue.Items.Add(new StockIssueItem
                            {
                                ItemId = itemVM.ItemId,
                                Quantity = itemVM.Quantity,
                                UnitCostSnapshot = itemEntity.AverageCost
                            });

                            // خصم المخزون
                            itemEntity.CurrentQuantity -= itemVM.Quantity;
                            db.Entry(itemEntity).State = EntityState.Modified;
                        }

                        issue.IsPosted = true;
                        db.StockIssues.Add(issue);
                        db.SaveChanges(); // للحصول على ID

                        // 3. القيد المحاسبي: من ح/ المصروفات إلى ح/ المخزون
                        var expenseAccount = db.Accounts.FirstOrDefault(a => a.Code == "5104" || a.Name.Contains("قرطاسية") || a.Code.StartsWith("5"));
                        var inventoryAccount = db.Accounts.FirstOrDefault(a => a.Name.Contains("مخزون") || a.Code.StartsWith("12"));

                        if (expenseAccount != null && inventoryAccount != null)
                        {
                            var fiscalYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                            int fiscalYearId = fiscalYear?.Id ?? 1;

                            // توليد رقم القيد (String)
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
                                EntryDate = issue.IssueDate,
                                Description = $"صرف مخزني رقم {issue.Id} - {issue.DepartmentName}",
                                SourceModule = "StockIssues",
                                SourceId = issue.Id,
                                IsPosted = true,
                                PostedDate = DateTime.Now,
                                PostedByUserId = (int?)Session["UserId"] ?? 1,
                                TotalDebit = totalCost,
                                TotalCredit = totalCost,
                                JournalEntryDetails = new List<JournalEntryDetail>() // ✅ استخدام الاسم الصحيح
                            };

                            // المدين (المصروف)
                            entry.JournalEntryDetails.Add(new JournalEntryDetail
                            {
                                AccountId = expenseAccount.Id,
                                Debit = totalCost,
                                Credit = 0,
                                Description = "مصاريف مهمات ومواد"
                            });

                            // الدائن (المخزون)
                            entry.JournalEntryDetails.Add(new JournalEntryDetail
                            {
                                AccountId = inventoryAccount.Id,
                                Debit = 0,
                                Credit = totalCost,
                                Description = "صرف من المخزون"
                            });

                            db.JournalEntries.Add(entry);
                            db.SaveChanges();

                            issue.JournalEntryId = entry.Id;
                            db.Entry(issue).State = EntityState.Modified;
                            db.SaveChanges();
                        }

                        transaction.Commit();

                        AuditService.LogAction("Create Stock Issue", "StockIssues", $"Issue #{issue.Id} To: {model.DepartmentName}, Cost: {totalCost}");
                        TempData["SuccessMessage"] = "تم صرف المواد بنجاح.";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "خطأ: " + ex.Message);
                    }
                }
            }

            ViewBag.EmployeeId = new SelectList(db.Users.Where(u => u.IsActive), "Id", "FullNameArabic", model.EmployeeId);
            ViewBag.ItemsList = db.Items.Where(i => i.IsActive).Select(i => new { i.Id, Name = i.Name + " (متوفر: " + i.CurrentQuantity + ")" }).ToList();
            return View(model);
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var issue = db.StockIssues
                .Include(s => s.Items.Select(i => i.Item))
                .FirstOrDefault(s => s.Id == id);

            if (issue == null) return HttpNotFound();
            return View(issue);
        }

        // الحذف (إلغاء الصرف وإرجاع للمخزن)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var issue = db.StockIssues.Include(s => s.Items).FirstOrDefault(s => s.Id == id);
                    if (issue == null) return HttpNotFound();

                    // 1. عكس أثر المخزون (زيادة الكميات)
                    foreach (var item in issue.Items)
                    {
                        var stockItem = db.Items.Find(item.ItemId);
                        if (stockItem != null)
                        {
                            stockItem.CurrentQuantity += item.Quantity; // إعادة للمخزن
                            db.Entry(stockItem).State = EntityState.Modified;
                        }
                    }

                    // 2. حذف القيد المالي
                    if (issue.JournalEntryId.HasValue)
                    {
                        // ✅ استخدام JournalEntryDetails
                        var entry = db.JournalEntries
                                      .Include(j => j.JournalEntryDetails)
                                      .FirstOrDefault(j => j.Id == issue.JournalEntryId);

                        if (entry != null)
                        {
                            // ✅ الحذف يتم من الجدول الأصلي JournalEntryDetails
                            db.JournalEntryDetails.RemoveRange(entry.JournalEntryDetails);
                            db.JournalEntries.Remove(entry);
                        }
                    }

                    db.StockIssueItems.RemoveRange(issue.Items);
                    db.StockIssues.Remove(issue);

                    db.SaveChanges();
                    transaction.Commit();

                    AuditService.LogAction("Delete Stock Issue", "StockIssues", $"Deleted Issue #{id}");
                    TempData["SuccessMessage"] = "تم إلغاء الصرف وإعادة المواد للمخزن.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "تعذر الإلغاء: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}