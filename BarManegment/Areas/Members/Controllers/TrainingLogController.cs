using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System;
using System.IO;
using System.Web;
using System.Collections.Generic;

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class TrainingLogController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // =========================================================
        // 1. عرض سجلات المتدرب (الأرشيف)
        // =========================================================
        public ActionResult Index()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            if (graduateApp == null) return HttpNotFound();

            var logs = db.TrainingLogs
                .Include(l => l.Supervisor)
                .Where(l => l.GraduateApplicationId == graduateApp.Id)
                .OrderByDescending(l => l.Year)
                .ThenByDescending(l => l.Month)
                .ToList();

            return View(logs);
        }

        // =========================================================
        // 2. صفحة تقديم سجل جديد (GET)
        // =========================================================
        public ActionResult Create()
        {
            // تعبئة الشهر والسنة الحالية افتراضياً للتسهيل
            var model = new TrainingLog
            {
                Year = DateTime.Now.Year,
                Month = DateTime.Now.Month,
                CasesCount = 0 // القيمة الافتراضية
            };
            return View(model);
        }

        // =========================================================
        // 3. حفظ السجل الجديد (POST)
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TrainingLog model, HttpPostedFileBase uploadedFile)
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            // 1. التحقق من وجود مشرف
            if (!graduateApp.SupervisorId.HasValue)
            {
                TempData["ErrorMessage"] = "لا يمكنك تقديم سجل تدريب لأنه لا يوجد مشرف معين لك حالياً.";
                return RedirectToAction("Index");
            }

            // 2. التحقق من التكرار (هل قدم لهذا الشهر من قبل؟)
            bool alreadySubmitted = db.TrainingLogs.Any(l =>
                l.GraduateApplicationId == graduateApp.Id &&
                l.Year == model.Year &&
                l.Month == model.Month);

            if (alreadySubmitted)
            {
                ModelState.AddModelError("", $"لقد قمت بتقديم سجل شهر {model.Month}/{model.Year} مسبقاً.");
            }

            // إزالة الحقول التي لا يدخلها المستخدم من التحقق
            ModelState.Remove("Status");
            ModelState.Remove("SupervisorNotes");

            if (ModelState.IsValid)
            {
                // 3. معالجة الملف المرفق (الجزء الأهم)
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    // التحقق من الامتداد (Security Check)
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var ext = Path.GetExtension(uploadedFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("uploadedFile", "نوع الملف غير مدعوم. يرجى رفع ملف PDF أو صورة فقط.");
                        return View(model);
                    }

                    // إنشاء اسم فريد للملف (لتجنب مشاكل الأسماء العربية والتكرار)
                    string fileName = Guid.NewGuid().ToString() + ext;

                    // مسار المجلد (ننشئ مجلد لكل متدرب لتنظيم الملفات)
                    string folderName = $"Uploads/TrainingLogs/{graduateApp.Id}";
                    string serverPath = Server.MapPath("~/" + folderName);

                    // التأكد من وجود المجلد
                    if (!Directory.Exists(serverPath))
                    {
                        Directory.CreateDirectory(serverPath);
                    }

                    // الحفظ الفعلي
                    string fullPath = Path.Combine(serverPath, fileName);
                    uploadedFile.SaveAs(fullPath);

                    // حفظ المسار النسبي في قاعدة البيانات (تحديثنا الجديد)
                    model.FilePath = "~/" + folderName + "/" + fileName;
                }

                // 4. إكمال بيانات النموذج
                model.GraduateApplicationId = graduateApp.Id;
                model.SupervisorId = graduateApp.SupervisorId; // ربط بالمشرف الحالي
                model.SubmissionDate = DateTime.Now;
                model.Status = "بانتظار موافقة المشرف";

                // 5. الحفظ في قاعدة البيانات
                db.TrainingLogs.Add(model);
                db.SaveChanges();

                TempData["SuccessMessage"] = "تم إرسال سجل التدريب الشهري للمشرف بنجاح.";
                return RedirectToAction("Index", "Dashboard"); // العودة للوحة التحكم الرئيسية
            }

            // في حال وجود أخطاء، نعيد العرض
            return View(model);
        }

        // =========================================================
        // 4. تنظيف الموارد
        // =========================================================
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}