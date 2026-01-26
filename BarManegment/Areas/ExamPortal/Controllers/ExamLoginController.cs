using BarManegment.Models;
using BarManegment.Areas.ExamPortal.ViewModels;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using BarManegment.Helpers;
using System.Web.Security;
using System;

namespace BarManegment.Areas.ExamPortal.Controllers
{
    [AllowAnonymous]
    public class ExamLoginController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            if (Session["EnrollmentId"] != null) return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ExamLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var now = DateTime.Now;
            string inputId = model.NationalIdNumber.Trim(); // إزالة المسافات

            // =========================================================
            // 1. البحث في جدول الأعضاء (Users) - التصحيح هنا
            // =========================================================
            // ✅ البحث في Username OR IdentificationNumber
            var user = db.Users.Include(u => u.UserType)
                               .FirstOrDefault(u => u.Username == inputId || u.IdentificationNumber == inputId);

            if (user != null)
            {
                // 1.1 التحقق من كلمة المرور
                if (!PasswordHelper.VerifyPassword(model.Password, user.HashedPassword))
                {
                    ModelState.AddModelError("Password", "كلمة المرور غير صحيحة.");
                    return View(model);
                }

                // 1.2 التحقق من نوع المستخدم
                if (user.UserType.NameEnglish != "Graduate" && user.UserType.NameEnglish != "Practicing" && user.UserType.NameEnglish != "Advocate")
                {
                    ModelState.AddModelError("", "هذا الحساب ليس لديه صلاحية تقديم امتحانات (نوع المستخدم غير مطابق).");
                    return View(model);
                }

                // 1.3 التحقق من ملف العضوية
                var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == user.Id);
                if (graduateApp == null)
                {
                    ModelState.AddModelError("", "المستخدم موجود ولكن لا يوجد ملف عضوية مرتبط به.");
                    return View(model);
                }

                // 1.4 التحقق من الامتحان
                var enrollment = db.ExamEnrollments
                    .Include(e => e.Exam)
                    .FirstOrDefault(e =>
                        e.GraduateApplicationId == graduateApp.Id &&
                        e.Exam.IsActive);

                if (enrollment == null)
                {
                    ModelState.AddModelError("", "أنت غير مسجل في أي امتحان نشط حالياً.");
                    return View(model);
                }

                // 1.5 التحقق من الوقت
                if (now < enrollment.Exam.StartTime)
                {
                    ModelState.AddModelError("", $"الامتحان لم يبدأ بعد. موعد البدء: {enrollment.Exam.StartTime:yyyy-MM-dd HH:mm}");
                    return View(model);
                }
                if (now > enrollment.Exam.EndTime)
                {
                    ModelState.AddModelError("", "عذراً، انتهى وقت الامتحان.");
                    return View(model);
                }

                // --> نجاح
                SetExamSession(enrollment.Id, user.FullNameArabic, "Member", graduateApp.Id, user.Username);
                return RedirectToAction("Index", "Dashboard");
            }

            // =========================================================
            // 2. البحث في جدول الخريجين الجدد (ExamApplications)
            // =========================================================
            var examApp = db.ExamApplications.FirstOrDefault(a => a.NationalIdNumber == inputId);

            if (examApp != null)
            {
                if (string.IsNullOrEmpty(examApp.TemporaryPassword) || !PasswordHelper.VerifyPassword(model.Password, examApp.TemporaryPassword))
                {
                    ModelState.AddModelError("Password", "كلمة المرور غير صحيحة.");
                    return View(model);
                }

                var enrollment = db.ExamEnrollments.Include(e => e.Exam)
                    .FirstOrDefault(e => e.ExamApplicationId == examApp.Id && e.Exam.IsActive);

                if (enrollment == null)
                {
                    ModelState.AddModelError("", "لست مسجلاً في امتحان.");
                    return View(model);
                }

                if (now < enrollment.Exam.StartTime)
                {
                    ModelState.AddModelError("", "الامتحان لم يبدأ بعد.");
                    return View(model);
                }
                if (now > enrollment.Exam.EndTime)
                {
                    ModelState.AddModelError("", "انتهى وقت الامتحان.");
                    return View(model);
                }

                // --> نجاح
                SetExamSession(enrollment.Id, examApp.FullName, "ExternalApplicant", examApp.Id, examApp.NationalIdNumber);
                return RedirectToAction("Index", "Dashboard");
            }

            // =========================================================
            // 3. فشل تام
            // =========================================================
            ModelState.AddModelError("", "البيانات المدخلة غير صحيحة.");
            return View(model);
        }

        private void SetExamSession(int enrollmentId, string name, string type, int applicantId, string username)
        {
            Session["EnrollmentId"] = enrollmentId;
            Session["TraineeName"] = name;
            Session["ApplicantType"] = type;
            Session["ApplicantId"] = applicantId;
            FormsAuthentication.SetAuthCookie(username, false);
        }

        public ActionResult LogOffConfirmation() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            Session.Clear();
            Session.Abandon();
            FormsAuthentication.SignOut();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}