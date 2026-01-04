using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "FinancialReports")] // تأكد من الصلاحية
    public class OpeningBalancesController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // 1. عرض الأرصدة الافتتاحية
        public ActionResult Index()
        {
            // نفترض أن الأرصدة الافتتاحية هي قيد من نوع "OpeningBalance"
            var openingEntries = db.JournalEntries
                // .Include(j => j.FiscalYear)
                .Where(j => j.SourceModule == "OpeningBalance")
                .OrderByDescending(j => j.EntryDate)
                .ToList();

            return View(openingEntries);
        }

        // 2. إنشاء قيد افتتاحي
        public ActionResult Create()
        {
            var currentYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
            if (currentYear == null)
            {
                TempData["ErrorMessage"] = "لا توجد سنة مالية مفتوحة.";
                return RedirectToAction("Index");
            }

            ViewBag.FiscalYearName = currentYear.Name;

            // جلب الحسابات لاستخدامها في الـ View
            var accounts = db.Accounts.Where(a => a.IsTransactional).Select(a => new { a.Id, Name = a.Code + " - " + a.Name }).ToList();
            ViewBag.Accounts = new SelectList(accounts, "Id", "Name");

            return View(new JournalEntryViewModel { EntryDate = currentYear.StartDate });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(JournalEntryViewModel model)
        {
            if (ModelState.IsValid)
            {
                // التحقق من التوازن
                decimal totalDebit = model.Lines.Sum(l => l.Debit);
                decimal totalCredit = model.Lines.Sum(l => l.Credit);

                if (totalDebit != totalCredit)
                {
                    ModelState.AddModelError("", $"القيد غير متوازن. الفرق: {Math.Abs(totalDebit - totalCredit)}");

                    var accounts = db.Accounts.Where(a => a.IsTransactional).Select(a => new { a.Id, Name = a.Code + " - " + a.Name }).ToList();
                    ViewBag.Accounts = new SelectList(accounts, "Id", "Name");
                    return View(model);
                }

                var currentYear = db.FiscalYears.FirstOrDefault(y => y.IsCurrent && !y.IsClosed);
                if (currentYear == null) return HttpNotFound("السنة المالية مغلقة");

                // توليد رقم القيد (نصي)
                string nextEntryNo = "1";
                if (db.JournalEntries.Any(j => j.FiscalYearId == currentYear.Id))
                {
                    var lastEntry = db.JournalEntries
                                      .Where(j => j.FiscalYearId == currentYear.Id)
                                      .OrderByDescending(j => j.Id)
                                      .FirstOrDefault();

                    if (lastEntry != null && int.TryParse(lastEntry.EntryNumber, out int lastNo))
                        nextEntryNo = (lastNo + 1).ToString();
                    else
                        nextEntryNo = (db.JournalEntries.Count(j => j.FiscalYearId == currentYear.Id) + 1).ToString();
                }

                var entry = new JournalEntry
                {
                    FiscalYearId = currentYear.Id,
                    EntryNumber = nextEntryNo, // أصبح string
                    EntryDate = model.EntryDate,
                    Description = "قيد افتتاحي - " + model.Description,
                    SourceModule = "OpeningBalance",
                    IsPosted = true, // ترحيل مباشر للأرصدة الافتتاحية
                    PostedDate = DateTime.Now,
                    PostedByUserId = (int?)Session["UserId"] ?? 1,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
                    JournalEntryDetails = new List<JournalEntryDetail>() // الاسم الجديد
                };

                foreach (var line in model.Lines)
                {
                    if (line.Debit == 0 && line.Credit == 0) continue;

                    entry.JournalEntryDetails.Add(new JournalEntryDetail
                    {
                        AccountId = line.AccountId,
                        Debit = line.Debit,
                        Credit = line.Credit,
                        Description = line.LineDescription ?? "رصيد افتتاحي"
                    });
                }

                db.JournalEntries.Add(entry);
                db.SaveChanges();

                AuditService.LogAction("Create Opening Balance", "OpeningBalances", $"Entry #{entry.EntryNumber}");
                TempData["SuccessMessage"] = "تم حفظ الأرصدة الافتتاحية بنجاح.";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // 3. الحذف (اختياري - بحذر)
        [HttpPost]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int id)
        {
            var entry = db.JournalEntries.Include(j => j.JournalEntryDetails).FirstOrDefault(j => j.Id == id);
            if (entry != null && entry.SourceModule == "OpeningBalance")
            {
                // الحذف من الجدول الأصلي JournalEntryDetails
                db.JournalEntryDetails.RemoveRange(entry.JournalEntryDetails);
                db.JournalEntries.Remove(entry);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حذف القيد الافتتاحي.";
            }
            return RedirectToAction("Index");
        }
    }
}