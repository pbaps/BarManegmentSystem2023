using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System;
using System.IO; // (لرفع الملفات)
using System.Web;

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class TrainingLogController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // (هذه الدالة ستعرض كل سجلات المتدرب)
        // GET: Members/TrainingLog
        public ActionResult Index()
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            var logs = db.TrainingLogs
                .Include(l => l.Supervisor)
                .Where(l => l.GraduateApplicationId == graduateApp.Id)
                .OrderByDescending(l => l.Year)
                .ThenByDescending(l => l.Month)
                .ToList();

            return View(logs);
        }

        // (هذه الدالة لتقديم سجل جديد)
        // GET: Members/TrainingLog/Create
        public ActionResult Create()
        {
            // (تعبئة الشهر والسنة افتراضياً)
            var model = new TrainingLog
            {
                Year = DateTime.Now.Year,
                Month = DateTime.Now.Month
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TrainingLog model, HttpPostedFileBase uploadedFile)
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            if (!graduateApp.SupervisorId.HasValue)
            {
                ModelState.AddModelError("", "لا يمكنك تقديم سجل تدريب لأنه لا يوجد مشرف معين لك حالياً.");
            }

            // (التحقق من عدم تكرار السجل لنفس الشهر والسنة)
            bool alreadySubmitted = db.TrainingLogs.Any(l =>
                l.GraduateApplicationId == graduateApp.Id &&
                l.Year == model.Year &&
                l.Month == model.Month);

            if (alreadySubmitted)
            {
                ModelState.AddModelError("", $"لقد قمت بتقديم سجل شهر {model.Month}/{model.Year} مسبقاً.");
            }
            ModelState.Remove("Status");
            if (ModelState.IsValid)
            {
                // (حفظ الملف المرفق إن وجد)
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    string directoryPath = Server.MapPath($"~/Uploads/TrainingLogs/{graduateApp.Id}");
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    string fileName = $"{model.Year}-{model.Month}-{Path.GetFileName(uploadedFile.FileName)}";
                    string path = Path.Combine(directoryPath, fileName);
                    uploadedFile.SaveAs(path);
                    model.FilePath = $"/Uploads/TrainingLogs/{graduateApp.Id}/{fileName}";

                }

                model.GraduateApplicationId = graduateApp.Id;
                model.SupervisorId = graduateApp.SupervisorId; // (تحديد المشرف عند التقديم)
                model.SubmissionDate = DateTime.Now;
                model.Status = "بانتظار موافقة المشرف";

                db.TrainingLogs.Add(model);
                db.SaveChanges();

                TempData["SuccessMessage"] = "تم إرسال سجل التدريب الشهري للمشرف بنجاح.";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}