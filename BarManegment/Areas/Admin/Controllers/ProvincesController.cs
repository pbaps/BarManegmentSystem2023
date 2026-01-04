using BarManegment.Models;
using BarManegment.Helpers;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    // (نفترض أننا سنضيف صلاحية "LookupManagement" لإدارة كل الجداول المساعدة)
    [CustomAuthorize(Permission = "CanView")]
    public class ProvincesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/Provinces
        public ActionResult Index()
        {
            return View(db.Provinces.ToList());
        }

        // GET: Admin/Provinces/Create
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/Provinces/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create([Bind(Include = "Id,Name")] Province province)
        {
            if (ModelState.IsValid)
            {
                db.Provinces.Add(province);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم إضافة المحافظة بنجاح.";
                return RedirectToAction("Index");
            }
            return View(province);
        }

        // GET: Admin/Provinces/Edit/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Province province = db.Provinces.Find(id);
            if (province == null)
            {
                return HttpNotFound();
            }
            return View(province);
        }

        // POST: Admin/Provinces/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,Name")] Province province)
        {
            if (ModelState.IsValid)
            {
                db.Entry(province).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل المحافظة بنجاح.";
                return RedirectToAction("Index");
            }
            return View(province);
        }

        // (يمكن إضافة دالة الحذف (Delete) لاحقاً إذا احتجت إليها)

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