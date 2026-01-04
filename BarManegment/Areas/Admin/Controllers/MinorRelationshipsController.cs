using BarManegment.Models;
using BarManegment.Helpers;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    // (هذا هو المتحكم الجديد الذي يستبدل PassportGuardianRolesController)
    [CustomAuthorize(Permission = "CanView")]
    public class MinorRelationshipsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/MinorRelationships
        public ActionResult Index()
        {
            return View(db.MinorRelationships.ToList());
        }

        // GET: Admin/MinorRelationships/Create
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/MinorRelationships/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create([Bind(Include = "Name")] MinorRelationship minorRelationship)
        {
            if (ModelState.IsValid)
            {
                db.MinorRelationships.Add(minorRelationship);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم إضافة صفة القاصر بنجاح.";
                return RedirectToAction("Index");
            }
            return View(minorRelationship);
        }

        // GET: Admin/MinorRelationships/Edit/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            MinorRelationship minorRelationship = db.MinorRelationships.Find(id);
            if (minorRelationship == null)
            {
                return HttpNotFound();
            }
            return View(minorRelationship);
        }

        // POST: Admin/MinorRelationships/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,Name")] MinorRelationship minorRelationship)
        {
            if (ModelState.IsValid)
            {
                db.Entry(minorRelationship).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل صفة القاصر بنجاح.";
                return RedirectToAction("Index");
            }
            return View(minorRelationship);
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