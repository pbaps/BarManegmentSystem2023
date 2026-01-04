using BarManegment.Helpers;
using BarManegment.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "FinancialSetup")] // أو FinancialReports
    public class ExchangeRatesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // عرض سجل الأسعار
        public ActionResult Index()
        {
            // جلب آخر سعر لكل عملة لعرضه في الأعلى
            var latestRates = db.ExchangeRates
                .Include(x => x.Currency)
                .GroupBy(x => x.CurrencyId)
                .Select(g => g.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).FirstOrDefault())
                .ToList();

            ViewBag.LatestRates = latestRates;

            // جلب السجل الكامل
            var history = db.ExchangeRates
                .Include(x => x.Currency)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Take(50)
                .ToList();

            return View(history);
        }

        // إضافة سعر جديد
        public ActionResult Create()
        {
            // نستثني الشيكل لأن سعره مقابل نفسه = 1 دائماً
            var currencies = db.Currencies
                .Where(c => c.Symbol != "₪" && c.Symbol != "NIS")
                .ToList();

            ViewBag.CurrencyId = new SelectList(currencies, "Id", "Name");
            return View(new ExchangeRate { Date = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ExchangeRate model)
        {
            if (ModelState.IsValid)
            {
                // التحقق من المنطق
                if (model.Rate <= 0)
                {
                    ModelState.AddModelError("Rate", "سعر الصرف يجب أن يكون أكبر من صفر.");
                }
                else
                {
                    model.CreatedBy = Session["FullName"] as string ?? "System";
                    db.ExchangeRates.Add(model);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "تم تحديث سعر الصرف بنجاح.";
                    return RedirectToAction("Index");
                }
            }

            var currencies = db.Currencies
                .Where(c => c.Symbol != "₪" && c.Symbol != "NIS")
                .ToList();
            ViewBag.CurrencyId = new SelectList(currencies, "Id", "Name", model.CurrencyId);

            return View(model);
        }

        // حذف سعر (في حال الخطأ فقط)
        [HttpPost]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int id)
        {
            var rate = db.ExchangeRates.Find(id);
            if (rate != null)
            {
                db.ExchangeRates.Remove(rate);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حذف السجل.";
            }
            return RedirectToAction("Index");
        }
    }
}