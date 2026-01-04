using BarManegment.Models;
using BarManegment.Areas.ExamPortal.ViewModels;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using BarManegment.Helpers;
using System.Web.Security;

namespace BarManegment.Areas.ExamPortal.Controllers
{
    [AllowAnonymous]
    public class ExamLoginController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            if (Session["TraineeName"] != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ExamLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. المحاولة الأولى: التحقق كـ "متدرب" أو "محامي مزاول" (من بوابة الأعضاء)
                // هؤلاء لديهم حسابات مستخدمين (Users)
                var user = db.Users.Include(u => u.UserType).FirstOrDefault(u => u.Username == model.NationalIdNumber);

                if (user != null && (user.UserType.NameEnglish == "Graduate" || user.UserType.NameEnglish == "Practicing" || user.UserType.NameEnglish == "Advocate"))
                {
                    if (user.IsActive && PasswordHelper.VerifyPassword(model.Password, user.HashedPassword))
                    {
                        var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == user.Id);
                        if (graduateApp != null)
                        {
                            // هل لديه امتحان نشط؟
                            var enrollment = db.ExamEnrollments.Include(e => e.Exam)
                                .FirstOrDefault(e => e.GraduateApplicationId == graduateApp.Id && e.Exam.IsActive);

                            if (enrollment != null)
                            {
                                Session["EnrollmentId"] = enrollment.Id;
                                Session["TraineeName"] = user.FullNameArabic;
                                Session["ApplicantType"] = "Graduate";
                                Session["ApplicantId"] = graduateApp.Id; // GraduateApplicationId

                                FormsAuthentication.SetAuthCookie(user.Username, false);
                                return RedirectToAction("Index", "Dashboard");
                            }
                            else
                            {
                                ModelState.AddModelError("", "لا يوجد امتحان نشط متاح لك حالياً.");
                                return View(model);
                            }
                        }
                    }
                }

                // 2. المحاولة الثانية: التحقق كـ "خريج جديد" (من طلبات الامتحان مباشرة)
                // هؤلاء ليس لديهم حسابات Users بعد، بل ExamApplication
                var examApp = db.ExamApplications.FirstOrDefault(a => a.NationalIdNumber == model.NationalIdNumber);
                if (examApp != null && !string.IsNullOrEmpty(examApp.TemporaryPassword) && PasswordHelper.VerifyPassword(model.Password, examApp.TemporaryPassword))
                {
                    var enrollment = db.ExamEnrollments.Include(e => e.Exam)
                        .FirstOrDefault(e => e.ExamApplicationId == examApp.Id && e.Exam.IsActive);

                    if (enrollment != null)
                    {
                        Session["EnrollmentId"] = enrollment.Id;
                        Session["TraineeName"] = examApp.FullName;
                        Session["ApplicantType"] = "ExamApplicant";
                        Session["ApplicantId"] = examApp.Id; // ExamApplicationId

                        FormsAuthentication.SetAuthCookie(examApp.NationalIdNumber, false);
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        ModelState.AddModelError("", "لا يوجد امتحان نشط متاح لك حالياً.");
                        return View(model);
                    }
                }

                ModelState.AddModelError("", "البيانات المدخلة غير صحيحة أو الامتحان غير مفعل حاليًا.");
            }
            return View(model);
        }

        public ActionResult LogOffConfirmation()
        {
            return View();
        }

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