using BarManegment.Models;
using BarManegment.Helpers;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    // (نفترض أننا سنستخدم نفس صلاحية "LookupManagement" أو "Provinces" كصلاحية عامة للجداول المساعدة)
    [CustomAuthorize(Permission = "CanView")]
    public class PartyRolesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/PartyRoles
        public ActionResult Index()
        {
            return View(db.PartyRoles.ToList());
        }

        // GET: Admin/PartyRoles/Create
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/PartyRoles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create([Bind(Include = "Id,Name")] PartyRole partyRole)
        {
            if (ModelState.IsValid)
            {
                db.PartyRoles.Add(partyRole);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم إضافة صفة الطرف بنجاح.";
                return RedirectToAction("Index");
            }
            return View(partyRole);
        }

        // GET: Admin/PartyRoles/Edit/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PartyRole partyRole = db.PartyRoles.Find(id);
            if (partyRole == null)
            {
                return HttpNotFound();
            }
            return View(partyRole);
        }

        // POST: Admin/PartyRoles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,Name")] PartyRole partyRole)
        {
            if (ModelState.IsValid)
            {
                db.Entry(partyRole).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل صفة الطرف بنجاح.";
                return RedirectToAction("Index");
            }
            return View(partyRole);
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