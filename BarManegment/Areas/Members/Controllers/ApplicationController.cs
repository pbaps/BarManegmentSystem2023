using BarManegment.Models;
using BarManegment.ViewModels;
using BarManegment.Services;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System;
using System.Web;
using System.Collections.Generic;
 
using System.Data.Entity.Validation;
using System.Diagnostics;
using BarManegment.Helpers;

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Edit()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Members" });
            }
            var userId = (int)Session["UserId"];
            var application = db.GraduateApplications
                .Include(a => a.ContactInfo)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Attachments.Select(att => att.AttachmentType))
                .FirstOrDefault(a => a.UserId == userId);

            if (application == null)
            {
                return HttpNotFound("لم يتم العثور على طلب الانتساب الخاص بك.");
            }

            var viewModel = new GraduateApplicationViewModel
            {
                Id = application.Id,
                ArabicName = application.ArabicName,
                EnglishName = application.EnglishName,
                NationalIdNumber = application.NationalIdNumber,
                NationalIdTypeId = application.NationalIdTypeId,
                BirthDate = application.BirthDate,
                BirthPlace = application.BirthPlace,
                Nationality = application.Nationality,
                GenderId = application.GenderId,
                ApplicationStatusId = application.ApplicationStatusId,
                PersonalPhotoPath = application.PersonalPhotoPath,
                SubmissionDate = application.SubmissionDate,
                ContactInfo = application.ContactInfo ?? new ContactInfo(),
                Genders = new SelectList(db.Genders, "Id", "Name", application.GenderId),
                Countries = new SelectList(GetCountries(), "Value", "Text", application.Nationality),
                Governorates = new SelectList(GetPalestinianGovernorates(), "Value", "Text", application.ContactInfo?.Governorate),
                Qualifications = application.Qualifications.ToList(),
                QualificationTypes = new SelectList(db.QualificationTypes, "Id", "Name"),
                Attachments = application.Attachments.ToList(),
                AttachmentTypes = new SelectList(db.AttachmentTypes, "Id", "Name")
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(GraduateApplicationViewModel viewModel)
        {
            if (Session["UserId"] == null) { return RedirectToAction("Login", "Account", new { area = "Members" }); }
            var userId = (int)Session["UserId"];

            if (!ModelState.IsValid)
            {
                RepopulateListsForViewModel(viewModel);
                TempData["ErrorMessage"] = "الرجاء مراجعة البيانات المدخلة، هناك حقول غير صالحة.";
                return View(viewModel);
            }

            try
            {
                var applicationInDb = db.GraduateApplications
                                        .FirstOrDefault(a => a.UserId == userId && a.Id == viewModel.Id);

                if (applicationInDb == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                applicationInDb.EnglishName = viewModel.EnglishName;
                applicationInDb.BirthDate = viewModel.BirthDate;
                applicationInDb.BirthPlace = viewModel.BirthPlace;
                applicationInDb.Nationality = viewModel.Nationality;
                applicationInDb.GenderId = viewModel.GenderId;
                applicationInDb.SupervisorId = viewModel.SupervisorId;

                // === بداية التعديل: آلية حفظ جديدة ومضمونة لبيانات الاتصال ===
                var contactInfo = db.ContactInfos.Find(applicationInDb.Id);
                if (contactInfo == null)
                {
                    var newContactInfo = viewModel.ContactInfo;
                    newContactInfo.Id = applicationInDb.Id;
                    db.ContactInfos.Add(newContactInfo);
                }
                else
                {
                    db.Entry(contactInfo).CurrentValues.SetValues(viewModel.ContactInfo);
                }
                // === نهاية التعديل ===

                if (viewModel.PersonalPhotoFile != null && viewModel.PersonalPhotoFile.ContentLength > 0)
                {
                    if (!string.IsNullOrEmpty(applicationInDb.PersonalPhotoPath))
                    {
                        var oldPath = Server.MapPath(applicationInDb.PersonalPhotoPath);
                        if (System.IO.File.Exists(oldPath)) { System.IO.File.Delete(oldPath); }
                    }
                    var fileName = Path.GetFileName(viewModel.PersonalPhotoFile.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                    var path = Path.Combine(Server.MapPath("~/Uploads/PersonalPhotos"), uniqueFileName);
                    viewModel.PersonalPhotoFile.SaveAs(path);
                    applicationInDb.PersonalPhotoPath = $"/Uploads/PersonalPhotos/{uniqueFileName}";
                }

                db.SaveChanges();

                TempData["SuccessMessage"] = "تم حفظ بيانات طلبك بنجاح.";
                return RedirectToAction("Edit", "Application", new { area = "Members" });
            }
            catch (DbEntityValidationException ex)
            {
                var errorMessages = ex.EntityValidationErrors.SelectMany(x => x.ValidationErrors).Select(x => x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                Debug.WriteLine(fullErrorMessage);
                TempData["ErrorMessage"] = "حدث خطأ أثناء التحقق من صحة البيانات. " + fullErrorMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                TempData["ErrorMessage"] = "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.";
            }

            RepopulateListsForViewModel(viewModel);
            return View(viewModel);
        }

        [HttpGet]
        public ActionResult SearchSupervisors(string term)
        {
            var practicingStatusId = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول")?.Id;
            if (practicingStatusId == null)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
            var fiveYearsAgo = DateTime.Now.AddYears(-5);

            var supervisors = db.GraduateApplications
                .Where(s => s.ApplicationStatusId == practicingStatusId)
                .Where(s => s.ArabicName.Contains(term))
                .Where(s => s.Trainees.Count < 2)
                //.Where(s => s.SubmissionDate <= fiveYearsAgo) // يمكن تفعيل هذا الشرط لاحقاً
                .Select(s => new
                {
                    id = s.Id,
                    label = s.ArabicName + " (الرقم: " + s.Id + ")",
                    value = s.ArabicName
                })
                .Take(10)
                .ToList();

            return Json(supervisors, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddQualification(GraduateApplicationViewModel viewModel)
        {
            if (Session["UserId"] == null) { return RedirectToAction("Login", "Account", new { area = "Members" }); }
            var userId = (int)Session["UserId"];
            var application = db.GraduateApplications.FirstOrDefault(a => a.UserId == userId && a.Id == viewModel.Id);

            if (application == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Invalid Application");
            }

            ModelState.Clear();
            TryValidateModel(viewModel.NewQualification);

            var qualificationType = db.QualificationTypes.Find(viewModel.NewQualification.QualificationTypeId);
            if (qualificationType != null && qualificationType.MinimumAcceptancePercentage.HasValue)
            {
                if (viewModel.NewQualification.GradePercentage.HasValue)
                {
                    if (viewModel.NewQualification.GradePercentage < qualificationType.MinimumAcceptancePercentage.Value)
                    {
                        TempData["ErrorMessage"] = $"نسبة القبول المطلوبة لـ '{qualificationType.Name}' هي {qualificationType.MinimumAcceptancePercentage.Value}% أو أعلى.";
                        return RedirectToAction("Edit");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = $"يجب إدخال الدرجة/المعدل لشهادة '{qualificationType.Name}'.";
                    return RedirectToAction("Edit");
                }
            }

            if (ModelState.IsValid)
            {
                viewModel.NewQualification.GraduateApplicationId = application.Id;
                db.Qualifications.Add(viewModel.NewQualification);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تمت إضافة المؤهل العلمي بنجاح.";
            }
            else
            {
                TempData["ErrorMessage"] = "فشلت إضافة المؤهل. الرجاء التأكد من ملء جميع الحقول.";
            }
            return RedirectToAction("Edit");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteQualification(int qualificationId)
        {
            if (Session["UserId"] == null) { return RedirectToAction("Login", "Account", new { area = "Members" }); }
            var userId = (int)Session["UserId"];
            var qualification = db.Qualifications.Include(q => q.GraduateApplication)
                                  .FirstOrDefault(q => q.Id == qualificationId && q.GraduateApplication.UserId == userId);

            if (qualification != null)
            {
                db.Qualifications.Remove(qualification);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حذف المؤهل العلمي بنجاح.";
            }

            return RedirectToAction("Edit");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAttachment(GraduateApplicationViewModel viewModel)
        {
            if (Session["UserId"] == null) { return RedirectToAction("Login", "Account", new { area = "Members" }); }
            var userId = (int)Session["UserId"];
            var application = db.GraduateApplications.FirstOrDefault(a => a.UserId == userId && a.Id == viewModel.Id);
            var file = viewModel.NewAttachmentFile;

            if (application == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Invalid Application");
            }

            if (file != null && file.ContentLength > 0 && viewModel.NewAttachmentTypeId.HasValue)
            {
                var originalFileName = Path.GetFileName(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                var directoryPath = Server.MapPath($"~/Uploads/Attachments/{application.Id}");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                var fullPath = Path.Combine(directoryPath, uniqueFileName);
                file.SaveAs(fullPath);

                var attachment = new Attachment
                {
                    GraduateApplicationId = application.Id,
                    AttachmentTypeId = viewModel.NewAttachmentTypeId.Value,
                    FilePath = $"/Uploads/Attachments/{application.Id}/{uniqueFileName}",
                    OriginalFileName = originalFileName,
                    UploadDate = DateTime.Now
                };
                db.Attachments.Add(attachment);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم رفع المرفق بنجاح.";
            }
            else
            {
                TempData["ErrorMessage"] = "فشل رفع المرفق. الرجاء اختيار ملف وتحديد نوعه.";
            }

            return RedirectToAction("Edit");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAttachment(int attachmentId)
        {
            if (Session["UserId"] == null) { return RedirectToAction("Login", "Account", new { area = "Members" }); }
            var userId = (int)Session["UserId"];
            var attachment = db.Attachments.Include(a => a.GraduateApplication)
                               .FirstOrDefault(a => a.Id == attachmentId && a.GraduateApplication.UserId == userId);

            if (attachment != null)
            {
                var physicalPath = Server.MapPath(attachment.FilePath);
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }
                db.Attachments.Remove(attachment);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حذف المرفق بنجاح.";
            }

            return RedirectToAction("Edit");
        }

        [HttpGet]
        public ActionResult GetAttachmentFile(int id)
        {
            if (Session["UserId"] == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Unauthorized);
            }
            var userId = (int)Session["UserId"];

            // التأكد من أن المرفق المطلوب يعود للمستخدم المسجل دخوله
            var attachment = db.Attachments.Include(a => a.GraduateApplication)
                               .FirstOrDefault(a => a.Id == id && a.GraduateApplication.UserId == userId);

            if (attachment == null || string.IsNullOrEmpty(attachment.FilePath))
            {
                return HttpNotFound();
            }

            var physicalPath = Server.MapPath(attachment.FilePath);
            if (!System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound();
            }

            string mimeType = MimeMapping.GetMimeMapping(physicalPath);
            return File(physicalPath, mimeType);
        }
        // === نهاية الإضافة ===

        #region Helper Methods
        private void RepopulateListsForViewModel(GraduateApplicationViewModel viewModel)
        {
            viewModel.Genders = new SelectList(db.Genders, "Id", "Name", viewModel.GenderId);
            viewModel.Countries = new SelectList(GetCountries(), "Value", "Text", viewModel.Nationality);
            viewModel.Governorates = new SelectList(GetPalestinianGovernorates(), "Value", "Text", viewModel.ContactInfo?.Governorate);
            if (viewModel.Id > 0)
            {
                viewModel.Qualifications = db.Qualifications.Where(q => q.GraduateApplicationId == viewModel.Id).ToList();
                viewModel.Attachments = db.Attachments.Where(a => a.GraduateApplicationId == viewModel.Id).ToList();
            }
            viewModel.QualificationTypes = new SelectList(db.QualificationTypes, "Id", "Name");
            viewModel.AttachmentTypes = new SelectList(db.AttachmentTypes, "Id", "Name");
        }


        private static List<SelectListItem> GetPalestinianGovernorates()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "--- اختر المحافظة ---", Value = "" },
                new SelectListItem { Text = "محافظة غزة", Value = "محافظة غزة" },
                new SelectListItem { Text = "محافظة خان يونس", Value = "محافظة خان يونس" },
                new SelectListItem { Text = "محافظة رفح", Value = "محافظة رفح" },
                new SelectListItem { Text = "محافظة شمال غزة", Value = "محافظة شمال غزة" },
                new SelectListItem { Text = "محافظة دير البلح", Value = "محافظة دير البلح" },
                new SelectListItem { Text = "محافظة جنين", Value = "محافظة جنين" },
                new SelectListItem { Text = "محافظة طوباس", Value = "محافظة طوباس" },
                new SelectListItem { Text = "محافظة طولكرم", Value = "محافظة طولكرم" },
                new SelectListItem { Text = "محافظة نابلس", Value = "محافظة نابلس" },
                new SelectListItem { Text = "محافظة قلقيلية", Value = "محافظة قلقيلية" },
                new SelectListItem { Text = "محافظة سلفيت", Value = "محافظة سلفيت" },
                new SelectListItem { Text = "محافظة رام الله والبيرة", Value = "محافظة رام الله والبيرة" },
                new SelectListItem { Text = "محافظة أريحا", Value = "محافظة أريحا" },
                new SelectListItem { Text = "محافظة القدس", Value = "محافظة القدس" },
                new SelectListItem { Text = "محافظة بيت لحم", Value = "محافظة بيت لحم" },
                new SelectListItem { Text = "محافظة الخليل", Value = "محافظة الخليل" }
            };
        }

        private static List<SelectListItem> GetCountries()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "دولة فلسطين", Value = "دولة فلسطين" },
                new SelectListItem { Text = "الأردن", Value = "الأردن" },
                new SelectListItem { Text = "مصر", Value = "مصر" },
                new SelectListItem { Text = "لبنان", Value = "لبنان" },
                new SelectListItem { Text = "سوريا", Value = "سوريا" },
                new SelectListItem { Text = "العراق", Value = "العراق" },
                new SelectListItem { Text = "السعودية", Value = "السعودية" },
                new SelectListItem { Text = "الإمارات العربية المتحدة", Value = "الإمارات العربية المتحدة" },
            };
        }
        #endregion
 
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
