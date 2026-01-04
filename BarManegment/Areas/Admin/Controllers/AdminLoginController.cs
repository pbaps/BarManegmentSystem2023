using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using BarManegment.ViewModels;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Data.Entity; // 💡 ضروري لاستخدام Include

namespace BarManegment.Areas.Admin.Controllers
{
    public class AdminLoginController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/AdminLogin/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            // إذا كان المستخدم مسجلاً للدخول بالفعل، لا داعي لعرض صفحة الدخول
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.IsExternalPage = true; // لإخفاء القوائم الجانبية في الـ Layout
            return View();
        }

        // POST: Admin/AdminLogin/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            ViewBag.IsExternalPage = true;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // تنظيف اسم المستخدم من المسافات الزائدة (Trim) لتطابق طريقة الحفظ
            string cleanUsername = model.Username?.Trim();

            // 1. جلب المستخدم مع نوعه (UserType) للتحقق من الصلاحيات
            var user = db.Users.Include(u => u.UserType)
                               .FirstOrDefault(u => u.Username == cleanUsername);

            // 2. التحقق من صحة كلمة المرور
            // نستخدم VerifyPassword لأنها تقرأ التشفير الذي تم إنشاؤه بواسطة HashPassword في UsersController
            if (user != null && PasswordHelper.VerifyPassword(model.Password, user.HashedPassword))
            {
                // 3. التحقق من أن الحساب فعال (غير معطل)
                if (!user.IsActive)
                {
                    ModelState.AddModelError("", "تم تعطيل هذا الحساب. يرجى مراجعة مسؤول النظام.");
                    return View(model);
                }

                // 4. التحقق الأمني: منع الخريجين والمحامين من دخول لوحة الإدارة
                if (user.UserType.NameEnglish == "Graduate" || user.UserType.NameEnglish == "Advocate")
                {
                    ModelState.AddModelError("", "لا تملك صلاحية الدخول من هذه البوابة. يرجى استخدام بوابة الأعضاء.");
                    return View(model);
                }

                // 5. تسجيل الدخول وبناء الجلسة (Session)
                Session["UserId"] = user.Id;
                Session["FullName"] = user.FullNameArabic;
                Session["UserTypeId"] = user.UserTypeId;
                Session["ProfilePicturePath"] = user.ProfilePicturePath;

                // إنشاء كوكي المصادقة
                FormsAuthentication.SetAuthCookie(user.Username, model.RememberMe);

                // تسجيل عملية الدخول في سجلات النظام (Audit)
                AuditService.LogAction("Login", "AdminLogin", $"User '{user.Username}' ({user.UserType.NameEnglish}) logged in via Admin Portal.");

                // التوجيه للصفحة المطلوبة أو الصفحة الرئيسية
                if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/") && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
            }
            else
            {
                // في حال فشل التحقق من كلمة المرور أو عدم وجود المستخدم
                ModelState.AddModelError("", "اسم المستخدم أو كلمة المرور غير صحيحة.");
                return View(model);
            }
        }

        // GET: Admin/AdminLogin/LogOffConfirmation
        public ActionResult LogOffConfirmation()
        {
            return View();
        }

        // POST: Admin/AdminLogin/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            if (Session["FullName"] != null)
            {
                AuditService.LogAction("Logout", "AdminLogin", $"User '{Session["FullName"]}' logged out.");
            }

            Session.Clear(); // مسح بيانات الجلسة
            FormsAuthentication.SignOut(); // مسح الكوكي

            return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });
        }

        // GET: Admin/AdminLogin
        [AllowAnonymous]
        public ActionResult Index()
        {
            return RedirectToAction("Login");
        }

        // GET: Admin/AdminLogin/SessionExpired
        [AllowAnonymous]
        public ActionResult SessionExpired()
        {
            ViewBag.IsExternalPage = true;
            return View();
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