using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class LecturesController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Members/Lectures
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Members" });
            }
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);
            if (graduateApp == null) return HttpNotFound();

            // جلب كل المحاضرات (القديمة والجديدة) المسجل بها المتدرب
            var myLectures = db.TraineeAttendances
                .Include(att => att.Session)
                .Include(att => att.Session.TrainingCourse) // لجلب اسم الدورة
                .Where(att => att.TraineeId == graduateApp.Id)
                .OrderByDescending(att => att.Session.SessionDate) // عرض الأحدث أولاً
                .ToList();

            return View(myLectures);
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