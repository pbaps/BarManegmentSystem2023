using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using BarManegment.ViewModels;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    // [Authorize] // ✅ تمت إزالتها لأن CustomAuthorize تقوم بالواجب
    public class UsersController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/Users
        // GET: Admin/Users
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult Index(string userGroup)
        {
            var query = db.Users.AsNoTracking().Include(u => u.UserType).AsQueryable();

            // 1. حساب العدادات للعرض في التبويبات
            // (نقوم بالعد في السيرفر ليكون الأداء سريعاً)
            ViewBag.CountAll = db.Users.Count();

            ViewBag.CountStaff = db.Users.Count(u => u.UserType.NameEnglish == "Administrator" || u.UserType.NameEnglish == "Employee");

            ViewBag.CountLawyers = db.Users.Count(u => u.UserType.NameEnglish == "Advocate");

            ViewBag.CountTrainees = db.Users.Count(u => u.UserType.NameEnglish == "Graduate");

            ViewBag.CountCommittees = db.Users.Count(u => u.UserType.NameEnglish == "CommitteeMember" || u.UserType.NameEnglish == "Grader");

            // 2. تطبيق الفلترة حسب المجموعة المختارة
            switch (userGroup)
            {
                case "Staff": // الإدارة والموظفين
                    query = query.Where(u => u.UserType.NameEnglish == "Administrator" || u.UserType.NameEnglish == "Employee");
                    break;
                case "Lawyers": // المحامين المزاولين
                    query = query.Where(u => u.UserType.NameEnglish == "Advocate");
                    break;
                case "Trainees": // المتدربين
                    query = query.Where(u => u.UserType.NameEnglish == "Graduate");
                    break;
                case "Committees": // اللجان والمصححين
                    query = query.Where(u => u.UserType.NameEnglish == "CommitteeMember" || u.UserType.NameEnglish == "Grader");
                    break;
                default: // الكل
                    break;
            }

            ViewBag.CurrentUserGroup = userGroup;

            var users = query.OrderByDescending(u => u.Id).ToList();
            return View(users);
        }

        // GET: Admin/Users/Details/5
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = db.Users.Include(u => u.UserType).FirstOrDefault(u => u.Id == id);
            if (user == null) return HttpNotFound();

            return View(user);
        }

        // GET: Admin/Users/Create
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            var viewModel = new UserViewModel
            {
                UserTypes = new SelectList(db.UserTypes, "Id", "NameArabic")
            };
            return View(viewModel);
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(UserViewModel viewModel)
        {
            // التحقق اليدوي لكلمة المرور عند الإنشاء (لأنها مطلوبة هنا)
            if (string.IsNullOrWhiteSpace(viewModel.Password))
            {
                ModelState.AddModelError("Password", "حقل كلمة المرور مطلوب.");
            }

            // التحقق من تكرار اسم المستخدم
            if (db.Users.Any(u => u.Username == viewModel.Username))
            {
                ModelState.AddModelError("Username", "اسم المستخدم هذا موجود مسبقاً. الرجاء اختيار اسم آخر.");
            }

            if (ModelState.IsValid)
            {
                var user = new UserModel
                {
                    FullNameArabic = viewModel.FullNameArabic,
                    Username = viewModel.Username,
                    IdentificationNumber = viewModel.IdentificationNumber,
                    Email = viewModel.Email,
                    UserTypeId = viewModel.UserTypeId,
                    IsActive = viewModel.IsActive,
                    HashedPassword = PasswordHelper.HashPassword(viewModel.Password)
                };

                // حفظ الصورة الشخصية
                if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.ContentLength > 0)
                {
                    var originalFileName = Path.GetFileName(viewModel.ProfilePictureFile.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                    var directoryPath = Server.MapPath("~/Uploads/ProfilePictures");

                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    var fullPath = Path.Combine(directoryPath, uniqueFileName);
                    viewModel.ProfilePictureFile.SaveAs(fullPath);
                    user.ProfilePicturePath = $"/Uploads/ProfilePictures/{uniqueFileName}";
                }

                db.Users.Add(user);
                db.SaveChanges();

                AuditService.LogAction("Create", "Users", $"User '{user.Username}' created by Admin.");
                TempData["SuccessMessage"] = "تمت إضافة المستخدم بنجاح!";
                return RedirectToAction("Index");
            }

            viewModel.UserTypes = new SelectList(db.UserTypes, "Id", "NameArabic", viewModel.UserTypeId);
            return View(viewModel);
        }

        // GET: Admin/Users/Edit/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            UserModel userModel = db.Users.Find(id);
            if (userModel == null) return HttpNotFound();

            var viewModel = new UserViewModel
            {
                Id = userModel.Id,
                FullNameArabic = userModel.FullNameArabic,
                Username = userModel.Username,
                IdentificationNumber = userModel.IdentificationNumber,
                Email = userModel.Email,
                IsActive = userModel.IsActive,
                UserTypeId = userModel.UserTypeId,
                ProfilePicturePath = userModel.ProfilePicturePath,
                UserTypes = new SelectList(db.UserTypes, "Id", "NameArabic", userModel.UserTypeId)
            };
            return View(viewModel);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(UserViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var userInDb = db.Users.Find(viewModel.Id);
                if (userInDb == null) return HttpNotFound();

                // التحقق من تكرار الاسم (مع استثناء المستخدم الحالي)
                if (db.Users.Any(u => u.Username == viewModel.Username && u.Id != viewModel.Id))
                {
                    ModelState.AddModelError("Username", "اسم المستخدم موجود مسبقاً.");
                    viewModel.UserTypes = new SelectList(db.UserTypes, "Id", "NameArabic", viewModel.UserTypeId);
                    return View(viewModel);
                }

                userInDb.FullNameArabic = viewModel.FullNameArabic;
                userInDb.Username = viewModel.Username;
                userInDb.IdentificationNumber = viewModel.IdentificationNumber;
                userInDb.Email = viewModel.Email;
                userInDb.UserTypeId = viewModel.UserTypeId;
                userInDb.IsActive = viewModel.IsActive;

                // تحديث كلمة المرور فقط إذا تم إدخال قيمة جديدة
                if (!string.IsNullOrWhiteSpace(viewModel.Password))
                {
                    userInDb.HashedPassword = PasswordHelper.HashPassword(viewModel.Password);
                }

                // تحديث الصورة
                if (viewModel.ProfilePictureFile != null && viewModel.ProfilePictureFile.ContentLength > 0)
                {
                    // حذف القديمة
                    if (!string.IsNullOrEmpty(userInDb.ProfilePicturePath))
                    {
                        var oldFilePath = Server.MapPath(userInDb.ProfilePicturePath);
                        if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                    }

                    var originalFileName = Path.GetFileName(viewModel.ProfilePictureFile.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                    var directoryPath = Server.MapPath("~/Uploads/ProfilePictures");

                    if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

                    var fullPath = Path.Combine(directoryPath, uniqueFileName);
                    viewModel.ProfilePictureFile.SaveAs(fullPath);
                    userInDb.ProfilePicturePath = $"/Uploads/ProfilePictures/{uniqueFileName}";
                }

                db.Entry(userInDb).State = EntityState.Modified;
                db.SaveChanges();

                AuditService.LogAction("Edit", "Users", $"User '{userInDb.Username}' updated by Admin.");
                TempData["SuccessMessage"] = "تم تعديل بيانات المستخدم بنجاح!";
                return RedirectToAction("Index");
            }

            viewModel.UserTypes = new SelectList(db.UserTypes, "Id", "NameArabic", viewModel.UserTypeId);
            return View(viewModel);
        }

        // GET: Admin/Users/Delete/5
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = db.Users.Include(u => u.UserType).FirstOrDefault(u => u.Id == id);
            if (user == null) return HttpNotFound();

            return View(user);
        }

        // POST: Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var user = db.Users.Find(id);
            if (user == null) return HttpNotFound();

            // 1. منع حذف الحساب الحالي
            var currentUserId = (int)Session["UserId"];
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "لا يمكنك حذف حسابك الشخصي أثناء تسجيل الدخول.";
                return RedirectToAction("Index");
            }

            // 2. التحقق من الارتباطات (AuditLogs)
            if (db.AuditLogs.Any(log => log.UserId == id))
            {
                TempData["ErrorMessage"] = $"لا يمكن حذف المستخدم '{user.Username}' لوجود سجلات نشاط (Audit Logs) مرتبطة به. يمكنك تعطيل الحساب بدلاً من حذفه.";
                return RedirectToAction("Index");
            }

            // 3. التحقق من الارتباطات (GraduateApplications)
            if (db.GraduateApplications.Any(g => g.UserId == id))
            {
                TempData["ErrorMessage"] = $"لا يمكن حذف المستخدم '{user.Username}' لأنه مرتبط بملف محامي/متدرب.";
                return RedirectToAction("Index");
            }

            // حذف الصورة
            if (!string.IsNullOrEmpty(user.ProfilePicturePath))
            {
                var imagePath = Server.MapPath(user.ProfilePicturePath);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            db.Users.Remove(user);
            db.SaveChanges();

            AuditService.LogAction("Delete", "Users", $"User '{user.Username}' deleted by Admin.");
            TempData["SuccessMessage"] = "تم حذف المستخدم بنجاح.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}