using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using BarManegment.ViewModels;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class GraduateApplicationsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ==================================================================
        // 1. العرض والبحث (Index)
        // ==================================================================
        public ActionResult Index(string searchTerm)
        {
            // استبعاد الحالات التي خرجت من نطاق مراجعة القبول
            var excludedStatuses = new List<string> {
                "محامي مزاول", "محامي غير مزاول", "محامي متقاعد",
                "محامي متوفي", "محامي مشطوب", "محامي موظف", "محامي موقوف",
                "متدرب مقيد", "متدرب موقوف",
                "مقبول (بانتظار الدفع)"
            };

            var query = db.GraduateApplications.AsNoTracking()
                .Include(a => a.ApplicationStatus)
                .Where(a => a.ApplicationStatus != null && !excludedStatuses.Contains(a.ApplicationStatus.Name));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a =>
                    a.ArabicName.Contains(searchTerm) ||
                    a.NationalIdNumber.Contains(searchTerm)
                );
            }

            var applications = query.OrderByDescending(a => a.SubmissionDate).ToList();
            ViewBag.SearchTerm = searchTerm;

            return View(applications);
        }

        // ==================================================================
        // 2. التفاصيل واتخاذ القرار (Details & Decision)
        // ==================================================================
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications.AsNoTracking()
                .Include(a => a.Gender)
                .Include(a => a.NationalIdType)
                .Include(a => a.ApplicationStatus)
                .Include(a => a.ContactInfo)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Attachments.Select(att => att.AttachmentType))
                .Include(a => a.Supervisor)
                .FirstOrDefault(a => a.Id == id);

            if (application == null) return HttpNotFound();

            // تعبئة القوائم للمودال (الإضافة والتعديل داخل صفحة التفاصيل)
            ViewBag.QualificationTypes = new SelectList(db.QualificationTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");
            ViewBag.AttachmentTypes = new SelectList(db.AttachmentTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult SubmitDecision(int id, string decision, string notes)
        {
            var application = db.GraduateApplications.Find(id);
            if (application == null) return HttpNotFound();

            string oldStatus = application.ApplicationStatus?.Name ?? "N/A";
            string newStatusName = "";

            if (decision == "Approve")
            {
                // التحقق من وجود مشرف قبل الموافقة
                if (application.SupervisorId == null)
                {
                    TempData["ErrorMessage"] = "لا يمكن الموافقة المبدئية قبل تعيين محامي مشرف.";
                    return RedirectToAction("Details", new { id = id });
                }

                var nextStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "بانتظار الموافقة النهائية");
                if (nextStatus != null)
                {
                    application.ApplicationStatusId = nextStatus.Id;
                    newStatusName = nextStatus.Name;
                    TempData["SuccessMessage"] = "تمت الموافقة المبدئية. تم تحويل الملف إلى لجنة التدريب.";
                }
            }
            else if (decision == "Return")
            {
                if (string.IsNullOrWhiteSpace(notes))
                {
                    TempData["ErrorMessage"] = "يجب كتابة ملاحظات النواقص عند الإعادة.";
                    return RedirectToAction("Details", new { id = id });
                }

                var returnStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "بانتظار استكمال النواقص");
                if (returnStatus != null)
                {
                    application.ApplicationStatusId = returnStatus.Id;
                    newStatusName = returnStatus.Name;
                    TempData["SuccessMessage"] = "تم إعادة الطلب للمتدرب لاستكمال النواقص.";
                }
            }
            else if (decision == "Reject")
            {
                if (string.IsNullOrWhiteSpace(notes))
                {
                    TempData["ErrorMessage"] = "يجب كتابة سبب الرفض.";
                    return RedirectToAction("Details", new { id = id });
                }

                var rejectStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "مرفوض");
                if (rejectStatus != null)
                {
                    application.ApplicationStatusId = rejectStatus.Id;
                    newStatusName = rejectStatus.Name;
                    TempData["SuccessMessage"] = "تم رفض الطلب نهائياً.";
                }
            }

            db.SaveChanges();
            AuditService.LogAction("Employee Decision", "GraduateApplications", $"App ID {id}: {oldStatus} -> {newStatusName}. Notes: {notes}");
            return RedirectToAction("Index");
        }

        // ==================================================================
        // 3. إدارة المشرفين (بحث وتعيين)
        // ==================================================================

        [HttpGet]
        public JsonResult SearchSupervisors(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new { success = false, message = "الرجاء إدخال اسم، رقم هوية، أو رقم عضوية للبحث." }, JsonRequestBehavior.AllowGet);
            }

            // 1. بحث شامل في قاعدة البيانات
            var candidates = db.GraduateApplications.AsNoTracking()
                .Include(g => g.ApplicationStatus)
                .Where(g => g.ArabicName.Contains(searchTerm) ||
                            g.NationalIdNumber.Contains(searchTerm) ||
                            g.MembershipId.Contains(searchTerm) || // البحث برقم العضوية
                            g.Id.ToString() == searchTerm)
                .Take(20)
                .ToList();

            if (!candidates.Any())
            {
                return Json(new { success = false, message = "لا توجد نتائج مطابقة في قاعدة البيانات." }, JsonRequestBehavior.AllowGet);
            }

            var resultList = new List<object>();

            // 2. استخدام الخدمة لفحص الأهلية لكل نتيجة
            using (var svc = new SupervisorService())
            {
                foreach (var lawyer in candidates)
                {
                    var check = svc.CheckEligibility(lawyer.Id);

                    string displayDate = lawyer.PracticeStartDate.HasValue
                        ? lawyer.PracticeStartDate.Value.ToString("yyyy-MM-dd")
                        : lawyer.SubmissionDate.ToString("yyyy-MM-dd") + " (تاريخ انتساب)";

                    resultList.Add(new
                    {
                        id = lawyer.Id,
                        name = lawyer.ArabicName,
                        practiceDate = displayDate,
                        isEligible = check.IsEligible,
                        ineligibilityReason = check.Message
                    });
                }
            }

            return Json(new { success = true, supervisors = resultList }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignSupervisor(int applicationId, int supervisorId)
        {
            var graduateApp = db.GraduateApplications.Find(applicationId);
            if (graduateApp == null) return HttpNotFound();

            // التحقق من الأهلية قبل الحفظ
            using (var svc = new SupervisorService())
            {
                var check = svc.CheckEligibility(supervisorId);
                if (!check.IsEligible)
                {
                    TempData["ErrorMessage"] = $"فشل تعيين المشرف: {check.Message}";
                    return RedirectToAction("Details", new { id = applicationId });
                }
            }

            graduateApp.SupervisorId = supervisorId;
            db.SaveChanges();

            AuditService.LogAction("Assign Supervisor", "GraduateApplications", $"Supervisor ID {supervisorId} assigned to App ID {applicationId}.");
            TempData["SuccessMessage"] = "تم تعيين المشرف بنجاح.";

            return RedirectToAction("Details", new { id = applicationId });
        }

        // ==================================================================
        // 4. الإنشاء والتعديل (Create & Edit)
        // ==================================================================

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(string nationalId = null)
        {
            var viewModel = new GraduateApplicationViewModel
            {
                NewQualification = new Qualification(), // تهيئة لتجنب null
                Genders = new SelectList(db.Genders, "Id", "Name"),
                NationalIdTypes = new SelectList(db.NationalIdTypes, "Id", "Name"),
                ApplicationStatuses = new SelectList(db.ApplicationStatuses, "Id", "Name"),
                Countries = new SelectList(GetCountries(), "Value", "Text"),
                Governorates = new SelectList(GetPalestinianGovernorates(), "Value", "Text"),
                // تعبئة القوائم المطلوبة للمودال
                QualificationTypes = new SelectList(db.QualificationTypes.OrderBy(t => t.Name).ToList(), "Id", "Name"),
                AttachmentTypes = new SelectList(db.AttachmentTypes.OrderBy(t => t.Name).ToList(), "Id", "Name")
            };

            // ميزة الجلب التلقائي عند البحث بالرقم الوطني
            if (!string.IsNullOrWhiteSpace(nationalId))
            {
                var examApp = db.ExamApplications
                    .OrderByDescending(e => e.ApplicationDate)
                    .FirstOrDefault(e => e.NationalIdNumber == nationalId);

                if (examApp != null)
                {
                    viewModel.ArabicName = examApp.FullName;
                    viewModel.NationalIdNumber = examApp.NationalIdNumber;
                    viewModel.BirthDate = examApp.BirthDate;
                    TempData["InfoMessage"] = "تم العثور على بيانات من سجل الامتحان. يرجى استكمال الحقول وحفظ الطلب لنسخ المرفقات والمؤهلات تلقائياً.";
                }
                else
                {
                    TempData["ErrorMessage"] = "لم يتم العثور على سجل امتحان لهذا الرقم الوطني.";
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(GraduateApplicationViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var application = new GraduateApplication
                {
                    ArabicName = viewModel.ArabicName,
                    EnglishName = viewModel.EnglishName,
                    NationalIdNumber = viewModel.NationalIdNumber,
                    NationalIdTypeId = viewModel.NationalIdTypeId,
                    BirthDate = viewModel.BirthDate,
                    BirthPlace = viewModel.BirthPlace,
                    Nationality = viewModel.Nationality,
                    GenderId = viewModel.GenderId,
                    ApplicationStatusId = viewModel.ApplicationStatusId,
                    SupervisorId = viewModel.SupervisorId,
                    SubmissionDate = DateTime.Now,
                    ContactInfo = viewModel.ContactInfo
                };

                if (viewModel.PersonalPhotoFile != null && viewModel.PersonalPhotoFile.ContentLength > 0)
                {
                    application.PersonalPhotoPath = SaveFile(viewModel.PersonalPhotoFile, "PersonalPhotos");
                }

                // ربط بطلب الامتحان
                var examApp = db.ExamApplications
                    .OrderByDescending(e => e.ApplicationDate)
                    .FirstOrDefault(e => e.NationalIdNumber == viewModel.NationalIdNumber);

                if (examApp != null)
                {
                    application.ExamApplicationId = examApp.Id;
                }

                db.GraduateApplications.Add(application);
                db.SaveChanges();

                // نقل البيانات تلقائياً
                if (examApp != null)
                {
                    TransferDataFromExam(application.Id, examApp.Id);
                }

                AuditService.LogAction("Create", "GraduateApplications", $"Application for '{application.ArabicName}' (ID: {application.Id}) created.");
                TempData["SuccessMessage"] = "تمت إضافة الطلب بنجاح، وتم نسخ البيانات المتوفرة من سجل الامتحان.";

                return RedirectToAction("Details", new { id = application.Id });
            }

            // إعادة التعبئة عند الفشل
            viewModel.Genders = new SelectList(db.Genders, "Id", "Name", viewModel.GenderId);
            viewModel.NationalIdTypes = new SelectList(db.NationalIdTypes, "Id", "Name", viewModel.NationalIdTypeId);
            viewModel.ApplicationStatuses = new SelectList(db.ApplicationStatuses, "Id", "Name", viewModel.ApplicationStatusId);
            viewModel.Countries = new SelectList(GetCountries(), "Value", "Text", viewModel.Nationality);
            viewModel.Governorates = new SelectList(GetPalestinianGovernorates(), "Value", "Text", viewModel.ContactInfo?.Governorate);

            // 💡 ضمان تعبئة قوائم المودال
            viewModel.QualificationTypes = new SelectList(db.QualificationTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");
            viewModel.AttachmentTypes = new SelectList(db.AttachmentTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");

            return View(viewModel);
        }

        // ============================================================
        // تعديل بيانات المتدرب (Edit)
        // ============================================================
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications
                .Include(a => a.ContactInfo)
                .FirstOrDefault(a => a.Id == id);

            if (application == null) return HttpNotFound();

            // تحويل الكائن إلى ViewModel للعرض
            var viewModel = new GraduateApplicationViewModel
            {
                Id = application.Id,
                NewQualification = new Qualification(), // 💡 تهيئة لتجنب null
                ArabicName = application.ArabicName,
                EnglishName = application.EnglishName,
                NationalIdNumber = application.NationalIdNumber,
                NationalIdTypeId = application.NationalIdTypeId,
                BirthDate = application.BirthDate,
                BirthPlace = application.BirthPlace,
                Nationality = application.Nationality,
                GenderId = application.GenderId,

                ApplicationStatusId = application.ApplicationStatusId,

                // بيانات الاتصال
                ContactInfo = application.ContactInfo ?? new ContactInfo { Id = application.Id },

                // البيانات البنكية (اختياري)
                BankName = application.BankName,
                BankBranch = application.BankBranch,
                AccountNumber = application.AccountNumber,
                Iban = application.Iban,

                // القوائم المنسدلة
                Genders = new SelectList(db.Genders, "Id", "Name", application.GenderId),
                NationalIdTypes = new SelectList(db.NationalIdTypes, "Id", "Name", application.NationalIdTypeId),
                Countries = new SelectList(GetCountries(), "Value", "Text", application.Nationality),
                Governorates = new SelectList(GetPalestinianGovernorates(), "Value", "Text", application.ContactInfo?.Governorate),

                ApplicationStatuses = new SelectList(db.ApplicationStatuses, "Id", "Name", application.ApplicationStatusId),

                // 💡 تم إضافة القوائم التالية لحل الخطأ (لأن المودال يحتاجها)
                QualificationTypes = new SelectList(db.QualificationTypes.OrderBy(t => t.Name).ToList(), "Id", "Name"),
                AttachmentTypes = new SelectList(db.AttachmentTypes.OrderBy(t => t.Name).ToList(), "Id", "Name")
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(GraduateApplicationViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var appInDb = db.GraduateApplications
                    .Include(a => a.ContactInfo)
                    .FirstOrDefault(a => a.Id == viewModel.Id);

                if (appInDb == null) return HttpNotFound();

                // 1. تحديث البيانات الأساسية
                appInDb.ArabicName = viewModel.ArabicName;
                appInDb.EnglishName = viewModel.EnglishName;
                appInDb.NationalIdNumber = viewModel.NationalIdNumber;
                appInDb.NationalIdTypeId = viewModel.NationalIdTypeId;
                appInDb.BirthDate = viewModel.BirthDate;
                appInDb.BirthPlace = viewModel.BirthPlace;
                appInDb.Nationality = viewModel.Nationality;
                appInDb.GenderId = viewModel.GenderId;
                appInDb.ApplicationStatusId = viewModel.ApplicationStatusId;

                // تحديث الصورة الشخصية إذا تم رفع واحدة جديدة
                if (viewModel.PersonalPhotoFile != null && viewModel.PersonalPhotoFile.ContentLength > 0)
                {
                    appInDb.PersonalPhotoPath = SaveFile(viewModel.PersonalPhotoFile, "PersonalPhotos");
                }

                // 2. تحديث البيانات المالية
                appInDb.BankName = viewModel.BankName;
                appInDb.BankBranch = viewModel.BankBranch;
                appInDb.AccountNumber = viewModel.AccountNumber;
                appInDb.Iban = viewModel.Iban;

                // 3. تحديث بيانات الاتصال
                if (appInDb.ContactInfo == null)
                {
                    viewModel.ContactInfo.Id = appInDb.Id; // ربط الـ ID (One-to-One)
                    db.ContactInfos.Add(viewModel.ContactInfo);
                }
                else
                {
                    appInDb.ContactInfo.Governorate = viewModel.ContactInfo.Governorate;
                    appInDb.ContactInfo.City = viewModel.ContactInfo.City;
                    appInDb.ContactInfo.Street = viewModel.ContactInfo.Street;
                    appInDb.ContactInfo.BuildingNumber = viewModel.ContactInfo.BuildingNumber;
                    appInDb.ContactInfo.MobileNumber = viewModel.ContactInfo.MobileNumber;
                    appInDb.ContactInfo.Email = viewModel.ContactInfo.Email;
                    appInDb.ContactInfo.EmergencyContactPerson = viewModel.ContactInfo.EmergencyContactPerson;
                    appInDb.ContactInfo.EmergencyContactNumber = viewModel.ContactInfo.EmergencyContactNumber;
                }

                db.Entry(appInDb).State = EntityState.Modified;
                db.SaveChanges();

                // ✅ Audit
                AuditService.LogAction("Edit Trainee Data", "GraduateApplications", $"Updated profile for {appInDb.ArabicName} (ID: {appInDb.Id})");

                TempData["SuccessMessage"] = "تم حفظ التعديلات بنجاح.";

                // العودة لصفحة التفاصيل في RegisteredTrainees إذا كان متدرباً، وإلا لصفحة الطلبات
                return RedirectToAction("Details", "RegisteredTrainees", new { id = viewModel.Id });
            }

            // إعادة تعبئة القوائم عند الفشل
            viewModel.Genders = new SelectList(db.Genders, "Id", "Name", viewModel.GenderId);
            viewModel.NationalIdTypes = new SelectList(db.NationalIdTypes, "Id", "Name", viewModel.NationalIdTypeId);
            viewModel.Countries = new SelectList(GetCountries(), "Value", "Text", viewModel.Nationality);
            viewModel.Governorates = new SelectList(GetPalestinianGovernorates(), "Value", "Text", viewModel.ContactInfo?.Governorate);

            viewModel.ApplicationStatuses = new SelectList(db.ApplicationStatuses, "Id", "Name", viewModel.ApplicationStatusId);

            // 💡 إعادة تعبئة قوائم المودال أيضاً عند الفشل (مهم جداً لحل الخطأ)
            viewModel.QualificationTypes = new SelectList(db.QualificationTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");
            viewModel.AttachmentTypes = new SelectList(db.AttachmentTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");

            // ضمان تهيئة الكائن الفرعي
            if (viewModel.NewQualification == null) viewModel.NewQualification = new Qualification();

            return View(viewModel);
        }

        // ==================================================================
        // 5. إدارة المؤهلات والمرفقات
        // ==================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AddQualification(GraduateApplicationViewModel viewModel)
        {
            if (viewModel.Id <= 0)
            {
                TempData["ErrorMessage"] = "خطأ: رقم الطلب غير صحيح.";
                return RedirectToAction("Index");
            }

            var qualification = new Qualification
            {
                GraduateApplicationId = viewModel.Id,
                QualificationTypeId = viewModel.NewQualification.QualificationTypeId,
                UniversityName = viewModel.NewQualification.UniversityName,
                Faculty = viewModel.NewQualification.Faculty,
                Specialization = viewModel.NewQualification.Specialization,
                GraduationYear = viewModel.NewQualification.GraduationYear
            };

            if (viewModel.NewQualification.GradePercentage.HasValue)
            {
                qualification.GradePercentage = viewModel.NewQualification.GradePercentage.Value;
            }

            if (qualification.QualificationTypeId > 0 && !string.IsNullOrEmpty(qualification.UniversityName))
            {
                try
                {
                    db.Qualifications.Add(qualification);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "تمت إضافة المؤهل بنجاح.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "حدث خطأ أثناء الحفظ: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "بيانات المؤهل ناقصة.";
            }

            return RedirectToAction("Details", new { id = viewModel.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteQualification(int qualificationId, int applicationId)
        {
            var qualification = db.Qualifications.Find(qualificationId);
            if (qualification != null)
            {
                db.Qualifications.Remove(qualification);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حذف المؤهل.";
            }
            return RedirectToAction("Details", new { id = applicationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AddAttachment(GraduateApplicationViewModel viewModel)
        {
            if (viewModel.NewAttachmentFile != null && viewModel.NewAttachmentFile.ContentLength > 0 && viewModel.NewAttachmentTypeId.HasValue)
            {
                try
                {
                    string subFolder = $"Attachments/{viewModel.Id}";
                    string directoryPath = Server.MapPath($"~/Uploads/{subFolder}");
                    if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.NewAttachmentFile.FileName);
                    string fullPath = Path.Combine(directoryPath, fileName);

                    viewModel.NewAttachmentFile.SaveAs(fullPath);

                    var attachment = new Attachment
                    {
                        GraduateApplicationId = viewModel.Id,
                        AttachmentTypeId = viewModel.NewAttachmentTypeId.Value,
                        FilePath = $"/Uploads/{subFolder}/{fileName}",
                        OriginalFileName = Path.GetFileName(viewModel.NewAttachmentFile.FileName),
                        UploadDate = DateTime.Now
                    };

                    db.Attachments.Add(attachment);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "تم رفع المرفق بنجاح.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "فشل الرفع: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "يجب اختيار ملف وتحديد نوعه.";
            }
            return RedirectToAction("Details", new { id = viewModel.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteAttachment(int attachmentId, int applicationId)
        {
            var attachment = db.Attachments.Find(attachmentId);
            if (attachment != null)
            {
                try
                {
                    var physicalPath = Server.MapPath(attachment.FilePath);
                    if (System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);
                }
                catch { }

                db.Attachments.Remove(attachment);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حذف المرفق.";
            }
            return RedirectToAction("Details", new { id = applicationId });
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult GetAttachment(int id)
        {
            var attachment = db.Attachments.Find(id);
            if (attachment == null || string.IsNullOrEmpty(attachment.FilePath)) return HttpNotFound();

            var physicalPath = Server.MapPath(attachment.FilePath);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound("الملف غير موجود على السيرفر.");

            string contentType = MimeMapping.GetMimeMapping(physicalPath);
            return File(physicalPath, contentType);
        }

        // ==================================================================
        // 6. دوال مساعدة (Helpers)
        // ==================================================================

        private void TransferDataFromExam(int graduateAppId, int examAppId)
        {
            try
            {
                var examApp = db.ExamApplications
                    .Include(e => e.Qualifications)
                    .FirstOrDefault(e => e.Id == examAppId);

                if (examApp == null) return;

                foreach (var examQual in examApp.Qualifications)
                {
                    var bachType = db.QualificationTypes.FirstOrDefault(t => t.Name.Contains("بكالوريوس") || t.Name.Contains("ليسانس"));
                    int typeId = bachType != null ? bachType.Id : 1;

                    var qual = new Qualification
                    {
                        GraduateApplicationId = graduateAppId,
                        QualificationTypeId = typeId,
                        UniversityName = examQual.UniversityName,
                        GraduationYear = examQual.GraduationYear,
                        Faculty = "غير متوفر",
                        Specialization = "غير متوفر",
                        GradePercentage = null
                    };
                    db.Qualifications.Add(qual);
                }

                if (!string.IsNullOrEmpty(examApp.BachelorCertificatePath))
                {
                    var certType = db.AttachmentTypes.FirstOrDefault(t => t.Name.Contains("شهادة") || t.Name.Contains("جامعة"));
                    int typeId = certType != null ? certType.Id : 1;

                    db.Attachments.Add(new Attachment
                    {
                        GraduateApplicationId = graduateAppId,
                        AttachmentTypeId = typeId,
                        FilePath = examApp.BachelorCertificatePath,
                        OriginalFileName = "منقول_من_طلب_الامتحان.pdf",
                        UploadDate = DateTime.Now
                    });
                }

                if (!string.IsNullOrEmpty(examApp.PersonalIdPath))
                {
                    var idType = db.AttachmentTypes.FirstOrDefault(t => t.Name.Contains("هوية"));
                    int typeId = idType != null ? idType.Id : 1;

                    db.Attachments.Add(new Attachment
                    {
                        GraduateApplicationId = graduateAppId,
                        AttachmentTypeId = typeId,
                        FilePath = examApp.PersonalIdPath,
                        OriginalFileName = "ID_From_Exam.pdf",
                        UploadDate = DateTime.Now
                    });
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error transferring data: " + ex.Message);
            }
        }

        private string SaveFile(HttpPostedFileBase file, string subFolder)
        {
            var fileName = Path.GetFileName(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var directoryPath = Server.MapPath($"~/Uploads/{subFolder}");
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            var fullPath = Path.Combine(directoryPath, uniqueFileName);
            file.SaveAs(fullPath);
            return $"/Uploads/{subFolder}/{uniqueFileName}";
        }

        private static List<SelectListItem> GetPalestinianGovernorates()
        {
            return new List<SelectListItem> {
                new SelectListItem { Text = "محافظة غزة", Value = "محافظة غزة" },
                new SelectListItem { Text = "محافظة خان يونس", Value = "محافظة خان يونس" },
                new SelectListItem { Text = "محافظة رفح", Value = "محافظة رفح" },
                new SelectListItem { Text = "محافظة شمال غزة", Value = "محافظة شمال غزة" },
                new SelectListItem { Text = "محافظة دير البلح", Value = "محافظة دير البلح" },
                new SelectListItem { Text = "محافظة القدس", Value = "محافظة القدس" },
                new SelectListItem { Text = "محافظة رام الله والبيرة", Value = "محافظة رام الله والبيرة" },
                new SelectListItem { Text = "محافظة نابلس", Value = "محافظة نابلس" },
                new SelectListItem { Text = "محافظة الخليل", Value = "محافظة الخليل" },
                new SelectListItem { Text = "محافظة جنين", Value = "محافظة جنين" },
                new SelectListItem { Text = "محافظة طولكرم", Value = "محافظة طولكرم" },
                new SelectListItem { Text = "محافظة قلقيلية", Value = "محافظة قلقيلية" },
                new SelectListItem { Text = "محافظة بيت لحم", Value = "محافظة بيت لحم" },
                new SelectListItem { Text = "محافظة أريحا", Value = "محافظة أريحا" },
                new SelectListItem { Text = "محافظة سلفيت", Value = "محافظة سلفيت" },
                new SelectListItem { Text = "محافظة طوباس", Value = "محافظة طوباس" }
             };
        }

        private static List<SelectListItem> GetCountries()
        {
            return new List<SelectListItem> {
                new SelectListItem { Text = "دولة فلسطين", Value = "دولة فلسطين" },
                new SelectListItem { Text = "مصر", Value = "مصر" },
                new SelectListItem { Text = "الأردن", Value = "الأردن" },
                new SelectListItem { Text = "لبنان", Value = "لبنان" },
                new SelectListItem { Text = "سوريا", Value = "سوريا" },
                new SelectListItem { Text = "العراق", Value = "العراق" },
                new SelectListItem { Text = "الجزائر", Value = "الجزائر" },
                new SelectListItem { Text = "تونس", Value = "تونس" },
                new SelectListItem { Text = "المغرب", Value = "المغرب" },
                new SelectListItem { Text = "السعودية", Value = "السعودية" },
                new SelectListItem { Text = "الإمارات", Value = "الإمارات" },
                new SelectListItem { Text = "قطر", Value = "قطر" },
                new SelectListItem { Text = "الكويت", Value = "الكويت" },
                new SelectListItem { Text = "عمان", Value = "عمان" },
                new SelectListItem { Text = "البحرين", Value = "البحرين" },
                new SelectListItem { Text = "اليمن", Value = "اليمن" },
                new SelectListItem { Text = "ليبيا", Value = "ليبيا" },
                new SelectListItem { Text = "السودان", Value = "السودان" }
             };
        }

        // ==================================================================
        // 7. الطباعة والتقارير (Printing) - الدوال المضافة
        // ==================================================================
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintApplicationForm(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications.AsNoTracking()
                .Include(a => a.Gender)
                .Include(a => a.NationalIdType)
                .Include(a => a.ApplicationStatus)
                .Include(a => a.ContactInfo)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Attachments.Select(att => att.AttachmentType))
                .Include(a => a.Supervisor)
                .FirstOrDefault(a => a.Id == id);

            if (application == null) return HttpNotFound();

            // جلب أعضاء المجلس للتوقيعات
            ViewBag.CouncilMembers = db.CouncilMembers.Where(m => m.IsActive).ToList();

            // تجهيز الـ ViewModel المطلوب للعرض
            var viewModel = new TraineeReviewViewModel
            {
                Id = application.Id,
                ArabicName = application.ArabicName,
                EnglishName = application.EnglishName,
                NationalIdNumber = application.NationalIdNumber,
                BirthDate = application.BirthDate,
                BirthPlace = application.BirthPlace,
                Nationality = application.Nationality,
                PersonalPhotoPath = application.PersonalPhotoPath,
                Status = application.ApplicationStatus?.Name,
                Gender = application.Gender,
                ContactInfo = application.ContactInfo,
                Supervisor = application.Supervisor,
                Qualifications = application.Qualifications.ToList(),
                Attachments = application.Attachments.ToList()
            };

            // 💡 التصحيح هنا: يجب إرسال viewModel وليس application
            return View(viewModel);
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintReport(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications.AsNoTracking()
                .Include(a => a.Gender) // Added: To prevent null reference on Model.Gender.Name
                .Include(a => a.NationalIdType) // Added: To prevent null reference on Model.NationalIdType.Name
                .Include(a => a.ContactInfo)
                .Include(a => a.ApplicationStatus)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Supervisor)
                .FirstOrDefault(a => a.Id == id);

            if (application == null) return HttpNotFound();

            return View(application);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}