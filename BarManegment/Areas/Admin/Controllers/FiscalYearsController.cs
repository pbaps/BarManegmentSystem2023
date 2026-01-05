using BarManegment.Helpers;
using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "FinancialSetup")]
    public class FiscalYearsController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // 1. عرض السنوات المالية
        public ActionResult Index()
        {
            return View(db.FiscalYears.OrderByDescending(f => f.StartDate).ToList());
        }

        // 2. إنشاء سنة جديدة (GET)
        public ActionResult Create()
        {
            return View();
        }

        // 3. حفظ السنة الجديدة (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FiscalYear fiscalYear)
        {
            if (ModelState.IsValid)
            {
                if (fiscalYear.IsCurrent)
                {
                    var otherYears = db.FiscalYears.Where(f => f.IsCurrent).ToList();
                    foreach (var year in otherYears)
                    {
                        year.IsCurrent = false;
                        db.Entry(year).State = EntityState.Modified;
                    }
                }

                db.FiscalYears.Add(fiscalYear);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تمت إضافة السنة المالية بنجاح.";
                return RedirectToAction("Index");
            }

            return View(fiscalYear);
        }

        // 4. تعديل سنة (GET)
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var fiscalYear = db.FiscalYears.Find(id);
            if (fiscalYear == null) return HttpNotFound();
            return View(fiscalYear);
        }

        // 5. حفظ التعديل (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FiscalYear fiscalYear)
        {
            if (ModelState.IsValid)
            {
                if (fiscalYear.IsCurrent)
                {
                    var otherYears = db.FiscalYears.Where(f => f.Id != fiscalYear.Id && f.IsCurrent).ToList();
                    foreach (var year in otherYears)
                    {
                        year.IsCurrent = false;
                        db.Entry(year).State = EntityState.Modified;
                    }
                }

                db.Entry(fiscalYear).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل السنة المالية بنجاح.";
                return RedirectToAction("Index");
            }
            return View(fiscalYear);
        }

        // 6. الحذف (POST) - تم التعديل ليكون آمناً
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var fiscalYear = db.FiscalYears.Find(id);
            if (fiscalYear == null)
            {
                TempData["ErrorMessage"] = "السنة المالية غير موجودة.";
                return RedirectToAction("Index");
            }

            // قيد الحذف: منع الحذف إذا كانت هناك قيود مرتبطة بهذه السنة
            if (db.JournalEntries.Any(j => j.FiscalYearId == id))
            {
                TempData["ErrorMessage"] = "عذراً، لا يمكن حذف هذه السنة المالية لوجود قيود مالية وحركات مرتبطة بها.";
                return RedirectToAction("Index");
            }

            db.FiscalYears.Remove(fiscalYear);
            db.SaveChanges();
            TempData["SuccessMessage"] = "تم حذف السنة المالية بنجاح.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}