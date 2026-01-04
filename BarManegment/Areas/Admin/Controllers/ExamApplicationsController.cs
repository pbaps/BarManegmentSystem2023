using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class ExamApplicationsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/ExamApplications
        // تم التحديث لدعم الفلترة حسب التصنيف (Categories/Tabs)
        public ActionResult Index(string searchTerm, string category)
        {
            var query = db.ExamApplications.Include(e => e.Gender).AsQueryable();

            // 1. البحث بالنص (مشترك للكل)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a => a.FullName.Contains(searchTerm) ||
                                         a.NationalIdNumber.Contains(searchTerm) ||
                                         a.Email.Contains(searchTerm));
            }

            // 2. حساب العدادات لجميع الفئات (للعرض في التبويبات)
            // نستخدم نفس شروط البحث للحساب ليكون العداد دقيقاً حسب البحث
            ViewBag.CountAll = db.ExamApplications.Count();
            ViewBag.CountPending = db.ExamApplications.Count(a => a.Status == "قيد المراجعة");
            ViewBag.CountAccepted = db.ExamApplications.Count(a => a.Status == "مقبول للامتحان");
            ViewBag.CountPassed = db.ExamApplications.Count(a => a.Status == "ناجح (بانتظار استكمال النواقص)");
            ViewBag.CountFailed = db.ExamApplications.Count(a => a.Status == "راسب");
            ViewBag.CountExempt = db.ExamApplications.Count(a => a.Status == "معفى (مؤهل للتسجيل)");
            ViewBag.CountRejected = db.ExamApplications.Count(a => a.Status == "مرفوض");

            // 3. تطبيق الفلترة حسب التبويب المختار
            switch (category)
            {
                case "Pending":
                    query = query.Where(a => a.Status == "قيد المراجعة");
                    break;
                case "Accepted":
                    query = query.Where(a => a.Status == "مقبول للامتحان");
                    break;
                case "Passed":
                    query = query.Where(a => a.Status == "ناجح (بانتظار استكمال النواقص)");
                    break;
                case "Failed":
                    query = query.Where(a => a.Status == "راسب");
                    break;
                case "Exempt":
                    query = query.Where(a => a.Status == "معفى (مؤهل للتسجيل)");
                    break;
                case "Rejected":
                    query = query.Where(a => a.Status == "مرفوض");
                    break;
                    // Default: All (لا فلترة)
            }

            ViewBag.CurrentCategory = category;
            ViewBag.SearchTerm = searchTerm;

            var applications = query.OrderByDescending(e => e.ApplicationDate).ToList();

            return View(applications);
        }

        // GET: Admin/ExamApplications/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var examApplication = db.ExamApplications
                .Include(e => e.Gender)
                .Include(e => e.Qualifications)
                .FirstOrDefault(e => e.Id == id);

            if (examApplication == null) return HttpNotFound();

            return View(examApplication);
        }

        // POST: Admin/ExamApplications/ProcessDecision
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ProcessDecision(int id, string decision, string rejectionReason)
        {
            var application = db.ExamApplications.Find(id);
            if (application == null) return HttpNotFound();

            if (decision == "Approve")
            {
                application.Status = "مقبول للامتحان";
            }
            else if (decision == "Reject")
            {
                if (string.IsNullOrWhiteSpace(rejectionReason))
                {
                    TempData["ErrorMessage"] = "سبب الرفض مطلوب.";
                    return RedirectToAction("Details", new { id = id });
                }
                application.Status = "مرفوض";
                application.RejectionReason = rejectionReason;
            }

            db.SaveChanges();
            AuditService.LogAction("Process Decision", "ExamApplications", $"App ID {id} status: {application.Status}");
            TempData["SuccessMessage"] = "تم تحديث الحالة بنجاح.";
            return RedirectToAction("Details", new { id = id });
        }

        // POST: Admin/ExamApplications/UpdateExamResult
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UpdateExamResult(int id, double score, double passingScore = 50)
        {
            var application = db.ExamApplications.Find(id);
            if (application == null) return HttpNotFound();

            application.ExamScore = score;

            if (score >= passingScore)
            {
                application.ExamResult = "ناجح";
                application.Status = "ناجح (بانتظار استكمال النواقص)";
            }
            else
            {
                application.ExamResult = "راسب";
                application.Status = "راسب";
            }

            db.SaveChanges();
            AuditService.LogAction("Update Exam Result", "ExamApplications", $"ID {id} Result: {application.ExamResult}");
            TempData["SuccessMessage"] = $"تم رصد النتيجة ({score}) وتحديث الحالة إلى {application.Status}";

            return RedirectToAction("Details", new { id = id });
        }

        // POST: Admin/ExamApplications/Exempt
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Exempt(int id)
        {
            var examApp = db.ExamApplications.Find(id);
            if (examApp == null) return HttpNotFound();

            if (examApp.IsAccountCreated)
            {
                TempData["ErrorMessage"] = "لا يمكن إعفاء هذا الطلب، لديه حساب بالفعل.";
                return RedirectToAction("Index");
            }

            examApp.Status = "معفى (مؤهل للتسجيل)";
            db.SaveChanges();
            AuditService.LogAction("Exempt", "ExamApplications", $"Applicant {examApp.FullName} exempted.");
            TempData["SuccessMessage"] = "تم إعفاء المتقدم بنجاح.";
            return RedirectToAction("Index");
        }

 
        // =========================================================================
        // 1. توليد كلمات مرور جماعية وتصديرها (للمقبولين الجدد)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanExport")]
        public ActionResult GenerateAndExportPasswords()
        {
            // جلب الأشخاص المقبولين للامتحان والذين ليس لديهم كلمة مرور بعد
            var applicants = db.ExamApplications
                .Where(a => a.Status == "مقبول للامتحان" && a.TemporaryPassword == null)
                .ToList();

            if (!applicants.Any())
            {
                TempData["InfoMessage"] = "لا يوجد متقدمون جدد (حالتهم 'مقبول للامتحان') لتوليد كلمات مرور لهم.";
                return RedirectToAction("Index");
            }

            var exportList = new List<dynamic>();

            foreach (var app in applicants)
            {
                // أ. إنشاء كلمة مرور عشوائية بسيطة (8 حروف)
                string plainTextPassword = Guid.NewGuid().ToString("N").Substring(0, 8);

                // ب. تشفير كلمة المرور وحفظها في قاعدة البيانات (لغرض التحقق لاحقاً)
                app.TemporaryPassword = PasswordHelper.HashPassword(plainTextPassword);

                // ج. حفظ كلمة المرور "غير المشفرة" في القائمة لتصديرها للإكسل (ليتم تسليمها للطالب)
                exportList.Add(new
                {
                    FullName = app.FullName,
                    NationalId = app.NationalIdNumber,
                    Mobile = app.MobileNumber,
                    Password = plainTextPassword // <-- هذه النسخة المقروءة
                });
            }

            db.SaveChanges(); // حفظ التشفير في القاعدة

            // د. إنشاء ملف الإكسل
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("بيانات الدخول");
                worksheet.View.RightToLeft = true;

                worksheet.Cells["A1"].Value = "الاسم الكامل";
                worksheet.Cells["B1"].Value = "الرقم الوطني (اسم المستخدم)";
                worksheet.Cells["C1"].Value = "رقم الجوال";
                worksheet.Cells["D1"].Value = "كلمة المرور"; // العمود المهم

                // تنسيق الترويسة
                using (var range = worksheet.Cells["A1:D1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                int row = 2;
                foreach (var item in exportList)
                {
                    worksheet.Cells[row, 1].Value = item.FullName;
                    worksheet.Cells[row, 2].Value = item.NationalId;
                    worksheet.Cells[row, 3].Value = item.Mobile;
                    worksheet.Cells[row, 4].Value = item.Password; // وضع كلمة المرور الظاهرة في الإكسل
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ExamPasswords-{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
        // ... (ResetPassword, ViewAttachment, UndoDecision remain as previously implemented) ...
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UndoDecision(int id)
        {
            var application = db.ExamApplications.Find(id);
            if (application == null) return HttpNotFound();
            application.Status = "قيد المراجعة";
            application.RejectionReason = null;
            application.ExamResult = null;
            application.ExamScore = null;
            db.SaveChanges();
            TempData["SuccessMessage"] = "تم التراجع عن القرار.";
            return RedirectToAction("Details", new { id = id });
        }

        // =========================================================================
        // 2. إعادة تعيين كلمة مرور لشخص واحد (Reset)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ResetPassword(int id)
        {
            var application = db.ExamApplications.Find(id);
            if (application == null) return HttpNotFound();

            // توليد كلمة مرور جديدة
            string plainTextPassword = Guid.NewGuid().ToString("N").Substring(0, 8);

            // تشفيرها وحفظها
            application.TemporaryPassword = PasswordHelper.HashPassword(plainTextPassword);
            db.SaveChanges();

            // عرض الكلمة الجديدة للموظف ليعطيها للمتقدم
            TempData["GeneratedPassword"] = plainTextPassword;
            TempData["SuccessMessage"] = "تم إعادة تعيين كلمة المرور بنجاح. يرجى تزويد المتقدم بكلمة المرور الظاهرة أدناه.";

            return RedirectToAction("Details", new { id = id });
        }


        [HttpGet]
        public ActionResult ViewAttachment(int id, string type)
        {
            var app = db.ExamApplications.Find(id);
            if (app == null) return HttpNotFound();
            string path = type == "highschool" ? app.HighSchoolCertificatePath : type == "bachelor" ? app.BachelorCertificatePath : app.PersonalIdPath;
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(Server.MapPath(path))) return HttpNotFound();
            return File(Server.MapPath(path), MimeMapping.GetMimeMapping(path));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}