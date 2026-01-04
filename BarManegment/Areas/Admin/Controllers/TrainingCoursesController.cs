using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services; // 💡 ضروري
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class TrainingCoursesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var courses = db.TrainingCourses.Include(c => c.Sessions).ToList();
            return View(courses);
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create([Bind(Include = "Id,CourseName,Description")] TrainingCourse trainingCourse)
        {
            if (ModelState.IsValid)
            {
                db.TrainingCourses.Add(trainingCourse);
                db.SaveChanges();

                // ✅ تسجيل التدقيق
                AuditService.LogAction("Create Course", "TrainingCourses", $"Created course: {trainingCourse.CourseName}");

                TempData["SuccessMessage"] = "تمت إضافة الدورة بنجاح.";
                return RedirectToAction("Index");
            }
            return View(trainingCourse);
        }

        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            TrainingCourse trainingCourse = db.TrainingCourses.Find(id);
            if (trainingCourse == null) return HttpNotFound();
            return View(trainingCourse);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,CourseName,Description")] TrainingCourse trainingCourse)
        {
            if (ModelState.IsValid)
            {
                db.Entry(trainingCourse).State = EntityState.Modified;
                db.SaveChanges();

                // ✅ تسجيل التدقيق
                AuditService.LogAction("Edit Course", "TrainingCourses", $"Updated course ID: {trainingCourse.Id}");

                TempData["SuccessMessage"] = "تم تعديل بيانات الدورة بنجاح.";
                return RedirectToAction("Index");
            }
            return View(trainingCourse);
        }

        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            TrainingCourse trainingCourse = db.TrainingCourses.Find(id);
            if (trainingCourse == null) return HttpNotFound();
            return View(trainingCourse);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteConfirmed(int id)
        {
            TrainingCourse trainingCourse = db.TrainingCourses.Include(c => c.Sessions).FirstOrDefault(c => c.Id == id);
            if (trainingCourse != null)
            {
                if (trainingCourse.Sessions.Any())
                {
                    db.TrainingSessions.RemoveRange(trainingCourse.Sessions);
                }

                db.TrainingCourses.Remove(trainingCourse);
                db.SaveChanges();

                // ✅ تسجيل التدقيق
                AuditService.LogAction("Delete Course", "TrainingCourses", $"Deleted course ID: {id} and its sessions.");

                TempData["SuccessMessage"] = "تم حذف الدورة والجلسات المرتبطة بها.";
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