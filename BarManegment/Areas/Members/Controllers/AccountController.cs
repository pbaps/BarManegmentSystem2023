using BarManegment.Areas.Members.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Data.Entity;
using BarManegment.Services;
using System.Web;
using System.Threading.Tasks;
using BarManegment.ViewModels;

namespace BarManegment.Areas.Members.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. تسجيل الدخول (Login)
        // ============================================================
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            // التحقق من الجلسة والكوكي
            if (User.Identity.IsAuthenticated && Session["UserId"] != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            // تنظيف الكوكي إذا كانت الجلسة منتهية
            if (User.Identity.IsAuthenticated)
            {
                FormsAuthentication.SignOut();
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(MemberLoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                string inputId = model.NationalIdNumber?.Trim();
                string inputPass = model.Password?.Trim();

                var user = db.Users.Include(u => u.UserType)
                                   .FirstOrDefault(u => u.IdentificationNumber == inputId || u.Username == inputId);

                if (user == null)
                {
                    ModelState.AddModelError("", "بيانات الدخول غير صحيحة.");
                }
                else if (!user.IsActive)
                {
                    ModelState.AddModelError("", "هذا الحساب غير فعال (معطل).");
                }
                else if (user.UserType.NameEnglish != "Graduate" && user.UserType.NameEnglish != "Advocate")
                {
                    ModelState.AddModelError("", "غير مسموح لهذا النوع من المستخدمين بالدخول من هنا.");
                }
                else
                {
                    bool isPasswordValid = false;
                    try { isPasswordValid = PasswordHelper.VerifyPassword(inputPass, user.HashedPassword); }
                    catch { isPasswordValid = false; }

                    if (!isPasswordValid)
                    {
                        ModelState.AddModelError("", "كلمة المرور غير صحيحة.");
                    }
                    else
                    {
                        // تسجيل الدخول
                        FormsAuthentication.SetAuthCookie(user.Username, model.RememberMe);
                        SetSessionVariables(user); // دالة مساعدة لضبط الجلسة

                        if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                            && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToAction("Index", "Dashboard");
                    }
                }
            }
            return View(model);
        }

        // ============================================================
        // 2. إنشاء حساب جديد (Register) - 💡 الجزء المعدل
        // ============================================================
        [AllowAnonymous]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. التحقق من وجود الحساب مسبقاً
                if (db.Users.Any(u => u.IdentificationNumber == model.NationalIdNumber))
                {
                    ModelState.AddModelError("NationalIdNumber", "يوجد حساب مسجل مسبقاً لهذا الرقم الوطني. يرجى تسجيل الدخول.");
                    return View(model);
                }

                // 2. التحقق من الأهلية (هل يوجد طلب امتحان قبول سابق لهذا الشخص؟)
                var examApp = db.ExamApplications.FirstOrDefault(e => e.NationalIdNumber == model.NationalIdNumber);

                if (examApp == null)
                {
                    ModelState.AddModelError("", "لم يتم العثور على سجل قبول لهذا الرقم الوطني. التسجيل متاح فقط لمن اجتازوا امتحان القبول.");
                    return View(model);
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 3. إنشاء مستخدم جديد (User)
                        var graduateRole = db.UserTypes.FirstOrDefault(ut => ut.NameEnglish == "Graduate");
                        var newUser = new UserModel
                        {
                            Username = model.NationalIdNumber,
                            IdentificationNumber = model.NationalIdNumber,
                            // التصحيح: استخدام FullName من ExamApplication بدلاً من ArabicName
                            FullNameArabic = examApp.FullName,
                            Email = examApp.Email,
                            HashedPassword = PasswordHelper.HashPassword(model.Password),
                            IsActive = true,
                            UserTypeId = graduateRole?.Id ?? 0
                            // التصحيح: إزالة CreatedAt لأنها غير موجودة في UserModel
                        };
                        db.Users.Add(newUser);
                        db.SaveChanges();

                        // جلب نوع الهوية الافتراضي (أول نوع متاح أو 1) لأن ExamApplication لا يحتويه
                        var defaultIdType = db.NationalIdTypes.FirstOrDefault()?.Id ?? 1;

                        // 4. إنشاء ملف الخريج (GraduateApplication)
                        var newStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "طلب جديد");
                        var profile = new GraduateApplication
                        {
                            UserId = newUser.Id,
                            NationalIdNumber = model.NationalIdNumber,
                            // التصحيح: استخدام القيمة الافتراضية لأن ExamApplication لا يحتوي على NationalIdTypeId
                            NationalIdTypeId = defaultIdType,
                            // التصحيح: استخدام FullName للاسم العربي
                            ArabicName = examApp.FullName,
                            // التصحيح: وضع قيمة فارغة أو نفس الاسم للاسم الإنجليزي لعدم توفره في المصدر
                            EnglishName = "",
                            BirthDate = examApp.BirthDate,
                            GenderId = examApp.GenderId,
                            // التصحيح: وضع قيمة افتراضية للجنسية لعدم توفرها في المصدر
                            Nationality = "فلسطيني",

                            ExamApplicationId = examApp.Id,
                            ApplicationStatusId = newStatus?.Id ?? 0,
                            SubmissionDate = DateTime.Now,

                            // إنشاء معلومات اتصال افتراضية
                            ContactInfo = new ContactInfo
                            {
                                MobileNumber = examApp.MobileNumber,
                                Email = examApp.Email
                            }
                        };
                        db.GraduateApplications.Add(profile);
                        db.SaveChanges();

                        transaction.Commit();

                        // 5. تسجيل الدخول التلقائي
                        FormsAuthentication.SetAuthCookie(newUser.Username, false);
                        SetSessionVariables(newUser);

                        TempData["SuccessMessage"] = "تم إنشاء الحساب بنجاح. يرجى استكمال بيانات ملفك.";
                        return RedirectToAction("Index", "Dashboard");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "حدث خطأ أثناء إنشاء الحساب: " + ex.Message);
                    }
                }
            }
            return View(model);
        }

        // ============================================================
        // 3. الخروج وإدارة الجلسة
        // ============================================================
        public ActionResult LogOff()
        {
            SignOutUser();
            return RedirectToAction("Login");
        }

        public ActionResult LogOffConfirmation()
        {
            return RedirectToAction("Login");
        }

        public ActionResult SessionExpired()
        {
            SignOutUser();
            TempData["ErrorMessage"] = "انتهت الجلسة بسبب عدم النشاط.";
            return RedirectToAction("Login");
        }

        // دوال مساعدة خاصة
        private void SignOutUser()
        {
            FormsAuthentication.SignOut();
            Session.Abandon();
            Session.Clear();
            if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                var c = new HttpCookie(FormsAuthentication.FormsCookieName) { Expires = DateTime.Now.AddDays(-1) };
                Response.Cookies.Add(c);
            }
        }

        private void SetSessionVariables(UserModel user)
        {
            Session["UserId"] = user.Id;
            Session["FullName"] = user.FullNameArabic;
            Session["UserType"] = user.UserType.NameEnglish;
            Session["ProfilePicturePath"] = user.ProfilePicturePath;
        }




        // --- بداية إضافة خاصية "نسيت كلمة المرور" ---

        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user == null)
                {
                    // لا تكشف للمستخدم ما إذا كان البريد الإلكتروني موجوداً أم لا لدواعي أمنية
                    return View("ForgotPasswordConfirmation");
                }

                // إنشاء رمز فريد وآمن
                var token = Guid.NewGuid().ToString();
                user.ResetPasswordToken = token;
                user.ResetPasswordTokenExpiration = DateTime.UtcNow.AddHours(1); // صلاحية الرمز ساعة واحدة
                db.SaveChanges();

                // إنشاء رابط إعادة التعيين
                // ملاحظة: تأكد من تمرير Area إذا كان الكونترولر داخل Area، وإلا احذفه
                var resetLink = Url.Action("ResetPassword", "Account", new { token = token, area = "Members" }, protocol: Request.Url.Scheme);

                // =========================================================================
                // تصميم الرسالة الاحترافي (HTML Email Template)
                // =========================================================================
                var subject = "استعادة كلمة المرور - نظام إدارة نقابة المحامين";

                var body = $@"
        <!DOCTYPE html>
        <html lang='ar' dir='rtl'>
        <head>
            <meta charset='UTF-8'>
            <style>
                body {{ font-family: 'Cairo', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f4f4; margin: 0; padding: 0; }}
                .email-container {{ max-width: 600px; margin: 20px auto; background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.1); border: 1px solid #e0e0e0; }}
                .header {{ background-color: #0f172a; padding: 30px; text-align: center; border-bottom: 5px solid #c5a059; }}
                .header h1 {{ color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; }}
                .content {{ padding: 40px 30px; text-align: right; direction: rtl; color: #333333; line-height: 1.8; }}
                .btn-reset {{ display: block; width: fit-content; margin: 30px auto; background-color: #c5a059; color: #ffffff !important; text-decoration: none; padding: 12px 35px; border-radius: 50px; font-weight: bold; font-size: 16px; box-shadow: 0 4px 6px rgba(197, 160, 89, 0.3); }}
                .btn-reset:hover {{ background-color: #b08d4b; }}
                .footer {{ background-color: #f9fafb; padding: 20px; text-align: center; font-size: 12px; color: #888888; border-top: 1px solid #eeeeee; }}
                .warning {{ font-size: 13px; color: #666; margin-top: 20px; border-top: 1px dashed #ddd; padding-top: 20px; }}
            </style>
        </head>
        <body>
            <div class='email-container'>
                <div class='header'>
                    <h1>نظام إدارة نقابة المحامين</h1>
                </div>

                <div class='content'>
                    <h2 style='color: #0f172a; margin-top: 0;'>مرحباً، {user.FullNameArabic}</h2>
                    <p>لقد تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك في البوابة الإلكترونية.</p>
                    <p>للمتابعة وإنشاء كلمة مرور جديدة، يرجى الضغط على الزر أدناه:</p>
                    
                    <center>
                        <a href='{resetLink}' class='btn-reset'>إعادة تعيين كلمة المرور</a>
                    </center>
                    
                    <div class='warning'>
                        <p><strong>ملاحظة:</strong> هذا الرابط صالح لمدة ساعة واحدة فقط.</p>
                        <p>إذا لم تكن أنت من قام بهذا الطلب، يمكنك تجاهل هذه الرسالة بأمان، ولن يتم إجراء أي تغييرات على حسابك.</p>
                        <p style='margin-bottom:0;'>إذا واجهت مشكلة في الضغط على الزر، يمكنك نسخ الرابط التالي ولصقه في المتصفح:</p>
                        <p style='direction: ltr; text-align: left; word-break: break-all; color: #c5a059; font-size: 12px;'>{resetLink}</p>
                    </div>
                </div>

                <div class='footer'>
                    <p>&copy; {DateTime.Now.Year} نقابة المحامين الفلسطينيين - جميع الحقوق محفوظة.</p>
                    <p>هذه رسالة آلية، يرجى عدم الرد عليها.</p>
                </div>
            </div>
        </body>
        </html>";

                await EmailService.SendEmailAsync(user.Email, subject, body);

                return View("ForgotPasswordConfirmation");
            }

            return View(model);
        }


        [AllowAnonymous]
        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return View("Error");
            }

            var user = db.Users.FirstOrDefault(u => u.ResetPasswordToken == token && u.ResetPasswordTokenExpiration > DateTime.UtcNow);
            if (user == null)
            {
                ViewBag.ErrorMessage = "رابط إعادة تعيين كلمة المرور غير صالح أو انتهت صلاحيته.";
                return View("ResetPasswordConfirmation");
            }

            var model = new ResetPasswordViewModel { Token = token, Email = user.Email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = db.Users.FirstOrDefault(u => u.ResetPasswordToken == model.Token && u.ResetPasswordTokenExpiration > DateTime.UtcNow);
            if (user == null)
            {
                ViewBag.ErrorMessage = "حدث خطأ ما. يرجى محاولة طلب إعادة تعيين كلمة المرور مرة أخرى.";
                return View("ResetPasswordConfirmation");
            }

            user.HashedPassword = PasswordHelper.HashPassword(model.Password);
            user.ResetPasswordToken = null; // إبطال الرمز بعد استخدامه
            user.ResetPasswordTokenExpiration = null;
            db.SaveChanges();

            AuditService.LogAction("ResetPassword", "Account", $"User '{user.Username}' reset their password.");

            return View("ResetPasswordConfirmation");
        }
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}