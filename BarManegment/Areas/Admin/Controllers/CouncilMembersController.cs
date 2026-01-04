using BarManegment.Helpers;
using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class CouncilMembersController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/CouncilMembers
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult Index()
        {
            return View(db.CouncilMembers.ToList());
        }

        // GET: Admin/CouncilMembers/Create
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create([Bind(Include = "Id,Name,Title,IsActive")] CouncilMember councilMember)
        {
            if (ModelState.IsValid)
            {
                db.CouncilMembers.Add(councilMember);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(councilMember);
        }

        // GET: Admin/CouncilMembers/Edit/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CouncilMember councilMember = db.CouncilMembers.Find(id);
            if (councilMember == null)
            {
                return HttpNotFound();
            }
            return View(councilMember);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,Name,Title,IsActive")] CouncilMember councilMember)
        {
            if (ModelState.IsValid)
            {
                db.Entry(councilMember).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(councilMember);
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
