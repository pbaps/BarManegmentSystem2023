using BarManegment.Areas.Members.ViewModels;
using BarManegment.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.IO;
using System.Collections.Generic;
using System.Web;
using BarManegment.Helpers;
using System.Configuration;
using BarManegment.ViewModels;

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // (دالة مساعدة لجلب بيانات المتدرب الحالي)
        private GraduateApplication GetCurrentUserApplication(int userId)
        {
            return db.GraduateApplications
                     .Include(g => g.ApplicationStatus)
                     .Include(g => g.ContactInfo)
                     .Include(g => g.Supervisor)
                     .Include(g => g.User)
                     .Include(g => g.Attachments.Select(a => a.AttachmentType))
                     .Include(g => g.Qualifications.Select(q => q.QualificationType))
                     .Include(g => g.ExamApplication)
                     .FirstOrDefault(g => g.UserId == userId);
        }

        // (دالة مساعدة لجلب المشرفين المتاحين - للـ ViewModel)
        private SelectList GetAvailableSupervisors()
        {
            var practicingStatusId = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول")?.Id;
            var fiveYearsAgo = DateTime.Now.AddYears(-5);

            var supervisors = db.GraduateApplications
                .Where(s => s.ApplicationStatusId == practicingStatusId && s.SubmissionDate <= fiveYearsAgo)
                .Select(s => new { s.Id, s.ArabicName })
                .ToList();
            return new SelectList(supervisors, "Id", "ArabicName");
        }

        // (دالة مساعدة لحفظ المرفقات)
        private string SaveFile(HttpPostedFileBase file, int id, string subfolder)
        {
            if (file == null || file.ContentLength == 0) return null;
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var directoryPath = Server.MapPath($"~/Uploads/{subfolder}/{id}");
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            var path = Path.Combine(directoryPath, fileName);
            file.SaveAs(path);
            return $"/Uploads/{subfolder}/{id}/{fileName}";
        }


        // GET: Members/Profile/Edit
        public ActionResult Edit()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var userId = (int)Session["UserId"];
            var user = db.Users.Find(userId);
            if (user == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var graduateApp = GetCurrentUserApplication(userId);
            if (graduateApp == null) return HttpNotFound();

            // (منطق جلب البيانات من ExamApplication)
            if (graduateApp.ExamApplicationId.HasValue && graduateApp.ExamApplication != null)
            {
                bool needsSave = false;
                if (graduateApp.ContactInfo == null)
                {
                    graduateApp.ContactInfo = new ContactInfo
                    {
                        Id = graduateApp.Id,
                        Email = graduateApp.ExamApplication.Email,
                        MobileNumber = graduateApp.ExamApplication.MobileNumber,
                        WhatsAppNumber = graduateApp.ExamApplication.WhatsAppNumber
                    };
                }
                if (graduateApp.BirthDate == DateTime.MinValue)
                {
                    graduateApp.BirthDate = graduateApp.ExamApplication.BirthDate;
                    needsSave = true;
                }
                if (needsSave) db.SaveChanges();
            }

            // --- تعبئة ViewModel ---
            var viewModel = new MemberProfileViewModel
            {
                Id = graduateApp.Id,
                ArabicName = graduateApp.ArabicName,
                NationalIdNumber = graduateApp.NationalIdNumber,
                BirthDate = graduateApp.BirthDate,
                EnglishName = graduateApp.EnglishName,
                BirthPlace = graduateApp.BirthPlace,
                Nationality = graduateApp.Nationality,
                TelegramChatId = graduateApp.TelegramChatId,
                ContactInfo = graduateApp.ContactInfo ?? new ContactInfo { Id = graduateApp.Id },
                Qualifications = graduateApp.Qualifications.ToList(),
                Attachments = graduateApp.Attachments.ToList(),
                CurrentPersonalPhotoPath = graduateApp.PersonalPhotoPath,
                Email = user.Email,
                CurrentStatusName = graduateApp.ApplicationStatus.Name,
                SupervisorId = graduateApp.SupervisorId,
                SupervisorNationalId = graduateApp.Supervisor?.NationalIdNumber,
 
                SupervisorNameDisplay = graduateApp.Supervisor?.ArabicName,

                // === 
                // === بداية الإضافة: جلب اسم البوت
                // ===
                TelegramBotName = ConfigurationManager.AppSettings["TelegramBotName"]?.Replace("@", ""),
                // === نهاية الإضافة ===
                // --- ⬇️ ⬇️ بداية الإضافة: جلب بيانات البنك ⬇️ ⬇️ ---
                BankName = graduateApp.BankName,
                BankBranch = graduateApp.BankBranch,
                AccountNumber = graduateApp.AccountNumber,
                Iban = graduateApp.Iban ,
                // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---
            };

            // --- تعبئة القوائم المنسدلة ---
            viewModel.Nationalities = new SelectList(GetCountries(), "Value", "Text", viewModel.Nationality);
            viewModel.Governorates = new SelectList(GetPalestinianGovernorates(), "Value", "Text", viewModel.ContactInfo?.Governorate);
            viewModel.QualificationTypes = new SelectList(db.QualificationTypes.ToList(), "Id", "Name");
            viewModel.AttachmentTypes = new SelectList(db.AttachmentTypes.ToList(), "Id", "Name");

            return View(viewModel);
        }

        // POST: Members/Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(MemberProfileViewModel viewModel)
        {
            var userId = (int)Session["UserId"];
            var graduateApp = GetCurrentUserApplication(userId);
            if (graduateApp == null) return HttpNotFound();
            var userToUpdate = db.Users.Find(userId);
            if (userToUpdate == null) return HttpNotFound("User account not found");

            string currentUserStatus = graduateApp.ApplicationStatus.Name;

            if (currentUserStatus != "محامي مزاول" && currentUserStatus != "Advocate")
            {
                if (!viewModel.SupervisorId.HasValue || viewModel.SupervisorId == 0)
                {
                    if (graduateApp.SupervisorId.HasValue && graduateApp.NationalIdNumber == viewModel.SupervisorNationalId)
                    {
                        viewModel.SupervisorId = graduateApp.SupervisorId;
                    }
                    else
                    {
                        ModelState.AddModelError("SupervisorNationalId", "الرجاء إدخال الرقم الوطني للمشرف والضغط على (تحقق) لتأكيده.");
                    }
                }
            }
            else
            {
                viewModel.SupervisorId = graduateApp.Id;
            }
            ModelState.Remove("TelegramChatId"); // (لأننا لا نريد من المستخدم إدخاله يدوياً)
            // --- دالة مساعدة لإعادة تعبئة البيانات عند الخطأ ---
            Action RePopulateViewModelOnFailure = () =>
            {
                viewModel.Qualifications = graduateApp.Qualifications.ToList();
                viewModel.Attachments = graduateApp.Attachments.ToList();
                viewModel.Nationalities = new SelectList(GetCountries(), "Value", "Text", viewModel.Nationality);
                viewModel.Governorates = new SelectList(GetPalestinianGovernorates(), "Value", "Text", viewModel.ContactInfo?.Governorate);
                viewModel.QualificationTypes = new SelectList(db.QualificationTypes.ToList(), "Id", "Name");
                viewModel.AttachmentTypes = new SelectList(db.AttachmentTypes.ToList(), "Id", "Name");
                viewModel.CurrentPersonalPhotoPath = graduateApp.PersonalPhotoPath;
                viewModel.CurrentStatusName = graduateApp.ApplicationStatus.Name;
                viewModel.TelegramBotName = ConfigurationManager.AppSettings["TelegramBotName"]?.Replace("@", ""); // (مهم جداً)
            };

            if (!ModelState.IsValid)
            {
                RePopulateViewModelOnFailure();
                return View(viewModel);
            }

            // --- 1. تحديث الصورة الشخصية ---
            if (viewModel.PersonalPhotoFile != null && viewModel.PersonalPhotoFile.ContentLength > 0)
            {
                // (منطق حذف الصورة القديمة)
                if (!string.IsNullOrEmpty(graduateApp.PersonalPhotoPath))
                {
                    string oldFullPath = Server.MapPath(graduateApp.PersonalPhotoPath);
                    if (System.IO.File.Exists(oldFullPath))
                    {
                        System.IO.File.Delete(oldFullPath);
                    }
                }
                graduateApp.PersonalPhotoPath = SaveFile(viewModel.PersonalPhotoFile, graduateApp.Id, "ProfilePics");
            }

            // --- 2. تحديث البيانات الأساسية ---
            graduateApp.EnglishName = viewModel.EnglishName;
            graduateApp.BirthPlace = viewModel.BirthPlace;
            graduateApp.Nationality = viewModel.Nationality;
            graduateApp.TelegramChatId = viewModel.TelegramChatId;
            graduateApp.BirthDate = viewModel.BirthDate;
            graduateApp.SupervisorId = viewModel.SupervisorId;

            // --- ⬇️ ⬇️ بداية الإضافة: حفظ بيانات البنك ⬇️ ⬇️ ---
            graduateApp.BankName = viewModel.BankName;
            graduateApp.BankBranch = viewModel.BankBranch;
            graduateApp.AccountNumber = viewModel.AccountNumber;
            graduateApp.Iban = viewModel.Iban;
            // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---

            // --- 3. تحديث بيانات الاتصال ---
            if (db.ContactInfos.AsNoTracking().FirstOrDefault(c => c.Id == graduateApp.Id) == null)
            {
                viewModel.ContactInfo.Id = graduateApp.Id;
                db.ContactInfos.Add(viewModel.ContactInfo);
            }
            else
            {
                var contactInfoToUpdate = db.ContactInfos.Find(graduateApp.Id);
                contactInfoToUpdate.Governorate = viewModel.ContactInfo.Governorate;
                contactInfoToUpdate.City = viewModel.ContactInfo.City;
                contactInfoToUpdate.Street = viewModel.ContactInfo.Street;
                contactInfoToUpdate.BuildingNumber = viewModel.ContactInfo.BuildingNumber;
                contactInfoToUpdate.MobileNumber = viewModel.ContactInfo.MobileNumber;
                contactInfoToUpdate.WhatsAppNumber = viewModel.ContactInfo.WhatsAppNumber;
                contactInfoToUpdate.HomePhoneNumber = viewModel.ContactInfo.HomePhoneNumber;
                contactInfoToUpdate.Email = viewModel.ContactInfo.Email;
                contactInfoToUpdate.EmergencyContactPerson = viewModel.ContactInfo.EmergencyContactPerson;
                contactInfoToUpdate.EmergencyContactNumber = viewModel.ContactInfo.EmergencyContactNumber;
                db.Entry(contactInfoToUpdate).State = EntityState.Modified;
            }

            // --- 4. تحديث بيانات الدخول (Email و Password) ---
            if (userToUpdate.Email != viewModel.Email)
            {
                bool emailExists = db.Users.Any(u => u.Email == viewModel.Email && u.Id != userId);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "هذا البريد الإلكتروني مستخدم لحساب آخر.");
                    RePopulateViewModelOnFailure();
                    return View(viewModel);
                }
                userToUpdate.Email = viewModel.Email;
            }
            if (!string.IsNullOrEmpty(viewModel.OldPassword) && !string.IsNullOrEmpty(viewModel.NewPassword))
            {
                if (PasswordHelper.VerifyPassword(viewModel.OldPassword, userToUpdate.HashedPassword))
                {
                    userToUpdate.HashedPassword = PasswordHelper.HashPassword(viewModel.NewPassword);
                }
                else
                {
                    ModelState.AddModelError("OldPassword", "كلمة المرور القديمة غير صحيحة.");
                    RePopulateViewModelOnFailure();
                    return View(viewModel);
                }
            }

            // --- 5. تحديث الحالة (للمتدربين فقط) ---
            if (currentUserStatus == "طلب جديد" || currentUserStatus == "بانتظار استكمال النواقص")
            {
                bool hasSupervisorApproval = graduateApp.Attachments.Any(a => a.AttachmentType.Name == "موافقة مشرف");
                if (hasSupervisorApproval && graduateApp.SupervisorId.HasValue)
                {
                    var pendingStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "بانتظار الموافقة النهائية");
                    graduateApp.ApplicationStatusId = pendingStatus.Id;
                    TempData["SuccessMessage"] = "تم إرسال ملفك بنجاح للمراجعة النهائية من قبل اللجنة.";
                }
                else
                {
                    var incompleteStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "بانتظار استكمال النواقص");
                    graduateApp.ApplicationStatusId = incompleteStatus.Id;
                    TempData["InfoMessage"] = "تم حفظ بياناتك. يرجى (التحقق من المشرف) و (رفع مرفق موافقة المشرف) لإرسال الطلب للجنة.";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "تم حفظ بياناتك بنجاح.";
            }

            db.SaveChanges();
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        public JsonResult VerifySupervisor(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId))
            {
                return Json(new { success = false, message = "الرجاء إدخال الرقم الوطني للمشرف." });
            }
            var practicingStatusId = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول")?.Id;
            var fiveYearsAgo = DateTime.Now.AddYears(-5);

            var supervisor = db.GraduateApplications
                .Where(s => s.NationalIdNumber == nationalId &&
                            s.ApplicationStatusId == practicingStatusId &&
                            s.SubmissionDate <= fiveYearsAgo)
                .Select(s => new { s.Id, s.ArabicName })
                .FirstOrDefault();

            if (supervisor == null)
            {
                return Json(new { success = false, message = "لم يتم العثور على مشرف بهذا الرقم الوطني، أو أنه غير مؤهل للإشراف." });
            }
            return Json(new { success = true, id = supervisor.Id, name = supervisor.ArabicName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddQualification(QualificationUploadViewModel NewQualification) // (استخدام المودل الفرعي)
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            if (NewQualification.QualificationTypeId > 0 && !string.IsNullOrWhiteSpace(NewQualification.UniversityName) && NewQualification.GraduationYear > 1900)
            {
                db.Qualifications.Add(new Qualification
                {
                    GraduateApplicationId = graduateApp.Id,
                    QualificationTypeId = NewQualification.QualificationTypeId,
                    UniversityName = NewQualification.UniversityName,
                    Faculty = NewQualification.Faculty,
                    Specialization = NewQualification.Specialization,
                    GraduationYear = NewQualification.GraduationYear,
                    GradePercentage = NewQualification.GradePercentage
                });
                db.SaveChanges();
                TempData["SuccessMessage"] = "تمت إضافة المؤهل بنجاح.";
            }
            else
            {
                TempData["ErrorMessage"] = "بيانات المؤهل غير صالحة. يرجى ملء الحقول المطلوبة.";
            }
            return RedirectToAction("Edit");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadAttachment(int ApplicationId, AttachmentUploadViewModel NewAttachment) // (استخدام المودل الفرعي)
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            // (التحقق من أن ID المرفق يطابق ID المستخدم)
            if (graduateApp.Id != ApplicationId)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            if (NewAttachment.File != null && NewAttachment.File.ContentLength > 0 && NewAttachment.AttachmentTypeId > 0)
            {
                var path = SaveFile(NewAttachment.File, graduateApp.Id, "Attachments");
                db.Attachments.Add(new Attachment
                {
                    GraduateApplicationId = graduateApp.Id,
                    FilePath = path,
                    OriginalFileName = NewAttachment.File.FileName,
                    UploadDate = DateTime.Now,
                    AttachmentTypeId = NewAttachment.AttachmentTypeId
                });
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم رفع المرفق بنجاح.";
            }
            else
            {
                TempData["ErrorMessage"] = "الرجاء اختيار نوع المرفق والملف معاً.";
            }
            return RedirectToAction("Edit");
        }

        #region Helper Methods (الدوال المساعدة)
        private static List<SelectListItem> GetPalestinianGovernorates()
        {
            return new List<SelectListItem>
            {
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

        public ActionResult PrintSupervisorForm()
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications
                .Include(a => a.ContactInfo)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .FirstOrDefault(a => a.UserId == userId);
            var bachelorQual = graduateApp.Qualifications.FirstOrDefault(q => q.QualificationType.Name == "بكالوريوس");
            var viewModel = new SupervisorFormViewModel
            {
                TraineeName = graduateApp.ArabicName,
                TraineeNationalId = graduateApp.NationalIdNumber,
                TraineeMobile = graduateApp.ContactInfo?.MobileNumber,
                TraineeUniversity = bachelorQual?.UniversityName,
                TraineeGradYear = bachelorQual?.GraduationYear,
                TraineePhotoPath = graduateApp.PersonalPhotoPath
            };
            return View(viewModel);
        }

        // (الدوال التي أرسلتها سابقاً للحذف والتعديل)
        // (يجب التأكد من أنها تستخدم التحقق الأمني الصحيح)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteQualification(int qualificationId)
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.AsNoTracking().FirstOrDefault(g => g.UserId == userId);
            var qual = db.Qualifications.Find(qualificationId);
            if (qual != null && qual.GraduateApplicationId == graduateApp.Id) // (تحقق من الملكية)
            {
                db.Qualifications.Remove(qual);
                db.SaveChanges();
            }
            return RedirectToAction("Edit");
        }

        [HttpGet]
        public ActionResult GetAttachmentFile(int id)
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.AsNoTracking().FirstOrDefault(g => g.UserId == userId);
            var attachment = db.Attachments.Find(id);
            if (attachment == null || attachment.GraduateApplicationId != graduateApp.Id) // (تحقق من الملكية)
            {
                return HttpNotFound();
            }
            var physicalPath = Server.MapPath(attachment.FilePath);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound("File not found on server.");
            string mimeType = MimeMapping.GetMimeMapping(physicalPath);
            return File(physicalPath, mimeType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAttachment(int attachmentId)
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.AsNoTracking().FirstOrDefault(g => g.UserId == userId);
            var attachment = db.Attachments.Find(attachmentId);
            if (attachment != null && attachment.GraduateApplicationId == graduateApp.Id) // (تحقق من الملكية)
            {
                // (حذف الملف الفعلي من الخادم)
                if (!string.IsNullOrEmpty(attachment.FilePath))
                {
                    var oldPhysicalPath = Server.MapPath(attachment.FilePath);
                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        System.IO.File.Delete(oldPhysicalPath);
                    }
                }
                db.Attachments.Remove(attachment);
                db.SaveChanges();
            }
            return RedirectToAction("Edit");
        }

        [HttpGet]
        public ActionResult EditQualification(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.AsNoTracking().FirstOrDefault(g => g.UserId == userId);
            var qualification = db.Qualifications.Find(id);
            if (qualification == null || qualification.GraduateApplicationId != graduateApp.Id)
            {
                return HttpNotFound();
            }
            ViewBag.QualificationTypeId = new SelectList(db.QualificationTypes.ToList(), "Id", "Name", qualification.QualificationTypeId);
            return View(qualification);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditQualification([Bind(Include = "Id,GraduateApplicationId,QualificationTypeId,UniversityName,Faculty,Specialization,GraduationYear,GradePercentage")] Qualification qualification)
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.AsNoTracking().FirstOrDefault(g => g.UserId == userId);
            if (qualification.GraduateApplicationId != graduateApp.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            if (ModelState.IsValid)
            {
                db.Entry(qualification).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل المؤهل بنجاح.";
                return RedirectToAction("Edit");
            }
            ViewBag.QualificationTypeId = new SelectList(db.QualificationTypes.ToList(), "Id", "Name", qualification.QualificationTypeId);
            return View(qualification);
        }

        [HttpGet]
        public ActionResult EditAttachment(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.AsNoTracking().FirstOrDefault(g => g.UserId == userId);
            var attachment = db.Attachments.Find(id);
            if (attachment == null || attachment.GraduateApplicationId != graduateApp.Id)
            {
                return HttpNotFound();
            }
            var viewModel = new AttachmentEditViewModel
            {
                Id = attachment.Id,
                GraduateApplicationId = attachment.GraduateApplicationId,
                AttachmentTypeId = attachment.AttachmentTypeId,
                OriginalFileName = attachment.OriginalFileName,
                FilePath = attachment.FilePath,
                AttachmentTypes = new SelectList(db.AttachmentTypes.ToList(), "Id", "Name", attachment.AttachmentTypeId)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAttachment(AttachmentEditViewModel viewModel)
        {
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.AsNoTracking().FirstOrDefault(g => g.UserId == userId);
            if (viewModel.GraduateApplicationId != graduateApp.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            var attachmentToUpdate = db.Attachments.Find(viewModel.Id);
            if (attachmentToUpdate == null)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                attachmentToUpdate.AttachmentTypeId = viewModel.AttachmentTypeId;

                if (viewModel.NewFile != null && viewModel.NewFile.ContentLength > 0)
                {
                    if (!string.IsNullOrEmpty(attachmentToUpdate.FilePath))
                    {
                        var oldPhysicalPath = Server.MapPath(attachmentToUpdate.FilePath);
                        if (System.IO.File.Exists(oldPhysicalPath))
                        {
                            System.IO.File.Delete(oldPhysicalPath);
                        }
                    }
                    string newPath = SaveFile(viewModel.NewFile, graduateApp.Id, "Attachments");
                    attachmentToUpdate.FilePath = newPath;
                    attachmentToUpdate.OriginalFileName = viewModel.NewFile.FileName;
                    attachmentToUpdate.UploadDate = DateTime.Now;
                }

                db.Entry(attachmentToUpdate).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل المرفق بنجاح.";
                return RedirectToAction("Edit");
            }
            viewModel.AttachmentTypes = new SelectList(db.AttachmentTypes.ToList(), "Id", "Name", viewModel.AttachmentTypeId);
            return View(viewModel);
        }

        // ---------------------------------------------------------
        // 2. السجل العائلي (Family Record) - الدوال الجديدة
        // ---------------------------------------------------------
        // ---------------------------------------------------------
        // 2. السجل العائلي (تحديث ليعمل مع الموديلات المعتمدة)
        // ---------------------------------------------------------
        [HttpGet]
        public ActionResult EditFamilyRecord()
        {
 
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var userId = (int)Session["UserId"];
            var lawyer = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);
            if (lawyer == null) return RedirectToAction("Index", "Dashboard");

            // جلب البيانات من الجداول المعتمدة
            var personalData = db.LawyerPersonalDatas
                                 .Include(p => p.Spouses)
                                 .Include(p => p.Children)
                                 .FirstOrDefault(p => p.LawyerId == lawyer.Id);

            var healthRecord = db.SecurityHealthRecords
                                 .FirstOrDefault(h => h.LawyerId == lawyer.Id);

            var viewModel = new LawyerFamilyViewModel
            {
                LawyerId = lawyer.Id,
                LawyerName = lawyer.ArabicName,
                PersonalData = personalData ?? new LawyerPersonalData { LawyerId = lawyer.Id },
                HealthRecord = healthRecord ?? new SecurityHealthRecord { LawyerId = lawyer.Id },

                // تعبئة القوائم
                SpousesList = personalData?.Spouses.ToList() ?? new List<LawyerSpouse>(),
                ChildrenList = personalData?.Children.ToList() ?? new List<LawyerChild>(),

                // الأدوية (كنص)
                MedicationsText = healthRecord?.MedicationsList
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditFamilyRecord(LawyerFamilyViewModel model)
        {
            if (model.SpousesList == null) model.SpousesList = new List<LawyerSpouse>();
            if (model.ChildrenList == null) model.ChildrenList = new List<LawyerChild>();

            if (ModelState.IsValid)
            {
                // 1. تحديث البيانات الشخصية
                var existingPersonal = db.LawyerPersonalDatas.FirstOrDefault(r => r.LawyerId == model.LawyerId);
                if (existingPersonal == null)
                {
                    existingPersonal = model.PersonalData;
                    existingPersonal.LawyerId = model.LawyerId;
                    db.LawyerPersonalDatas.Add(existingPersonal);
                }
                else
                {
                    // تحديث الحقول
                    existingPersonal.MaritalStatus = model.PersonalData.MaritalStatus;
                    existingPersonal.DisplacementGovernorate = model.PersonalData.DisplacementGovernorate;
                }

                // 2. تحديث السجل الصحي والأمني
                var existingHealth = db.SecurityHealthRecords.FirstOrDefault(h => h.LawyerId == model.LawyerId);
                if (existingHealth == null)
                {
                    existingHealth = model.HealthRecord;
                    existingHealth.LawyerId = model.LawyerId;
                    db.SecurityHealthRecords.Add(existingHealth);
                }
                else
                {
                    db.Entry(existingHealth).CurrentValues.SetValues(model.HealthRecord);
                    existingHealth.LawyerId = model.LawyerId; // تأكيد المفتاح
                }

                // تحديث نص الأدوية
                existingHealth.MedicationsList = model.MedicationsText;

                // 3. حفظ الملفات
                if (model.MedicalReportFile != null)
                {
                    // (تأكد من وجود خاصية MedicalReportPath في InjuryRecord أو SecurityHealthRecord)
                    // للتبسيط سنفترض إضافتها في SecurityHealthRecord أو استخدام InjuryRecord
                    // existingHealth.MedicalReportPath = SaveFile(model.MedicalReportFile, model.LawyerId, "Medical");
                }
                if (model.DetentionProofFile != null)
                {
                    existingHealth.DetentionAffidavitPath = SaveFile(model.DetentionProofFile, model.LawyerId, "Detention");
                }

                // 4. تحديث القوائم (الزوجات والأبناء)
                // تنظيف القديم
                var oldSpouses = db.LawyerSpouses.Where(x => x.LawyerId == model.LawyerId).ToList();
                db.LawyerSpouses.RemoveRange(oldSpouses);

                var oldChildren = db.LawyerChildren.Where(x => x.LawyerId == model.LawyerId).ToList();
                db.LawyerChildren.RemoveRange(oldChildren);

                // إضافة الجديد
                if (model.PersonalData.MaritalStatus != "أعزب")
                {
                    foreach (var w in model.SpousesList)
                    {
                        if (!string.IsNullOrWhiteSpace(w.FullName))
                        {
                            w.LawyerId = model.LawyerId;
                            db.LawyerSpouses.Add(w);
                        }
                    }
                }

                // إضافة الأبناء (نفترض وجود Checkbox في الواجهة للتحقق)
                foreach (var c in model.ChildrenList)
                {
                    if (!string.IsNullOrWhiteSpace(c.FullName))
                    {
                        c.LawyerId = model.LawyerId;
                        db.LawyerChildren.Add(c);
                    }
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حفظ السجل العائلي والصحي بنجاح.";
                return RedirectToAction("EditFamilyRecord");
            }

            return View(model);
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