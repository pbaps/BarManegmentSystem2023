using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using BarManegment.ViewModels;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [Authorize] // يضمن أن المستخدم مسجل دخول
    public class ProfileController : Controller // إذا كان BaseController يحتوي على db، غيّر هذا إلى Controller
    {
        // تعريف المتغير مرة واحدة فقط
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/Profile/Index
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });
            }

            var currentUserId = (int)Session["UserId"];
            var user = db.Users.Find(currentUserId);

            if (user == null)
            {
                return HttpNotFound();
            }

            var viewModel = new ProfileViewModel
            {
                Id = user.Id,
                FullNameArabic = user.FullNameArabic,
                IdentificationNumber = user.IdentificationNumber,
                Email = user.Email,
                Username = user.Username,
                ProfilePicturePath = user.ProfilePicturePath
            };

            return View(viewModel);
        }

        // POST: Admin/Profile/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ProfileViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "فشلت عملية التحديث. يرجى مراجعة البيانات.";
                return View(viewModel);
            }

            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });
            }

            var currentUserId = (int)Session["UserId"];
            var userInDb = db.Users.Find(currentUserId);

            if (userInDb == null)
            {
                return HttpNotFound();
            }

            // تحديث البيانات
            userInDb.FullNameArabic = viewModel.FullNameArabic;
            userInDb.IdentificationNumber = viewModel.IdentificationNumber;
            userInDb.Email = viewModel.Email;

            // تحديث كلمة المرور
            if (!string.IsNullOrWhiteSpace(viewModel.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(viewModel.OldPassword) || !PasswordHelper.VerifyPassword(viewModel.OldPassword, userInDb.HashedPassword))
                {
                    ModelState.AddModelError("OldPassword", "كلمة المرور الحالية غير صحيحة.");
                    return View(viewModel);
                }
                userInDb.HashedPassword = PasswordHelper.HashPassword(viewModel.NewPassword);
            }

            // تحديث الصورة
            if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.ContentLength > 0)
            {
                if (!string.IsNullOrEmpty(userInDb.ProfilePicturePath))
                {
                    var oldPath = Server.MapPath(userInDb.ProfilePicturePath);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                var fileName = Path.GetFileName(viewModel.ProfilePictureFile.FileName);
                var uniqueName = $"{Guid.NewGuid()}_{fileName}";
                var path = Server.MapPath("~/Uploads/ProfilePictures");

                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                viewModel.ProfilePictureFile.SaveAs(Path.Combine(path, uniqueName));
                userInDb.ProfilePicturePath = $"/Uploads/ProfilePictures/{uniqueName}";
            }

            db.Entry(userInDb).State = EntityState.Modified;
            db.SaveChanges();

            // تحديث الجلسة
            Session["FullName"] = userInDb.FullNameArabic;
            Session["ProfilePicturePath"] = userInDb.ProfilePicturePath;

            AuditService.LogAction("Update Profile", "Profile", $"Admin '{userInDb.Username}' updated profile.");
            TempData["SuccessMessage"] = "تم تحديث الملف الشخصي بنجاح.";

            return RedirectToAction("Index");
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult GetAttachmentFile(int id)
        {
            var attachment = db.Attachments.Find(id);

            // 1. التحقق من وجود المرفق في قاعدة البيانات
            if (attachment == null || string.IsNullOrEmpty(attachment.FilePath))
            {
                return HttpNotFound();
            }

            var physicalPath = Server.MapPath(attachment.FilePath);

            // 2. التحقق من وجود الملف الفعلي على السيرفر
            if (!System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound("الملف الفعلي غير موجود على الخادم.");
            }

            // 3. إرجاع الملف
            string mimeType = MimeMapping.GetMimeMapping(physicalPath);
            return File(physicalPath, mimeType);
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