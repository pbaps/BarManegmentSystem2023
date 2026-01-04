using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class TrainingCommitteeController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ==================================================================
        // 1. العرض والبحث (Index & Details)
        // ==================================================================
        public ActionResult Index(string searchTerm)
        {
            var viewModel = new TrainingCommitteeIndexViewModel();
            viewModel.SearchTerm = searchTerm;

            // 1. جلب طلبات التدريب
            var graduateAppsQuery = db.GraduateApplications.AsNoTracking()
                .Include(a => a.ApplicationStatus)
                .Include(a => a.Supervisor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                graduateAppsQuery = graduateAppsQuery.Where(a => a.ArabicName.Contains(searchTerm) || a.NationalIdNumber.Contains(searchTerm));
            }

            var allApps = graduateAppsQuery.ToList();

            // توزيع الطلبات على القوائم حسب الحالة
            viewModel.AwaitingCommitteeApprovalApplications = allApps
                .Where(a => a.ApplicationStatus.Name == "بانتظار الموافقة النهائية").ToList();

            viewModel.AwaitingCompletionApplications = allApps
                .Where(a => a.ApplicationStatus.Name == "بانتظار استكمال النواقص" ||
                            a.ApplicationStatus.Name == "طلب جديد" ||
                            a.ApplicationStatus.Name.Contains("ناجح")).ToList();

            viewModel.ApprovedApplications = allApps
                .Where(a => a.ApplicationStatus.Name == "مقبول (بانتظار الدفع)").ToList();

            // 2. جلب المعفيين (من جدول الامتحانات)
            var exemptedQuery = db.ExamApplications.AsNoTracking()
                .Include(e => e.Gender)
                .Where(e => e.Status == "معفى (مؤهل للتسجيل)" && e.IsAccountCreated == false);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                exemptedQuery = exemptedQuery.Where(a => a.FullName.Contains(searchTerm) || a.NationalIdNumber.Contains(searchTerm));
            }

            viewModel.ExemptedApplications = exemptedQuery.OrderByDescending(e => e.ApplicationDate).ToList();

            return View(viewModel);
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var graduateApp = db.GraduateApplications.AsNoTracking()
                .Include(a => a.ContactInfo)
                .Include(a => a.Supervisor.ApplicationStatus)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Attachments.Select(at => at.AttachmentType))
                .Include(a => a.ApplicationStatus)
                .FirstOrDefault(a => a.Id == id);

            if (graduateApp == null) return HttpNotFound();

            // استخدام ViewModel العرض الشامل
            var viewModel = new TraineeReviewViewModel
            {
                Id = graduateApp.Id,
                ArabicName = graduateApp.ArabicName,
                EnglishName = graduateApp.EnglishName,
                NationalIdNumber = graduateApp.NationalIdNumber,
                BirthDate = graduateApp.BirthDate,
                BirthPlace = graduateApp.BirthPlace,
                Nationality = graduateApp.Nationality,
                PersonalPhotoPath = graduateApp.PersonalPhotoPath,
                Status = graduateApp.ApplicationStatus.Name,
                TelegramChatId = graduateApp.TelegramChatId,
                ContactInfo = graduateApp.ContactInfo ?? new ContactInfo(), // تجنب Null
                Supervisor = graduateApp.Supervisor,
                Qualifications = graduateApp.Qualifications.ToList(),
                Attachments = graduateApp.Attachments.ToList()
            };

            // القوائم المنسدلة في حال أرادت اللجنة تعديل بيانات (اختياري)
            ViewBag.QualificationTypes = new SelectList(db.QualificationTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");
            ViewBag.AttachmentTypes = new SelectList(db.AttachmentTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");

            return View(viewModel);
        }

        // ==================================================================
        // 2. اتخاذ القرارات (Approve / Reject)
        // ==================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ProcessDecision(int Id, string decision, string RejectionReason)
        {
            var graduateApp = db.GraduateApplications.Find(Id);
            if (graduateApp == null) return HttpNotFound();

            string oldStatus = db.Entry(graduateApp).Reference(g => g.ApplicationStatus).Query().FirstOrDefault()?.Name ?? "غير محدد";
            string newStatusName = "";

            if (decision == "Approve")
            {
                // التحقق من وجود مشرف
                if (graduateApp.SupervisorId == null)
                {
                    TempData["ErrorMessage"] = "لا يمكن الموافقة. يجب تعيين مشرف أولاً.";
                    return RedirectToAction("Details", new { id = Id });
                }

                // 💡 استخدام الخدمة المركزية للتحقق من أهلية المشرف قبل الموافقة النهائية
                using (var svc = new SupervisorService())
                {
                    var check = svc.CheckEligibility(graduateApp.SupervisorId.Value);
                    if (!check.IsEligible)
                    {
                        TempData["ErrorMessage"] = $"لا يمكن الاعتماد. المشرف غير مؤهل: {check.Message}";
                        return RedirectToAction("Details", new { id = Id });
                    }
                }

                var status = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "مقبول (بانتظار الدفع)");
                graduateApp.ApplicationStatusId = status.Id;
                newStatusName = status.Name;
                TempData["SuccessMessage"] = "تم اعتماد الطلب بنجاح. المتدرب جاهز للدفع.";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(RejectionReason))
                {
                    TempData["ErrorMessage"] = "سبب الرفض مطلوب.";
                    return RedirectToAction("Details", new { id = Id });
                }

                var status = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "مرفوض"); // أو "بانتظار استكمال النواقص" حسب السياسة
                graduateApp.ApplicationStatusId = status.Id;
                newStatusName = status.Name;

                // يمكن تخزين سبب الرفض في حقل ملاحظات (Notes)
                graduateApp.Notes = $"سبب الرفض: {RejectionReason}";

                TempData["SuccessMessage"] = "تم رفض الطلب.";
            }

            db.SaveChanges();
            AuditService.LogAction("Committee Decision", "TrainingCommittee", $"App ID {Id}: {oldStatus} -> {newStatusName}");
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult RevertApproval(int id)
        {
            var graduateApp = db.GraduateApplications.Find(id);
            if (graduateApp == null) return HttpNotFound();

            var pendingStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "بانتظار الموافقة النهائية");
            graduateApp.ApplicationStatusId = pendingStatus.Id;
            db.SaveChanges();

            AuditService.LogAction("Revert Approval", "TrainingCommittee", $"Reverted approval for App ID {id}");
            TempData["SuccessMessage"] = "تم التراجع عن الموافقة.";
            return RedirectToAction("Details", new { id = id });
        }

        // ==================================================================
        // 3. إدارة المشرفين (بحث وتعيين)
        // ==================================================================
        [HttpGet]
        public JsonResult SearchSupervisors(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new { success = false, message = "الرجاء إدخال كلمة بحث." }, JsonRequestBehavior.AllowGet);
            }

            // 1. بحث شامل
            var candidates = db.GraduateApplications.AsNoTracking()
                .Include(g => g.ApplicationStatus)
                .Where(g => g.ArabicName.Contains(searchTerm) ||
                            g.NationalIdNumber.Contains(searchTerm) ||
                            g.MembershipId.Contains(searchTerm) ||
                            g.Id.ToString() == searchTerm)
                .Take(20)
                .ToList();

            if (!candidates.Any())
            {
                return Json(new { success = false, message = "لا توجد نتائج مطابقة." }, JsonRequestBehavior.AllowGet);
            }

            var resultList = new List<object>();

            using (var svc = new SupervisorService())
            {
                foreach (var lawyer in candidates)
                {
                    // 2. فحص الأهلية
                    var check = svc.CheckEligibility(lawyer.Id);

                    string displayDate = lawyer.PracticeStartDate.HasValue
                        ? lawyer.PracticeStartDate.Value.ToString("yyyy-MM-dd")
                        : lawyer.SubmissionDate.ToString("yyyy-MM-dd");

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
        public ActionResult AssignSupervisor(int applicationId, int supervisorId)
        {
            var app = db.GraduateApplications.Find(applicationId);
            if (app == null) return HttpNotFound();

            using (var svc = new SupervisorService())
            {
                var check = svc.CheckEligibility(supervisorId);
                if (!check.IsEligible)
                {
                    TempData["ErrorMessage"] = check.Message;
                    return RedirectToAction("Details", new { id = applicationId });
                }
            }

            app.SupervisorId = supervisorId;
            db.SaveChanges();
            TempData["SuccessMessage"] = "تم تعيين المشرف بنجاح.";
            return RedirectToAction("Details", new { id = applicationId });
        }

        // ==================================================================
        // 4. إدارة المرفقات (Add/Get) - اللجنة قد تحتاج لإضافة مرفق
        // ==================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AddAttachment(int applicationId, int AttachmentTypeId, HttpPostedFileBase UploadedFile)
        {
            if (UploadedFile != null && UploadedFile.ContentLength > 0)
            {
                try
                {
                    string path = Server.MapPath($"~/Uploads/Attachments/{applicationId}");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(UploadedFile.FileName);
                    string fullPath = Path.Combine(path, fileName);

                    UploadedFile.SaveAs(fullPath);

                    var attachment = new Attachment
                    {
                        GraduateApplicationId = applicationId,
                        AttachmentTypeId = AttachmentTypeId,
                        FilePath = $"/Uploads/Attachments/{applicationId}/{fileName}",
                        OriginalFileName = Path.GetFileName(UploadedFile.FileName),
                        UploadDate = DateTime.Now
                    };
                    db.Attachments.Add(attachment);
                    db.SaveChanges();

                    AuditService.LogAction("Add Attachment (Committee)", "TrainingCommittee", $"Added attachment to App ID {applicationId}");
                    TempData["SuccessMessage"] = "تمت إضافة المرفق بنجاح.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "حدث خطأ أثناء رفع المرفق: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "الرجاء اختيار ملف.";
            }

            return RedirectToAction("Details", new { id = applicationId });
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult GetAttachmentFile(int id)
        {
            var attachment = db.Attachments.Find(id);
            if (attachment == null || string.IsNullOrEmpty(attachment.FilePath)) return HttpNotFound();

            var physicalPath = Server.MapPath(attachment.FilePath);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound("الملف غير موجود على السيرفر.");

            string mimeType = MimeMapping.GetMimeMapping(physicalPath);
            return File(physicalPath, mimeType);
        }

        // ==================================================================
        // 5. الطباعة
        // ==================================================================
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintApplicationForm(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var graduateApp = db.GraduateApplications.AsNoTracking()
                .Include(a => a.ContactInfo)
                .Include(a => a.Supervisor.ApplicationStatus)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Attachments.Select(at => at.AttachmentType))
                .Include(a => a.ApplicationStatus)
                .FirstOrDefault(a => a.Id == id);

            if (graduateApp == null) return HttpNotFound();

            // جلب أعضاء المجلس النشطين لتوقيعهم في النموذج
            ViewBag.CouncilMembers = db.CouncilMembers.Where(c => c.IsActive).ToList();

            // تحويل لـ ViewModel لتناسق العرض
            var viewModel = new TraineeReviewViewModel
            {
                Id = graduateApp.Id,
                ArabicName = graduateApp.ArabicName,
                EnglishName = graduateApp.EnglishName,
                NationalIdNumber = graduateApp.NationalIdNumber,
                BirthDate = graduateApp.BirthDate,
                BirthPlace = graduateApp.BirthPlace,
                Nationality = graduateApp.Nationality,
                PersonalPhotoPath = graduateApp.PersonalPhotoPath,
                Status = graduateApp.ApplicationStatus.Name,
                TelegramChatId = graduateApp.TelegramChatId,
                ContactInfo = graduateApp.ContactInfo,
                Supervisor = graduateApp.Supervisor,
                Qualifications = graduateApp.Qualifications.ToList(),
                Attachments = graduateApp.Attachments.ToList()
            };

            AuditService.LogAction("Print Training Form", "TrainingCommittee", $"User printed training form for App ID {id}.");

            return View("PrintTrainingForm", viewModel);
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintReport(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications.AsNoTracking()
                .Include(a => a.Gender)
                .Include(a => a.NationalIdType)
                .Include(a => a.ApplicationStatus)
                .Include(a => a.ContactInfo)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Supervisor)
                .FirstOrDefault(a => a.Id == id);

            if (application == null) return HttpNotFound();

            var universityDegree = application.Qualifications
                .FirstOrDefault(q => q.QualificationType.Name.Contains("بكالوريوس") || q.QualificationType.Name.Contains("ليسانس"));

            ViewBag.UniversityDegree = universityDegree;

            AuditService.LogAction("Print Full Report", "TrainingCommittee", $"User printed full report for App ID {id}.");

            return View(application);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}