using BarManegment.Helpers;
using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class ExamTypesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View(db.ExamTypes.ToList());
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create([Bind(Include = "Id,Name")] ExamType examType)
        {
            if (ModelState.IsValid)
            {
                db.ExamTypes.Add(examType);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(examType);
        }

        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            ExamType examType = db.ExamTypes.Find(id);
            if (examType == null) return HttpNotFound();
            return View(examType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,Name")] ExamType examType)
        {
            if (ModelState.IsValid)
            {
                db.Entry(examType).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(examType);
        }
    }
}
