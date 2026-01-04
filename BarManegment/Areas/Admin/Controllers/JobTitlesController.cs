using BarManegment.Models;
using BarManegment.Helpers;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class JobTitlesController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View(db.JobTitles.ToList());
        }

        public ActionResult Create() { return View(); }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(JobTitle jobTitle)
        {
            if (ModelState.IsValid)
            {
                db.JobTitles.Add(jobTitle);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(jobTitle);
        }


        // --- تعديل (Edit) ---
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var jobTitle = db.JobTitles.Find(id);
            if (jobTitle == null) return HttpNotFound();
            return View(jobTitle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(JobTitle jobTitle)
        {
            if (ModelState.IsValid)
            {
                db.Entry(jobTitle).State = EntityState.Modified;
                db.SaveChanges();
                BarManegment.Services.AuditService.LogAction("Edit JobTitle", "JobTitles", $"Updated ID: {jobTitle.Id}");
                return RedirectToAction("Index");
            }
            return View(jobTitle);
        }

        // --- حذف (Delete) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int id)
        {
            var jobTitle = db.JobTitles.Find(id);
            if (jobTitle != null)
            {
                // حماية: منع الحذف إذا كان المسمى مستخدماً
                if (db.Employees.Any(e => e.JobTitleId == id))
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف هذا المسمى الوظيفي لأنه مرتبط بموظفين حاليين.";
                    return RedirectToAction("Index");
                }

                db.JobTitles.Remove(jobTitle);
                db.SaveChanges();
                BarManegment.Services.AuditService.LogAction("Delete JobTitle", "JobTitles", $"Deleted ID: {id}");
                TempData["SuccessMessage"] = "تم حذف المسمى الوظيفي بنجاح.";
            }
            return RedirectToAction("Index");
        }
    }
}