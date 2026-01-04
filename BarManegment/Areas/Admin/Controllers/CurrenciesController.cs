using BarManegment.Helpers;
using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class CurrenciesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View(db.Currencies.ToList());
        }
        // صلاحية "الإضافة" مطلوبة لعرض صفحة الإنشاء
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // صلاحية "الإضافة" مطلوبة لعرض صفحة الإنشاء
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create([Bind(Include = "Id,Name,Symbol")] Currency currency)
        {
            if (ModelState.IsValid)
            {
                db.Currencies.Add(currency);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(currency);
        }
        // صلاحية "التعديل" مطلوبة لعرض صفحة التعديل
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Currency currency = db.Currencies.Find(id);
            if (currency == null)
            {
                return HttpNotFound();
            }
            return View(currency);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // صلاحية "التعديل" مطلوبة لعرض صفحة التعديل
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,Name,Symbol")] Currency currency)
        {
            if (ModelState.IsValid)
            {
                db.Entry(currency).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(currency);
        }
    }
}
