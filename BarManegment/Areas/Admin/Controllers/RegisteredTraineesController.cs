using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.IO;
using BarManegment.ViewModels;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class RegisteredTraineesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ... (Index و Details كما هي بدون تغيير لأنها عمليات عرض فقط) ...
        // ============================================================
        // 1. القائمة والبحث (Index)
        // ============================================================
        public ActionResult Index(string searchTerm, string filterType)
        {
            var relevantStatuses = new List<string> { "متدرب مقيد", "متدرب موقوف", "محامي مزاول" };

            var query = db.GraduateApplications.AsNoTracking()
                .Include(a => a.ApplicationStatus)
                .Include(a => a.Supervisor)
                .Include(a => a.ContactInfo)
                .Where(a => relevantStatuses.Contains(a.ApplicationStatus.Name));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a =>
                    a.ArabicName.Contains(searchTerm) ||
                    a.NationalIdNumber.Contains(searchTerm) ||
                    a.TraineeSerialNo.Contains(searchTerm) ||
                    a.Supervisor.ArabicName.Contains(searchTerm)
                );
            }

            if (filterType == "Active") query = query.Where(t => t.ApplicationStatus.Name == "متدرب مقيد");
            else if (filterType == "Suspended") query = query.Where(t => t.ApplicationStatus.Name == "متدرب موقوف");

            var trainees = query.OrderByDescending(a => a.TraineeSerialNo).Take(100).ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.FilterType = filterType;

            return View(trainees);
        }

        // ============================================================
        // 2. التفاصيل (الملف الشخصي الشامل)
        // ============================================================
        public ActionResult Details(int? id)
        {
            // ... (الكود كما هو تماماً في سؤالك) ...
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var graduateApp = db.GraduateApplications.AsNoTracking()
                .Include(a => a.ContactInfo)
                .Include(a => a.Supervisor.ApplicationStatus)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Attachments.Select(at => at.AttachmentType))
                .Include(a => a.ApplicationStatus)
                .Include(a => a.Gender)
                .Include(a => a.ExamApplication)
                .FirstOrDefault(a => a.Id == id);

            if (graduateApp == null) return HttpNotFound();

            var allowedStatuses = new List<string> { "متدرب مقيد", "متدرب موقوف", "محامي مزاول", "محامي غير مزاول" };
            if (!allowedStatuses.Contains(graduateApp.ApplicationStatus.Name))
            {
                TempData["ErrorMessage"] = "هذا الملف لا يعود لمتدرب مقيد أو محامي.";
                return RedirectToAction("Details", "GraduateApplications", new { id = id });
            }

            // --- جلب السجلات التاريخية ---
            var paymentHistory = db.Receipts.AsNoTracking().Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType)).Where(r => r.PaymentVoucher.GraduateApplicationId == id).OrderByDescending(r => r.BankPaymentDate).ToList();
            var renewals = db.TraineeRenewals.AsNoTracking().Where(r => r.TraineeId == id).OrderByDescending(r => r.RenewalYear).ToList();
            var supervisorChangeRequests = db.SupervisorChangeRequests.AsNoTracking().Include(r => r.NewSupervisor).Where(r => r.TraineeId == id).OrderByDescending(r => r.RequestDate).ToList();

            // الإيقافات (تشمل الإدارية)
            var councilSuspensions = db.TraineeSuspensions.AsNoTracking().Include(s => s.CreatedByUser).Where(s => s.GraduateApplicationId == id).OrderByDescending(s => s.SuspensionStartDate).ToList();

            // الامتحانات (بما في ذلك الامتحانات من طلب القبول القديم)
            int? traineeExamAppId = graduateApp.ExamApplicationId;
            var examHistory = db.ExamEnrollments.AsNoTracking()
                .Include(e => e.Exam.ExamType)
                .Where(e => e.GraduateApplicationId == id || (traineeExamAppId.HasValue && e.ExamApplicationId == traineeExamAppId.Value))
                .OrderByDescending(e => e.Exam.StartTime)
                .ToList();

            var oralExamHistory = db.OralExamEnrollments.AsNoTracking().Include(o => o.OralExamCommittee).Where(o => o.GraduateApplicationId == id).OrderByDescending(o => o.ExamDate).ToList();
            var oathRequestHistory = db.OathRequests.AsNoTracking().Where(o => o.GraduateApplicationId == id).OrderByDescending(o => o.RequestDate).ToList();
            var trainingLogs = db.TrainingLogs.AsNoTracking().Include(l => l.Supervisor).Where(l => l.GraduateApplicationId == id).OrderByDescending(l => l.Year).ThenByDescending(l => l.Month).ToList();

            // الأبحاث وقراراتها (مهم جداً Include Decisions)
            var legalResearches = db.LegalResearches.AsNoTracking().Include(r => r.Decisions).Where(r => r.GraduateApplicationId == id).OrderByDescending(r => r.SubmissionDate).ToList();

            // سجل الحضور والساعات
            var attendedSessions = db.TraineeAttendances.AsNoTracking().Include(a => a.Session.TrainingCourse).Where(a => a.TraineeId == id && a.Status == "حاضر").OrderByDescending(a => a.Session.SessionDate).ToList();
            ViewBag.AttendedSessions = attendedSessions;

            // --- الحسابات والشروط ---
            double completedHours = attendedSessions.Sum(s => s.Session.CreditHours);
            var requiredHoursSetting = db.SystemSettings.Find("RequiredTrainingHours");
            double requiredHours = requiredHoursSetting != null ? double.Parse(requiredHoursSetting.SettingValue) : 100;
            ViewBag.CompletedTrainingHours = completedHours;
            ViewBag.RequiredTrainingHours = requiredHours;

            DateTime? actualEndDate = null;
            int totalSuspensionDays = 0;
            if (graduateApp.TrainingStartDate.HasValue)
            {
                var expectedEnd = graduateApp.TrainingStartDate.Value.AddYears(2);
                foreach (var suspension in councilSuspensions)
                {
                    if (suspension.Status == "معتمد" || suspension.Status.Contains("منتهية"))
                    {
                        if (suspension.SuspensionEndDate.HasValue)
                            totalSuspensionDays += (suspension.SuspensionEndDate.Value - suspension.SuspensionStartDate).Days;
                        else
                            totalSuspensionDays += (DateTime.Now - suspension.SuspensionStartDate).Days; // إيقاف مستمر
                    }
                }
                actualEndDate = expectedEnd.AddDays(totalSuspensionDays);
            }
            ViewBag.TotalSuspensionDays = totalSuspensionDays;
            ViewBag.ActualEndDate = actualEndDate;

            // --- التحقق من الأهلية ---
            // 💡 التعديل المطلوب: توسيع نطاق البحث عن الامتحان الكتابي ليشمل "إلكتروني" أو "إنهاء تدريب" بشكل عام
            bool hasPassedWrittenExam = examHistory.Any(e =>
                (e.Exam.ExamType.Name.Contains("تحريري") ||
                 e.Exam.ExamType.Name.Contains("إلكتروني") ||
                 e.Exam.ExamType.Name.Contains("إنهاء تدريب"))
                && e.Result == "ناجح");

            bool hasPassedOralExam = oralExamHistory.Any(o => o.Result == "ناجح");

            // الشرط: البحث مقبول أو مكتمل أو يوجد قرار ناجح
            bool researchAccepted = legalResearches.Any(r => r.Status == "مقبول" || r.Status == "مكتمل" || (r.Decisions != null && r.Decisions.Any(d => d.Result == "ناجح")));

            bool trainingPeriodCompleted = actualEndDate.HasValue && DateTime.Now >= actualEndDate.Value;
            bool trainingHoursCompleted = requiredHours == 0 || completedHours >= requiredHours;

            var eligibilityIssues = new List<string>();
            if (!hasPassedWrittenExam) eligibilityIssues.Add("لم يجتز امتحان إنهاء التدريب (التحريري/الإلكتروني).");
            if (!hasPassedOralExam) eligibilityIssues.Add("لم يجتز الامتحان الشفوي.");
            if (!researchAccepted) eligibilityIssues.Add("لم يتم قبول البحث القانوني.");
            if (!trainingPeriodCompleted) eligibilityIssues.Add("لم يكمل المدة القانونية.");
            if (!trainingHoursCompleted) eligibilityIssues.Add($"لم يكمل ساعات التدريب المطلوبة ({completedHours}/{requiredHours}).");
            if (graduateApp.ApplicationStatus.Name != "متدرب مقيد") eligibilityIssues.Add("الحالة الحالية ليست 'متدرب مقيد'.");

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
                Gender = graduateApp.Gender,
                TelegramChatId = graduateApp.TelegramChatId,
                ContactInfo = graduateApp.ContactInfo ?? new ContactInfo(),
                Supervisor = graduateApp.Supervisor,
                Qualifications = graduateApp.Qualifications.ToList(),
                Attachments = graduateApp.Attachments.ToList(),
                TraineeSerialNo = graduateApp.TraineeSerialNo,
                TrainingStartDate = graduateApp.TrainingStartDate,
                PaymentHistory = paymentHistory,
                SupervisorChangeRequests = supervisorChangeRequests,
                Renewals = renewals,
                LegalResearches = legalResearches,
                ExamHistory = examHistory,
                OralExamHistory = oralExamHistory,
                OathRequestHistory = oathRequestHistory,
                CouncilSuspensions = councilSuspensions,
                TrainingLogs = trainingLogs,
                CanApplyForOath = (!eligibilityIssues.Any()),
                OathEligibilityIssues = eligibilityIssues,
                IsPracticingLawyer = (graduateApp.ApplicationStatus.Name == "محامي مزاول")
            };

            ViewBag.QualificationTypes = new SelectList(db.QualificationTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");
            ViewBag.AttachmentTypes = new SelectList(db.AttachmentTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");

            return View(viewModel);
        }

        // ============================================================
        // 3. الإجراءات الإدارية
        // ============================================================

        // أ. إنشاء إيقاف (موجود فيه اللوج مسبقاً)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult CreateCouncilSuspension(int TraineeId, string Reason, DateTime SuspensionStartDate, DateTime SuspensionEndDate)
        {
            var trainee = db.GraduateApplications
                .Include(t => t.ApplicationStatus)
                .FirstOrDefault(t => t.Id == TraineeId);

            if (trainee == null) return HttpNotFound();

            if (SuspensionEndDate <= SuspensionStartDate)
            {
                TempData["ErrorMessage"] = "تاريخ الانتهاء يجب أن يكون بعد تاريخ البدء.";
                return RedirectToAction("Details", new { id = TraineeId });
            }

            var suspension = new TraineeSuspension
            {
                GraduateApplicationId = TraineeId,
                Reason = Reason,
                SuspensionStartDate = SuspensionStartDate,
                SuspensionEndDate = SuspensionEndDate,
                DecisionDate = DateTime.Now,
                CreatedByUserId = (int)Session["UserId"],
                Status = "معتمد"
            };
            db.TraineeSuspensions.Add(suspension);

            // إيقاف المتدرب فوراً إذا كان التاريخ سارياً
            if (DateTime.Now >= SuspensionStartDate && DateTime.Now <= SuspensionEndDate)
            {
                var stoppedStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب موقوف");
                // التأكد من أن الحالة الحالية تسمح بالإيقاف (مقيد)
                if (stoppedStatus != null && trainee.ApplicationStatus.Name == "متدرب مقيد")
                {
                    trainee.ApplicationStatusId = stoppedStatus.Id;
                    db.Entry(trainee).State = EntityState.Modified;
                }
            }

            db.SaveChanges();
            TempData["SuccessMessage"] = "تم تسجيل الإيقاف الإداري بنجاح.";
            AuditService.LogAction("Suspend Trainee", "RegisteredTrainees", $"Suspended trainee {trainee.ArabicName}");
            return RedirectToAction("Details", new { id = TraineeId });
        }

        // ب. إنهاء الإيقاف (موجود فيه اللوج مسبقاً)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult EndCouncilSuspension(int suspensionId, DateTime returnDate)
        {
            var suspension = db.TraineeSuspensions
                .Include(s => s.Trainee.ApplicationStatus)
                .FirstOrDefault(s => s.Id == suspensionId);

            if (suspension == null) return HttpNotFound();

            // التحقق من التاريخ
            if (returnDate < suspension.SuspensionStartDate)
            {
                TempData["ErrorMessage"] = "تاريخ العودة لا يمكن أن يكون قبل تاريخ بدء الإيقاف.";
                return RedirectToAction("Details", new { id = suspension.GraduateApplicationId });
            }

            // 1. تحديث سجل الإيقاف
            suspension.SuspensionEndDate = returnDate;
            suspension.Status = "منتهية (تم الاستئناف)";

            // 2. إعادة تفعيل المتدرب (إعادته لمتدرب مقيد)
            var activeStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب مقيد");

            // التغيير فقط إذا كان حالياً "موقوف"
            if (activeStatus != null && suspension.Trainee.ApplicationStatus.Name == "متدرب موقوف")
            {
                suspension.Trainee.ApplicationStatusId = activeStatus.Id;
                db.Entry(suspension.Trainee).State = EntityState.Modified;
            }

            db.SaveChanges();

            AuditService.LogAction("Resume Trainee", "RegisteredTrainees", $"Resumed training for {suspension.Trainee.ArabicName}. Suspension ID: {suspensionId}");
            TempData["SuccessMessage"] = "تم استئناف التدريب بنجاح وتحديث الحالة.";

            return RedirectToAction("Details", new { id = suspension.GraduateApplicationId });
        }

        // ج. إدارة المشرفين (موجود فيه اللوج)
        [HttpGet]
        public JsonResult SearchSupervisors(string searchTerm)
        {
            // ... (نفس الكود) ...
            if (string.IsNullOrWhiteSpace(searchTerm)) return Json(new { success = false, message = "الرجاء إدخال كلمة بحث." }, JsonRequestBehavior.AllowGet);

            using (var svc = new SupervisorService())
            {
                var results = svc.SearchEligibleSupervisors(searchTerm);
                if (results.Count == 0) return Json(new { success = false, message = "لا توجد نتائج." }, JsonRequestBehavior.AllowGet);

                var formattedResults = results.Select(s => new {
                    id = s.Id,
                    name = s.Name,
                    practiceDate = s.PracticeDate,
                    isEligible = s.IsEligible,
                    ineligibilityReason = s.IneligibilityReason
                }).ToList();

                return Json(new { success = true, supervisors = formattedResults }, JsonRequestBehavior.AllowGet);
            }
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
            AuditService.LogAction("Assign Supervisor", "RegisteredTrainees", $"Assigned supervisor {supervisorId} to trainee {applicationId}");
            TempData["SuccessMessage"] = "تم تعيين المشرف بنجاح.";
            return RedirectToAction("Details", new { id = applicationId });
        }

        // د. المؤهلات والمرفقات
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddQualification(FormCollection form)
        {
            try
            {
                int appId = int.Parse(form["applicationId"]);
                var qual = new Qualification
                {
                    GraduateApplicationId = appId,
                    QualificationTypeId = int.Parse(form["QualificationTypeId"]),
                    UniversityName = form["UniversityName"],
                    Faculty = form["Faculty"],
                    Specialization = form["Specialization"],
                    GraduationYear = int.Parse(form["GraduationYear"])
                };
                if (!string.IsNullOrWhiteSpace(form["GradePercentage"]) && double.TryParse(form["GradePercentage"], out double grade))
                    qual.GradePercentage = grade;

                db.Qualifications.Add(qual);
                db.SaveChanges();

                // >>> إضافة اللوج <<<
                AuditService.LogAction("Add Qualification", "Qualifications", $"AppID {appId}, University: {qual.UniversityName}, Specialization: {qual.Specialization}");

                TempData["SuccessMessage"] = "تمت إضافة المؤهل.";
                return RedirectToAction("Details", new { id = appId });
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "خطأ: " + ex.Message; return RedirectToAction("Index"); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAttachment(int applicationId, int AttachmentTypeId, HttpPostedFileBase UploadedFile)
        {
            if (UploadedFile != null && UploadedFile.ContentLength > 0)
            {
                try
                {
                    string path = Server.MapPath($"~/Uploads/Attachments/{applicationId}");
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(UploadedFile.FileName);
                    UploadedFile.SaveAs(Path.Combine(path, fileName));

                    var att = new Attachment
                    {
                        GraduateApplicationId = applicationId,
                        AttachmentTypeId = AttachmentTypeId,
                        FilePath = $"/Uploads/Attachments/{applicationId}/{fileName}",
                        OriginalFileName = Path.GetFileName(UploadedFile.FileName),
                        UploadDate = DateTime.Now
                    };
                    db.Attachments.Add(att);
                    db.SaveChanges();

                    // >>> إضافة اللوج <<<
                    AuditService.LogAction("Add Attachment", "Attachments", $"AppID {applicationId}, File: {att.OriginalFileName}");

                    TempData["SuccessMessage"] = "تم رفع المرفق.";
                }
                catch (Exception ex) { TempData["ErrorMessage"] = "فشل الرفع: " + ex.Message; }
            }
            return RedirectToAction("Details", new { id = applicationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAttachment(int attachmentId, int applicationId)
        {
            var attachment = db.Attachments.Find(attachmentId);
            if (attachment != null)
            {
                try { System.IO.File.Delete(Server.MapPath(attachment.FilePath)); } catch { }
                db.Attachments.Remove(attachment);
                db.SaveChanges();

                // >>> إضافة اللوج <<<
                AuditService.LogAction("Delete Attachment", "Attachments", $"AttachmentID {attachmentId}, AppID {applicationId}");

                TempData["SuccessMessage"] = "تم حذف المرفق.";
            }
            return RedirectToAction("Details", new { id = applicationId });
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult GetAttachmentFile(int id)
        {
            // ... (عرض فقط) ...
            var attachment = db.Attachments.Find(id);
            if (attachment == null || string.IsNullOrEmpty(attachment.FilePath)) return HttpNotFound();
            var physicalPath = Server.MapPath(attachment.FilePath);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound();
            return File(physicalPath, MimeMapping.GetMimeMapping(physicalPath));
        }

        // ============================================================
        // 4. الطباعة والتقارير
        // ============================================================
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintIdCard(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var trainee = db.GraduateApplications.Include(a => a.Supervisor).Include(a => a.ContactInfo).Include(a => a.ApplicationStatus).FirstOrDefault(a => a.Id == id);
            if (trainee == null) return HttpNotFound();
            if (trainee.ApplicationStatus.Name != "متدرب مقيد") { TempData["ErrorMessage"] = "لا يمكن الطباعة لغير المقيدين."; return RedirectToAction("Details", new { id = id }); }

            var vm = new TraineeIdCardViewModel
            {
                TraineeName = trainee.ArabicName,
                TraineeSerialNo = trainee.TraineeSerialNo,
                NationalIdNumber = trainee.NationalIdNumber,
                SupervisorName = trainee.Supervisor?.ArabicName,
                Governorate = trainee.ContactInfo?.Governorate,
                ProfessionalStatus = "محامي متدرب",
                TrainingStartDate = trainee.TrainingStartDate,
                CardIssueDate = DateTime.Now,
                CardExpiryDate = DateTime.Now.AddYears(1),
                QRCodeData = $"{Request.Url.Scheme}://{Request.Url.Authority}/Verify/Trainee/{trainee.TraineeSerialNo}",
                PersonalPhotoPath = trainee.PersonalPhotoPath
            };

            // >>> إضافة اللوج <<<
            AuditService.LogAction("Print ID Card", "RegisteredTrainees", $"Printed for {trainee.ArabicName} ({trainee.TraineeSerialNo})");

            return View(vm);
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintComprehensiveReport(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            // لاستخدام نفس منطق التقرير الشامل
            return Details(id);
        }

        // المسار: Areas/Admin/Controllers/RegisteredTraineesController.cs

        // المسار: Areas/Admin/Controllers/RegisteredTraineesController.cs

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintRegistrationCertificate(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // 1. جلب بيانات المتدرب الأساسية (بدون الامتحانات هنا لتجنب الخطأ)
            var trainee = db.GraduateApplications
                .Include(t => t.ApplicationStatus)
                .Include(t => t.Supervisor)
                .FirstOrDefault(t => t.Id == id);

            if (trainee == null) return HttpNotFound();

            // 2. جلب الامتحانات في استعلام منفصل باستخدام ID المتدرب
            // هذا يحل المشكلة لأننا نطلب من جدول الامتحانات مباشرة
            var traineeExams = db.ExamEnrollments
                .Include(e => e.Exam.ExamType)
                .Where(e => e.GraduateApplicationId == id)
                .ToList();

            // 3. تعبئة الـ ViewModel
            var viewModel = new TraineeReviewViewModel
            {
                Id = trainee.Id,
                ArabicName = trainee.ArabicName,
                NationalIdNumber = trainee.NationalIdNumber,
                TraineeSerialNo = trainee.TraineeSerialNo,
                TrainingStartDate = trainee.TrainingStartDate,
                Status = trainee.ApplicationStatus?.Name ?? "غير محدد",
                Supervisor = trainee.Supervisor,
                PersonalPhotoPath = trainee.PersonalPhotoPath,

                // نضع القائمة التي جلبناها في الخطوة 2
                ExamHistory = traineeExams
            };

            AuditService.LogAction("Print Registration Certificate", "RegisteredTrainees", $"Printed for {trainee.ArabicName}.");

            return View("PrintRegistrationCertificate", viewModel);
        }

        // ============================================================
        // تعديل بيانات المتدرب (Edit)
        // ============================================================
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            // ... (عرض فقط) ...
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications
                .Include(a => a.ContactInfo)
                .FirstOrDefault(a => a.Id == id);

            if (application == null) return HttpNotFound();

            // تحويل الكائن إلى ViewModel للعرض
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
                Governorates = new SelectList(GetPalestinianGovernorates(), "Value", "Text", application.ContactInfo?.Governorate)
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

                // تحديث الصورة الشخصية إذا تم رفع واحدة جديدة
                if (viewModel.PersonalPhotoFile != null && viewModel.PersonalPhotoFile.ContentLength > 0)
                {
                    // حذف القديمة إن وجدت (اختياري)
                    /* if (!string.IsNullOrEmpty(appInDb.PersonalPhotoPath)) { ... delete logic ... } */

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

                // ✅ Audit (موجود مسبقاً)
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

            return View(viewModel);
        }

        // ... (Private methods: GetPalestinianGovernorates, GetCountries, SaveFile) ...
        // ... (PrintApplicationForm - already has audit) ...
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


        // 💡 أكشن جديد: طباعة نموذج انتهاء التدريب
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintApplicationForm(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var graduateApp = db.GraduateApplications.AsNoTracking()
                .Include(a => a.ContactInfo)
                .Include(a => a.Supervisor)
                .FirstOrDefault(a => a.Id == id);

            if (graduateApp == null) return HttpNotFound();

            var viewModel = new TraineeReviewViewModel
            {
                Id = graduateApp.Id,
                ArabicName = graduateApp.ArabicName,
                NationalIdNumber = graduateApp.NationalIdNumber,
                TrainingStartDate = graduateApp.TrainingStartDate,
                ContactInfo = graduateApp.ContactInfo,
                Supervisor = graduateApp.Supervisor
            };

            AuditService.LogAction("Print Training Completion Form", "RegisteredTrainees", $"Printed form for {graduateApp.ArabicName}.");

            // استخدام الفيو الموجود في مجلد TrainingCommittee
            return View("~/Areas/Admin/Views/RegisteredTrainees/PrintTrainingForm.cshtml", viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}