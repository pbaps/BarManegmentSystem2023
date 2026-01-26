using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression; // تأكد من إضافة مرجع System.IO.Compression.FileSystem
using System.Linq;
using System.Web.Mvc;
using BarManegment.Helpers; // للتحقق من الصلاحيات

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanManageBackups")] // صلاحية خاصة جداً
    public class SystemMaintenanceController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // عرض صفحة النسخ الاحتياطي
        public ActionResult Index()
        {
            var backupPath = Server.MapPath("~/App_Data/Backups");

            // التأكد من وجود المجلد
            if (!Directory.Exists(backupPath)) Directory.CreateDirectory(backupPath);

            // جلب قائمة الملفات الموجودة
            var files = Directory.GetFiles(backupPath, "*.zip")
                                 .Select(f => new FileInfo(f))
                                 .OrderByDescending(f => f.CreationTime)
                                 .ToList();

            return View(files);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateFullBackup()
        {
            try
            {
                // 1. إعداد المسارات
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupFolder = Server.MapPath("~/App_Data/Backups");
                string tempFolder = Server.MapPath($"~/App_Data/Backups/Temp_{timestamp}");
                string dbBackupPath = Path.Combine(tempFolder, $"Database_{timestamp}.bak");
                string finalZipPath = Path.Combine(backupFolder, $"FullBackup_{timestamp}.zip");

                // إنشاء مجلد مؤقت
                if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);

                // 2. نسخ قاعدة البيانات (Database Backup)
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                var builder = new SqlConnectionStringBuilder(connectionString);
                string databaseName = builder.InitialCatalog;

                // تنفيذ أمر SQL لعمل Backup
                string backupQuery = $"BACKUP DATABASE [{databaseName}] TO DISK = '{dbBackupPath}' WITH FORMAT, MEDIANAME = 'Z_SQLServerBackups', NAME = 'Full Backup of {databaseName}';";

                // نستخدم ExecuteSqlCommand من الـ Context
                db.Database.ExecuteSqlCommand(System.Data.Entity.TransactionalBehavior.DoNotEnsureTransaction, backupQuery);

                // 3. نسخ ملفات المشروع (مثل الصور المرفوعة)
                // سنقوم بنسخ مجلد Uploads لأنه الأهم، نسخ المشروع كاملاً قد يسبب مشاكل أثناء التشغيل
                string uploadsSourcePath = Server.MapPath("~/Uploads");
                string uploadsDestPath = Path.Combine(tempFolder, "Uploads");

                if (Directory.Exists(uploadsSourcePath))
                {
                    // نسخ المجلدات والملفات (دالة مساعدة بالأسفل)
                    DirectoryCopy(uploadsSourcePath, uploadsDestPath, true);
                }

                // 4. ضغط الكل في ملف ZIP واحد
                ZipFile.CreateFromDirectory(tempFolder, finalZipPath);

                // 5. تنظيف الملفات المؤقتة
                if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);

                TempData["SuccessMessage"] = "تم إنشاء النسخة الاحتياطية (قاعدة بيانات + ملفات) بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "فشل إنشاء النسخة الاحتياطية: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // تحميل النسخة
        public ActionResult DownloadBackup(string fileName)
        {
            string fullPath = Path.Combine(Server.MapPath("~/App_Data/Backups"), fileName);
            if (!System.IO.File.Exists(fullPath)) return HttpNotFound();

            return File(fullPath, "application/zip", fileName);
        }

        // حذف النسخة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBackup(string fileName)
        {
            string fullPath = Path.Combine(Server.MapPath("~/App_Data/Backups"), fileName);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                TempData["SuccessMessage"] = "تم حذف النسخة الاحتياطية.";
            }
            return RedirectToAction("Index");
        }

        // دالة مساعدة لنسخ المجلدات
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists) return;

            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName)) Directory.CreateDirectory(destDirName);

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }


        // استرجاع النسخة الاحتياطية
        // ✅ 1. دالة الاسترجاع من مسار خارجي (جديدة)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RestoreFromPath(string customFilePath)
        {
            if (string.IsNullOrWhiteSpace(customFilePath) || !System.IO.File.Exists(customFilePath))
            {
                TempData["ErrorMessage"] = "الملف غير موجود في المسار المحدد، تأكد من كتابة المسار الكامل (مثال: D:\\Backups\\file.zip).";
                return RedirectToAction("Index");
            }

            return ExecuteRestore(customFilePath);
        }

        // ✅ 2. تعديل دالة الاسترجاع القديمة لتستخدم الدالة المساعدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RestoreBackup(string fileName)
        {
            string backupFolder = Server.MapPath("~/App_Data/Backups");
            string zipFilePath = Path.Combine(backupFolder, fileName);

            if (!System.IO.File.Exists(zipFilePath))
            {
                TempData["ErrorMessage"] = "الملف غير موجود.";
                return RedirectToAction("Index");
            }

            return ExecuteRestore(zipFilePath);
        }

        // ✅ 3. الدالة المساعدة (Private Helper) التي تقوم بالعملية الفعلية
        private ActionResult ExecuteRestore(string zipFilePath)
        {
            string tempExtractFolder = Server.MapPath($"~/App_Data/Backups/Restore_Temp_{Guid.NewGuid()}");

            try
            {
                // 1. فك الضغط
                if (!Directory.Exists(tempExtractFolder)) Directory.CreateDirectory(tempExtractFolder);
                ZipFile.ExtractToDirectory(zipFilePath, tempExtractFolder);

                var bakFile = Directory.GetFiles(tempExtractFolder, "*.bak").FirstOrDefault();
                if (bakFile == null) throw new Exception("لم يتم العثور على ملف قاعدة البيانات (.bak) داخل الأرشيف.");

                // 2. استرجاع قاعدة البيانات
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                var builder = new SqlConnectionStringBuilder(connectionString);
                string dbName = builder.InitialCatalog;
                builder.InitialCatalog = "master"; // الاتصال بالماستر

                using (var conn = new SqlConnection(builder.ToString()))
                {
                    conn.Open();
                    string restoreQuery = $@"
                        ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        RESTORE DATABASE [{dbName}] FROM DISK = '{bakFile}' WITH REPLACE;
                        ALTER DATABASE [{dbName}] SET MULTI_USER;";

                    using (var cmd = new SqlCommand(restoreQuery, conn))
                    {
                        cmd.CommandTimeout = 300;
                        cmd.ExecuteNonQuery();
                    }
                }

                // 3. استرجاع الملفات (Uploads)
                string extractedUploads = Path.Combine(tempExtractFolder, "Uploads");
                string currentUploads = Server.MapPath("~/Uploads");

                if (Directory.Exists(extractedUploads))
                {
                    if (Directory.Exists(currentUploads)) Directory.Delete(currentUploads, true);
                    Directory.Move(extractedUploads, currentUploads);
                }

                TempData["SuccessMessage"] = "تم استرجاع النظام بنجاح من المسار: " + zipFilePath;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "فشل الاسترجاع: " + ex.Message;
            }
            finally
            {
                if (Directory.Exists(tempExtractFolder)) Directory.Delete(tempExtractFolder, true);
            }

            return RedirectToAction("Index");
        }

    }
}