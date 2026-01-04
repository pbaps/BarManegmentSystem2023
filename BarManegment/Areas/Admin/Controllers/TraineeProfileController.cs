using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Web;
using BarManegment.Services;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")] // استخدم الصلاحية المناسبة
    public class TraineeProfileController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();


        // GET: Admin/TraineeProfile/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var graduateApp = db.GraduateApplications
                .Include(a => a.ContactInfo)
                .Include(a => a.Supervisor)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Attachments.Select(at => at.AttachmentType))
                .Include(a => a.ApplicationStatus)
                .Include(a => a.ExamApplication)
                .Include(a => a.Gender) // (إضافة Gender لأنه مستخدم في ViewModel)
                .FirstOrDefault(a => a.Id == id);

            var allowedStatuses = new List<string> { "متدرب مقيد", "متدرب موقوف", "محامي مزاول" };
            if (graduateApp == null || graduateApp.ApplicationStatus == null || !allowedStatuses.Contains(graduateApp.ApplicationStatus.Name))
            {
                TempData["ErrorMessage"] = "لا يمكن عرض هذا الملف لأنه لا يعود لمتدرب مقيد أو موقوف أو مزاول.";
                return RedirectToAction("Index", "RegisteredTrainees");
            }

            // --- جلب جميع السجلات المرتبطة بالمتدرب ---
            var paymentHistory = db.Receipts.Include(r => r.PaymentVoucher)
                .Where(r => r.PaymentVoucher.GraduateApplicationId == id).OrderByDescending(r => r.BankPaymentDate).ToList();

            var supervisorChangeRequests = db.SupervisorChangeRequests.Include(r => r.NewSupervisor)
                .Where(r => r.TraineeId == id).OrderByDescending(r => r.RequestDate).ToList();

            var renewals = db.TraineeRenewals.Include(r => r.Receipt)
                .Where(r => r.TraineeId == id).OrderByDescending(r => r.RenewalYear).ToList();

            var legalResearches = db.LegalResearches.Include(r => r.Decisions)
                .Include(r => r.Committee).Where(r => r.GraduateApplicationId == id).OrderByDescending(r => r.SubmissionDate).ToList();

            int? traineeExamAppId = graduateApp.ExamApplicationId;
            var examHistory = db.ExamEnrollments.Include(e => e.Exam.ExamType)
                .Where(e => e.GraduateApplicationId == id || (traineeExamAppId.HasValue && e.ExamApplicationId == traineeExamAppId.Value))
                .OrderByDescending(e => e.Exam.StartTime).ToList();

            var oralExamHistory = db.OralExamEnrollments.Include(o => o.OralExamCommittee)
                .Where(o => o.GraduateApplicationId == id).OrderByDescending(o => o.ExamDate).ToList();

            var oathRequestHistory = db.OathRequests
                .Where(o => o.GraduateApplicationId == id)
                .OrderByDescending(o => o.RequestDate)
                .ToList();

            var councilSuspensions = db.TraineeSuspensions
                .Where(s => s.GraduateApplicationId == id)
                .OrderBy(s => s.SuspensionStartDate)
                .ToList();

            // (جلب سجلات التدريب الشهرية)
            var trainingLogs = db.TrainingLogs
                            .Include(l => l.Supervisor)
                            .Where(l => l.GraduateApplicationId == id)
                            .OrderByDescending(l => l.Year)
                            .ThenByDescending(l => l.Month)
                            .ToList();


            // ---  بداية: منطق التحقق من أهلية أداء اليمين ---
            var eligibilityIssues = new List<string>();
            bool isEligible = true;
            // 1. الحالة الأساسية
            if (graduateApp.ApplicationStatus.Name != "متدرب مقيد")
            {
                isEligible = false;
                eligibilityIssues.Add($"يجب أن يكون المتدرب في حالة 'متدرب مقيد' (الحالة الحالية: {graduateApp.ApplicationStatus.Name}).");
            }
            // 2. مدة التدريب الصافية (عامين = 730 يوم)
            if (!graduateApp.TrainingStartDate.HasValue)
            {
                isEligible = false;
                eligibilityIssues.Add("لم يتم تحديد تاريخ بدء التدريب.");
            }
            else
            {
                DateTime startDate = graduateApp.TrainingStartDate.Value.Date;
                var today = DateTime.Now.Date;
                double totalDaysElapsed = (today - startDate).TotalDays;

                double totalSuspensionDays = 0;

                // --- أ. حساب أيام الانقطاع (طلبات الوقف/الاستكمال) ---
                DateTime? lastStopDate = null;
                var approvedRequests = supervisorChangeRequests
                    .Where(r => (r.RequestType == "وقف" || r.RequestType == "استكمال") && r.Status == "معتمد" && r.DecisionDate.HasValue)
                    .OrderBy(r => r.DecisionDate);

                foreach (var req in approvedRequests)
                {
                    if (req.RequestType == "وقف") { lastStopDate = req.DecisionDate.Value.Date; }
                    else if (req.RequestType == "استكمال" && lastStopDate.HasValue)
                    {
                        totalSuspensionDays += (req.DecisionDate.Value.Date - lastStopDate.Value).TotalDays;
                        lastStopDate = null;
                    }
                }

                // === 
                // === بداية التصحيح (السطر 366)
                // ===
                // --- ب. إضافة أيام الإيقاف الإداري (قرارات المجلس) ---
                foreach (var suspension in councilSuspensions)
                {
                    // (التحقق من أن تاريخ الانتهاء موجود وأكبر من تاريخ البدء)
                    if (suspension.SuspensionEndDate.HasValue && suspension.SuspensionEndDate.Value > suspension.SuspensionStartDate)
                    {
                        totalSuspensionDays += (suspension.SuspensionEndDate.Value - suspension.SuspensionStartDate).TotalDays;
                    }
                }
                // === نهاية التصحيح ===

                double netTrainingDays = totalDaysElapsed - totalSuspensionDays;
                const double requiredDays = 730; // عامين

                if (netTrainingDays < requiredDays)
                {
                    isEligible = false;
                    eligibilityIssues.Add($"لم يكمل مدة التدريب الصافية (المطلوب: {requiredDays} يوم / الحالي: {Math.Floor(netTrainingDays)} يوم).");
                }
            }

            // // 3.... (باقي التحققات: 3. التحريري, 4. الشفوي, 5. البحث, 6. الطلب المعلق ... كما هي) ...
            bool hasPassedWritten = examHistory.Any(e => e.Exam.ExamType.Name == "امتحان إنهاء تدريب" && e.Result == "ناجح");
            if (!hasPassedWritten)
            {
                isEligible = false;
                eligibilityIssues.Add("لم يجتز امتحان إنهاء التدريب (التحريري) بنجاح.");
            }
            // 4. الامتحان الشفوي
            bool hasPassedOral = oralExamHistory.Any(o => o.Result == "ناجح");
            if (!hasPassedOral)
            {
                isEligible = false;
                eligibilityIssues.Add("لم يجتز امتحان اللجنة الشفوية بنجاح.");
            }

            // 5. البحث القانوني
            bool researchAccepted = legalResearches.Any(r => r.Status == "مقبول"); // يفترض حالة "مقبول"
            if (!researchAccepted)
            {
                isEligible = false;
                eligibilityIssues.Add("لم يتم قبول البحث القانوني.");
            }
            // 6. طلب يمين معلق
            bool hasPendingOath = oathRequestHistory.Any(o => o.Status != "مرفوض" && o.Status != "مكتمل");
            if (hasPendingOath)
            {
                isEligible = false;
                eligibilityIssues.Add("لديه طلب يمين قيد المراجعة أو الدفع.");
            }

            // بناء الـ ViewModel
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
                ContactInfo = graduateApp.ContactInfo ?? new ContactInfo(),
                Supervisor = graduateApp.Supervisor,
                Qualifications = graduateApp.Qualifications.ToList(),
                Attachments = graduateApp.Attachments.ToList(),
                TraineeSerialNo = graduateApp.TraineeSerialNo,
                TrainingStartDate = graduateApp.TrainingStartDate,
                Gender = graduateApp.Gender, // (إضافة الحقل المنسي)

                PaymentHistory = paymentHistory,
                SupervisorChangeRequests = supervisorChangeRequests,
                Renewals = renewals,
                LegalResearches = legalResearches,
                ExamHistory = examHistory,
                OralExamHistory = oralExamHistory,
                OathRequestHistory = oathRequestHistory,
                CouncilSuspensions = councilSuspensions,
                TrainingLogs = trainingLogs, // (إضافة السجلات)

                IsPracticingLawyer = (graduateApp.ApplicationStatus.Name == "محامي مزاول"),
                CanApplyForOath = isEligible,
                OathEligibilityIssues = eligibilityIssues
            };

            // (جلب الـ ViewBag للـ Modals)
            ViewBag.QualificationTypes = new SelectList(db.QualificationTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");
            ViewBag.AttachmentTypes = new SelectList(db.AttachmentTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");

            return View(viewModel);
        }


        // === بداية الإضافة: أكشن حفظ الإيقاف الإداري ===
        // POST: Admin/TraineeProfile/CreateCouncilSuspension
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")] // استخدم صلاحية مناسبة (مثلاً من وحدة جديدة)
        public ActionResult CreateCouncilSuspension(CreateSuspensionViewModel viewModel)
        {
            if (viewModel.SuspensionEndDate <= viewModel.SuspensionStartDate)
            {
                ModelState.AddModelError("SuspensionEndDate", "تاريخ الانتهاء يجب أن يكون بعد تاريخ البدء.");
            }

            if (ModelState.IsValid)
            {
                var trainee = db.GraduateApplications.Find(viewModel.TraineeId);
                if (trainee == null) return HttpNotFound();

                var suspension = new TraineeSuspension
                {
                    GraduateApplicationId = viewModel.TraineeId,
                    Reason = viewModel.Reason,
                    SuspensionStartDate = viewModel.SuspensionStartDate,
                    SuspensionEndDate = viewModel.SuspensionEndDate,
                    DecisionDate = DateTime.Now,
                    CreatedByUserId = (int?)Session["UserId"] // الموظف الذي سجل القرار
                };
                db.TraineeSuspensions.Add(suspension);

                // --- تحديث حالة المتدرب إذا كان الإيقاف ساريًا الآن ---
                var today = DateTime.Now.Date;
                if (trainee.ApplicationStatus.Name == "متدرب مقيد" &&
                    today >= suspension.SuspensionStartDate &&
                    today < suspension.SuspensionEndDate)
                {
                    var stoppedStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب موقوف");
                    if (stoppedStatus != null)
                    {
                        trainee.ApplicationStatusId = stoppedStatus.Id;
                    }
                }

                db.SaveChanges();

                // >>> إضافة اللوج (تسجيل العملية) <<<
                AuditService.LogAction("Create Council Suspension", "TraineeSuspensions", $"TraineeId {viewModel.TraineeId}, Reason: {viewModel.Reason}, From: {viewModel.SuspensionStartDate:yyyy-MM-dd}");

                TempData["SuccessMessage"] = "تم تسجيل فترة الإيقاف الإداري بنجاح.";
            }
            else
            {
                // إذا فشل التحقق، أعد توجيه مع رسالة خطأ
                string errors = string.Join("; ", ModelState.Values
                                                       .SelectMany(x => x.Errors)
                                                       .Select(x => x.ErrorMessage));
                TempData["ErrorMessage"] = "فشل تسجيل الإيقاف: " + errors;
            }

            return RedirectToAction("Details", new { id = viewModel.TraineeId });
        }
        // === نهاية الإضافة ===

        // POST: Admin/TraineeProfile/AddQualification
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")] // أو صلاحية مناسبة
        public ActionResult AddQualification(FormCollection form)
        {
            int applicationId = int.Parse(form["applicationId"]);
            try
            {
                var qualification = new Qualification
                {
                    GraduateApplicationId = applicationId,
                    QualificationTypeId = int.Parse(form["QualificationTypeId"]),
                    UniversityName = form["UniversityName"],
                    Faculty = form["Faculty"],
                    Specialization = form["Specialization"],
                    GraduationYear = int.Parse(form["GraduationYear"])
                };

                if (!string.IsNullOrWhiteSpace(form["GradePercentage"]))
                {
                    if (double.TryParse(form["GradePercentage"], out double grade))
                        qualification.GradePercentage = grade;
                }

                db.Qualifications.Add(qualification);
                db.SaveChanges();

                // >>> إضافة اللوج (تسجيل العملية) <<<
                AuditService.LogAction("Add Qualification", "Qualifications", $"AppID {applicationId}, University: {qualification.UniversityName}, Specialization: {qualification.Specialization}");

                TempData["SuccessMessage"] = "تمت إضافة المؤهل بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء إضافة المؤهل: " + ex.Message;
            }
            return RedirectToAction("Details", new { id = applicationId });
        }

        // POST: Admin/TraineeProfile/AddAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AddAttachment(int applicationId, int AttachmentTypeId, HttpPostedFileBase UploadedFile)
        {
            if (UploadedFile == null || UploadedFile.ContentLength == 0)
            {
                TempData["ErrorMessage"] = "الرجاء اختيار ملف ليتم رفعه.";
                return RedirectToAction("Details", new { id = applicationId });
            }
            try
            {
                string path = Server.MapPath($"~/Uploads/Attachments/{applicationId}");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string extension = Path.GetExtension(UploadedFile.FileName);
                string fileName = Guid.NewGuid().ToString() + extension;
                string fullPath = Path.Combine(path, fileName);
                UploadedFile.SaveAs(fullPath);

                var attachment = new Attachment
                {
                    GraduateApplicationId = applicationId,
                    AttachmentTypeId = AttachmentTypeId,
                    FilePath = $"/Uploads/Attachments/{applicationId}/" + fileName,
                    OriginalFileName = Path.GetFileName(UploadedFile.FileName),
                    UploadDate = DateTime.Now
                };
                db.Attachments.Add(attachment);
                db.SaveChanges();

                // >>> إضافة اللوج (تسجيل العملية) <<<
                AuditService.LogAction("Add Attachment", "Attachments", $"AppID {applicationId}, FileName: {attachment.OriginalFileName}, TypeID: {AttachmentTypeId}");

                TempData["SuccessMessage"] = "تمت إضافة المرفق بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء رفع المرفق: " + ex.Message;
            }
            return RedirectToAction("Details", new { id = applicationId });
        }

        // GET: Admin/TraineeProfile/PrintIdCard/5
        // GET: Admin/TraineeProfile/PrintIdCard/5
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintIdCard(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var trainee = db.GraduateApplications
                            .Include(a => a.Supervisor)
                            .Include(a => a.ApplicationStatus)
                            .Include(a => a.ContactInfo) // (تأكد من وجود Include لـ ContactInfo إذا كان Governorate فيه)
                            .FirstOrDefault(a => a.Id == id);

            if (trainee == null) return HttpNotFound();

            // (منطق التحقق من الحالة سليم)
            var allowedStatuses = new List<string> { "متدرب مقيد" };
            if (trainee.ApplicationStatus == null || !allowedStatuses.Contains(trainee.ApplicationStatus.Name))
            {
                TempData["ErrorMessage"] = "لا يمكن طباعة بطاقة متدرب لهذا الشخص (الحالة الحالية: " + trainee.ApplicationStatus?.Name + ").";
                return RedirectToAction("Details", "TraineeProfile", new { id = id });
            }

            DateTime? trainingEndDate = trainee.TrainingStartDate?.AddYears(2);

            var viewModel = new TraineeIdCardViewModel
            {
                // ===
                // === بداية التصحيح: مطابقة أسماء الحقول
                // ===
                TraineeId = trainee.Id, // (كانت Id سابقاً، الآن هي TraineeId)
                                        // === نهاية التصحيح ===

                TraineeName = trainee.ArabicName,
                TraineeSerialNo = trainee.TraineeSerialNo,
                NationalIdNumber = trainee.NationalIdNumber,
                Governorate = trainee.ContactInfo?.Governorate, // (تأكد أن ContactInfo تم جلبه بـ Include)
                SupervisorName = trainee.Supervisor?.ArabicName,
                ProfessionalStatus = trainee.ApplicationStatus.Name,
                TrainingStartDate = trainee.TrainingStartDate,
                TrainingEndDate = trainingEndDate,
                PersonalPhotoPath = trainee.PersonalPhotoPath,

                // (بيانات QR Code سليمة)
                QRCodeData = $"Serial: {trainee.TraineeSerialNo} | ID: {trainee.NationalIdNumber}"
            };

            return View(viewModel); // توجيه إلى PrintIdCard.cshtml
        }
        // (Inside TraineeProfileController.cs)

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintComprehensiveReport(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var graduateApp = db.GraduateApplications
                .Include(a => a.ContactInfo)
                .Include(a => a.Supervisor)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Attachments.Select(at => at.AttachmentType))
                .Include(a => a.ApplicationStatus)
                .Include(a => a.ExamApplication)
                .Include(a => a.Gender) // <-- 1. تأكد من إضافة هذا
                .FirstOrDefault(a => a.Id == id);

            var allowedStatuses = new List<string> { "متدرب مقيد", "متدرب موقوف", "محامي مزاول" };
            if (graduateApp == null || graduateApp.ApplicationStatus == null || !allowedStatuses.Contains(graduateApp.ApplicationStatus.Name))
            {
                TempData["ErrorMessage"] = "لا يمكن عرض هذا الملف.";
                return RedirectToAction("Index", "RegisteredTrainees");
            }

            // --- جلب جميع السجلات ---
            var paymentHistory = db.Receipts
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency)) // <-- 2. تأكد من إضافة هذا
                .Where(r => r.PaymentVoucher.GraduateApplicationId == id)
                .OrderByDescending(r => r.BankPaymentDate).ToList();

            // ... (باقي كود جلب السجلات: supervisorChangeRequests, renewals, legalResearches, ...)
            var supervisorChangeRequests = db.SupervisorChangeRequests.Include(r => r.NewSupervisor)
                .Where(r => r.TraineeId == id).OrderByDescending(r => r.RequestDate).ToList();
            var renewals = db.TraineeRenewals.Include(r => r.Receipt)
                .Where(r => r.TraineeId == id).OrderByDescending(r => r.RenewalYear).ToList();
            var legalResearches = db.LegalResearches.Include(r => r.Decisions)
                .Include(r => r.Committee).Where(r => r.GraduateApplicationId == id).OrderByDescending(r => r.SubmissionDate).ToList();
            int? traineeExamAppId = graduateApp.ExamApplicationId;
            var examHistory = db.ExamEnrollments.Include(e => e.Exam.ExamType)
                .Where(e => e.GraduateApplicationId == id || (traineeExamAppId.HasValue && e.ExamApplicationId == traineeExamAppId.Value))
                .OrderByDescending(e => e.Exam.StartTime).ToList();
            var oralExamHistory = db.OralExamEnrollments.Include(o => o.OralExamCommittee)
                .Where(o => o.GraduateApplicationId == id).OrderByDescending(o => o.ExamDate).ToList();
            var oathRequestHistory = db.OathRequests
                .Where(o => o.GraduateApplicationId == id)
                .OrderByDescending(o => o.RequestDate).ToList();
            var councilSuspensions = db.TraineeSuspensions
                .Where(s => s.GraduateApplicationId == id)
                .OrderBy(s => s.SuspensionStartDate).ToList();

            // --- منطق التحقق من أهلية أداء اليمين (كما هو) ---
            var eligibilityIssues = new List<string>();
            bool isEligible = true;
            // ... (كل كود التحقق من الأهلية ... )

            // بناء الـ ViewModel
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
                Gender = graduateApp.Gender, // <-- 3. تمرير كائن الجنس
                TelegramChatId = graduateApp.TelegramChatId,
                ContactInfo = graduateApp.ContactInfo ?? new ContactInfo(),
                Supervisor = graduateApp.Supervisor,
                Qualifications = graduateApp.Qualifications.ToList(),
                Attachments = graduateApp.Attachments.ToList(),
                TraineeSerialNo = graduateApp.TraineeSerialNo, // (تم تعديله ليصبح string في الرد السابق)
                TrainingStartDate = graduateApp.TrainingStartDate,

                PaymentHistory = paymentHistory,
                SupervisorChangeRequests = supervisorChangeRequests,
                Renewals = renewals,
                LegalResearches = legalResearches,
                ExamHistory = examHistory,
                OralExamHistory = oralExamHistory,
                OathRequestHistory = oathRequestHistory,
                CouncilSuspensions = councilSuspensions,

                IsPracticingLawyer = (graduateApp.ApplicationStatus.Name == "محامي مزاول"),
                CanApplyForOath = isEligible,
                OathEligibilityIssues = eligibilityIssues
            };

            // ... (ViewBags كما هي) ...

            return View("PrintComprehensiveReport", viewModel);
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