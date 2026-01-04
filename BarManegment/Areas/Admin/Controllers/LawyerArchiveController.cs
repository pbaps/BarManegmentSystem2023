using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BarManegment.ViewModels; // للاستفادة من ViewModels المشتركة

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class LawyerArchiveController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // -----------------------------------------------------------------
        // === 1. إدارة المرفقات (سجل الأرشيف)
        // -----------------------------------------------------------------

        // GET: Admin/LawyerArchive/ManageAttachments/5
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult ManageAttachments(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var trainee = db.GraduateApplications
                                .Include(a => a.Attachments.Select(att => att.AttachmentType))
                                .FirstOrDefault(a => a.Id == id);

            if (trainee == null) return HttpNotFound("لم يتم العثور على ملف المحامي/المتدرب.");

            ViewBag.TraineeName = trainee.ArabicName;
            ViewBag.AttachmentTypes = new SelectList(db.AttachmentTypes.OrderBy(t => t.Name), "Id", "Name");
            ViewBag.ApplicationId = trainee.Id;

            return View(trainee.Attachments.ToList()); // نرسل قائمة المرفقات
        }

        // POST: Admin/LawyerArchive/AddAttachment (رفع ملف جديد)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AddAttachment(int ApplicationId, int NewAttachmentTypeId, HttpPostedFileBase UploadedFile)
        {
            if (UploadedFile == null || UploadedFile.ContentLength == 0 || NewAttachmentTypeId <= 0)
            {
                TempData["ErrorMessage"] = "الرجاء اختيار نوع المرفق والملف معاً.";
                return RedirectToAction("ManageAttachments", new { id = ApplicationId });
            }

            try
            {
                // استخدام دالة حفظ الملفات المشتركة
                string path = SaveFile(UploadedFile, ApplicationId, "Attachments");
                if (path == null)
                {
                    TempData["ErrorMessage"] = "فشل في حفظ الملف على الخادم.";
                    return RedirectToAction("ManageAttachments", new { id = ApplicationId });
                }

                var attachment = new Attachment
                {
                    GraduateApplicationId = ApplicationId,
                    AttachmentTypeId = NewAttachmentTypeId,
                    FilePath = path,
                    OriginalFileName = Path.GetFileName(UploadedFile.FileName),
                    UploadDate = DateTime.Now
                };
                db.Attachments.Add(attachment);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم رفع المرفق بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ: " + ex.Message;
            }
            return RedirectToAction("ManageAttachments", new { id = ApplicationId });
        }


        // GET: Admin/LawyerArchive/GetAttachmentFile/5
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult GetAttachmentFile(int id)
        {
            var attachment = db.Attachments.Find(id);
            if (attachment == null || string.IsNullOrEmpty(attachment.FilePath))
            {
                return HttpNotFound();
            }

            var physicalPath = Server.MapPath(attachment.FilePath);
            if (!System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound("الملف الفعلي غير موجود على الخادم.");
            }

            string mimeType = MimeMapping.GetMimeMapping(physicalPath);
            return File(physicalPath, mimeType);
        }

        // POST: Admin/LawyerArchive/DeleteAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteAttachment(int attachmentId, int applicationId)
        {
            var attachment = db.Attachments.Find(attachmentId);
            if (attachment != null && attachment.GraduateApplicationId == applicationId)
            {
                if (!string.IsNullOrEmpty(attachment.FilePath))
                {
                    var oldPhysicalPath = Server.MapPath(attachment.FilePath);
                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath); // حذف الملف الفعلي
                    }
                }
                db.Attachments.Remove(attachment);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حذف المرفق بنجاح.";
            }
            else
            {
                TempData["ErrorMessage"] = "لم يتم العثور على المرفق المطلوب أو لا تملك صلاحية حذفه.";
            }
            return RedirectToAction("ManageAttachments", new { id = applicationId });
        }

        // POST: Admin/LawyerArchive/EditAttachmentType
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult EditAttachmentType(int id, int NewAttachmentTypeId, int applicationId)
        {
            var attachment = db.Attachments.Find(id);
            if (attachment != null && attachment.GraduateApplicationId == applicationId)
            {
                attachment.AttachmentTypeId = NewAttachmentTypeId;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل نوع المرفق بنجاح.";
            }
            else
            {
                TempData["ErrorMessage"] = "لم يتم العثور على المرفق المطلوب.";
            }
            return RedirectToAction("ManageAttachments", new { id = applicationId });
        }


        // -----------------------------------------------------------------
        // === 2. الدوال المساعدة (يجب نقلها أو تعريفها في BaseController)
        // -----------------------------------------------------------------
        // (إذا كانت هذه الدوال موجودة في BaseController أو في TraineeProfileController، يمكنك حذفها من هنا)
        private string SaveFile(HttpPostedFileBase file, int id, string subfolder)
        {
            if (file == null || file.ContentLength == 0) return null;
            try
            {
                string directoryPath = Server.MapPath($"~/Uploads/{subfolder}/{id}");
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

                string extension = Path.GetExtension(file.FileName);
                string fileName = $"{Guid.NewGuid()}{extension}";
                string path = Path.Combine(directoryPath, fileName);
                file.SaveAs(path);

                return $"/Uploads/{subfolder}/{id}/{fileName}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("File Save Error: " + ex.Message);
                return null;
            }
        }


        // GET: Admin/LawyerArchive/SearchLawyer
        // هذه الدالة تبحث عن المحامي وتحول النص إلى ID
        [HttpGet]
        public ActionResult SearchLawyer(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                TempData["ErrorMessage"] = "الرجاء إدخال قيمة للبحث (اسم أو رقم).";
                return RedirectToAction("Index", "Home");
            }

            // محاولة البحث بالرقم الوطني أو رقم العضوية أو الاسم
            var lawyer = db.GraduateApplications
                .Where(g => g.NationalIdNumber == searchTerm ||
                            g.MembershipId == searchTerm ||
                            g.ArabicName.Contains(searchTerm))
                .FirstOrDefault();

            if (lawyer == null)
            {
                TempData["ErrorMessage"] = $"لم يتم العثور على محامي/متدرب مطابق لـ '{searchTerm}'.";
                // نستخدم هنا الرابط الذي يعود إلى الداشبورد
                return RedirectToAction("Index", "Home");
            }

            // إذا وجد المحامي، أعد توجيهه إلى دالة ManageAttachments باستخدام الـ ID
            return RedirectToAction("ManageAttachments", new { id = lawyer.Id });
        }


        // ... (باقي الدوال مثل ManageAttachments تبقى كما هي) ...
    }
}