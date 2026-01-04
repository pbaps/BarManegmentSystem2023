using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Threading.Tasks;
using BarManegment.Services;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class ExamEnrollmentsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/ExamEnrollments/Index
 

        [CustomAuthorize(Permission = "CanEdit")]
        // (1. إضافة باراميتر البحث)
        public ActionResult Index(int? examId, string searchTerm)
        {
            if (examId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var exam = db.Exams.Include(e => e.ExamType).FirstOrDefault(e => e.Id == examId);
            if (exam == null) return HttpNotFound();

            var viewModel = new ExamEnrollmentViewModel
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                ExamTypeName = exam.ExamType.Name
            };

            var alreadyEnrolled = db.ExamEnrollments.Where(en => en.ExamId == examId).ToList();
            var alreadyEnrolledIds = alreadyEnrolled.Select(e => e.GraduateApplicationId).ToHashSet();

            // (إضافة رسالة العرض في حالة عدم وجود نتائج)
            string noResultsMessage = "لا يوجد متقدمون مؤهلون لهذا الامتحان حاليًا.";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                noResultsMessage = $"لا توجد نتائج مطابقة للبحث: '{searchTerm}'";
            }
            ViewBag.NoResultsMessage = noResultsMessage;
            ViewBag.SearchTerm = searchTerm; // (إعادة مصطلح البحث للواجهة)


            if (exam.ExamType.Name == "امتحان قبول")
            {
                // (2. بناء الاستعلام أولاً)
                var query = db.ExamApplications
                                .Where(a => a.Status == "مقبول للامتحان");

                // (3. تطبيق الفلترة)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(a => a.FullName.Contains(searchTerm) ||
                                             a.NationalIdNumber.Contains(searchTerm));
                }

                // (4. تنفيذ الاستعلام)
                viewModel.Candidates = query.ToList()
                    .Select(a => new CandidateViewModel
                    {
                        ApplicantId = a.Id,
                        Name = a.FullName,
                        Identifier = a.NationalIdNumber,
                        IsEnrolled = alreadyEnrolled.Any(en => en.ExamApplicationId == a.Id)
                    }).ToList();
            }
            else if (exam.ExamType.Name == "امتحان إنهاء تدريب")
            {
                const double requiredTrainingDays = 548;
                var today = DateTime.Now.Date;

                // (2. بناء الاستعلام أولاً)
                var potentialTraineesQuery = db.GraduateApplications
                    .Include(a => a.ApplicationStatus)
                    .Where(a => a.ApplicationStatus.Name == "متدرب مقيد" &&
                                a.TrainingStartDate.HasValue);

                // (3. تطبيق الفلترة)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    potentialTraineesQuery = potentialTraineesQuery.Where(a => a.ArabicName.Contains(searchTerm) ||
                                                                           a.TraineeSerialNo.Contains(searchTerm) ||
                                                                           a.NationalIdNumber.Contains(searchTerm));
                }

                // (4. تنفيذ الاستعلام)
                var potentialTrainees = potentialTraineesQuery.ToList();

                // ================================================
                // ===            💡 بداية التحسين              ===
                // ================================================

                // 1. جلب IDs المتدربين الذين تم فلترتهم
                var traineeIds = potentialTrainees.Select(t => t.Id).ToList();

                // 2. سحب "كل" الطلبات المتعلقة بهم في استعلام واحد (خارج اللوب)
                var allRequests = db.SupervisorChangeRequests
                    .Where(r => traineeIds.Contains(r.TraineeId) && // <--- الفلترة هنا
                                (r.RequestType == "وقف" || r.RequestType == "استكمال") &&
                                r.Status == "موافق عليه" &&
                                r.DecisionDate.HasValue)
                    .OrderBy(r => r.DecisionDate)
                    .ToList();

                // 3. تحويل الطلبات إلى مجموعات (Groups) لسهولة الوصول إليها
                var requestsByTrainee = allRequests.GroupBy(r => r.TraineeId);

                // ================================================
                // ===             نهاية التحسين               ===
                // ================================================

                var eligibleCandidates = new List<CandidateViewModel>();

                foreach (var trainee in potentialTrainees)
                {
                    // 4. جلب طلبات هذا المتدرب من الذاكرة (سريع جداً)
                    var requests = requestsByTrainee
                        .FirstOrDefault(g => g.Key == trainee.Id)?
                        .ToList() ?? new List<SupervisorChangeRequest>(); // قائمة فارغة إذا لم يوجد له طلبات

                    // 🛑 تم حذف الكود القديم الذي كان هنا (الذي يستدعي قاعدة البيانات) 🛑

                    DateTime startDate = trainee.TrainingStartDate.Value.Date;
                    double totalSuspensionDays = 0;
                    DateTime? lastStopDate = null; // <-- تم إلغاء تعليق هذا السطر لاستخدامه في اللوب

                    // --- بداية لوب حساب أيام الوقف ---
                    // (هذا اللوب يجب أن يكون موجوداً، أنت حذفته في المثال السابق)
                    foreach (var request in requests)
                    {
                        if (request.RequestType == "وقف" && request.DecisionDate.HasValue)
                        {
                            lastStopDate = request.DecisionDate.Value.Date;
                        }
                        else if (request.RequestType == "استكمال" && request.DecisionDate.HasValue && lastStopDate.HasValue)
                        {
                            totalSuspensionDays += (request.DecisionDate.Value.Date - lastStopDate.Value).TotalDays;
                            lastStopDate = null; // إعادة تعيين تاريخ الوقف
                        }
                    }
                    // إذا كان آخر طلب هو "وقف" وما زال موقوفاً
                    if (lastStopDate.HasValue)
                    {
                        totalSuspensionDays += (today - lastStopDate.Value).TotalDays;
                    }
                    // --- نهاية لوب حساب أيام الوقف ---

                    double totalDaysElapsed = (today - startDate).TotalDays;
                    double netTrainingDays = totalDaysElapsed - totalSuspensionDays;

                    if (netTrainingDays >= requiredTrainingDays)
                    {
                        eligibleCandidates.Add(new CandidateViewModel
                        {
                            ApplicantId = trainee.Id,
                            Name = $"{trainee.ArabicName} (صافي تدريب: {Math.Floor(netTrainingDays / 30.44)} أشهر)",
                            Identifier = trainee.TraineeSerialNo ?? trainee.Id.ToString(),
                            IsEnrolled = alreadyEnrolledIds.Contains(trainee.Id)
                        });
                    }
                }
                viewModel.Candidates = eligibleCandidates.OrderBy(c => c.Name).ToList();
            }
            else if (exam.ExamType.Name == "اختبار وظيفي")
            {
                // (2. بناء الاستعلام أولاً)
                var query = db.GraduateApplications
                                .Where(a => a.ApplicationStatus.Name == "محامي مزاول");

                // (3. تطبيق الفلترة)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(a => a.ArabicName.Contains(searchTerm) ||
                                             a.Id.ToString().Contains(searchTerm));
                }

                // (4. تنفيذ الاستعلام)
                viewModel.Candidates = query.ToList()
                    .Select(a => new CandidateViewModel
                    {
                        ApplicantId = a.Id,
                        Name = a.ArabicName,
                        Identifier = a.Id.ToString(),
                        IsEnrolled = alreadyEnrolled.Any(en => en.GraduateApplicationId == a.Id)
                    }).ToList();
            }

            return View(viewModel);
        }

        // POST: Admin/ExamEnrollments/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Index(ExamEnrollmentViewModel viewModel)
        {
            var exam = db.Exams.Include(e => e.ExamType).FirstOrDefault(e => e.Id == viewModel.ExamId);
            if (exam == null) return HttpNotFound();

            var existingEnrollments = db.ExamEnrollments.Where(e => e.ExamId == viewModel.ExamId).ToList();
            int addedCount = 0;
            int removedCount = 0;
            foreach (var candidate in viewModel.Candidates)
            {
                var isAlreadyEnrolled = existingEnrollments.Any(e =>
                    (exam.ExamType.Name == "امتحان قبول" && e.ExamApplicationId == candidate.ApplicantId) ||
                    (exam.ExamType.Name != "امتحان قبول" && e.GraduateApplicationId == candidate.ApplicantId)
                );

                if (candidate.IsEnrolled && !isAlreadyEnrolled)
                {
                    var enrollment = new ExamEnrollment { ExamId = viewModel.ExamId };
                    if (exam.ExamType.Name == "امتحان قبول")
                        enrollment.ExamApplicationId = candidate.ApplicantId;
                    else
                        enrollment.GraduateApplicationId = candidate.ApplicantId;

                    db.ExamEnrollments.Add(enrollment);
                }
                else if (!candidate.IsEnrolled && isAlreadyEnrolled)
                {
                    var enrollmentToRemove = existingEnrollments.FirstOrDefault(e =>
                        (exam.ExamType.Name == "امتحان قبول" && e.ExamApplicationId == candidate.ApplicantId) ||
                        (exam.ExamType.Name != "امتحان قبول" && e.GraduateApplicationId == candidate.ApplicantId)
                    );
                    if (enrollmentToRemove != null)
                        db.ExamEnrollments.Remove(enrollmentToRemove);
                }
            }

            db.SaveChanges();
            // ✅ Audit
            if (addedCount > 0 || removedCount > 0)
            {
                AuditService.LogAction("Batch Enrollment Update", "ExamEnrollments", $"Exam ID {viewModel.ExamId}: Added {addedCount}, Removed {removedCount}.");
            }
            TempData["SuccessMessage"] = "تم حفظ تسجيلات المتقدمين بنجاح.";
            return RedirectToAction("Index", "Exams");
        }

        // GET: Admin/ExamEnrollments/ManageResults
        // GET: Admin/ExamEnrollments/ManageResults/5

        // GET: Admin/ExamEnrollments/ManageResults/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ManageResults(int? examId)
        {
            if (examId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var exam = db.Exams
                .Include(e => e.ExamType)
                .Include(e => e.Enrollments.Select(en => en.ExamApplication))
                .Include(e => e.Enrollments.Select(en => en.GraduateApplication.ContactInfo))
                .FirstOrDefault(e => e.Id == examId);

            if (exam == null) return HttpNotFound();

            // === 1. حساب مجموع الدرجات من جدول الأسئلة ===
            // إذا لم تكن هناك أسئلة، نفترض الافتراضي 100 لتجنب القسمة على صفر
            double totalScore = db.Questions
                                  .Where(q => q.ExamId == exam.Id)
                                  .Sum(q => (double?)q.Points) ?? 0;

            if (totalScore == 0) totalScore = 100;

            ViewBag.PassingPercentage = exam.PassingPercentage;

            var viewModel = new ExamResultsViewModel
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                ExamDate = exam.StartTime,
                TotalPossibleScore = totalScore, // تمرير المجموع المحسوب
                Candidates = exam.Enrollments.Select(e => new EnrolledCandidateResultViewModel
                {
                    EnrollmentId = e.Id,
                    ApplicantName = e.ExamApplicationId != null ? e.ExamApplication.FullName :
                                    (e.GraduateApplication != null ? e.GraduateApplication.ArabicName : "غير معروف"),

                    ApplicantIdentifier = e.ExamApplicationId != null ? e.ExamApplication.NationalIdNumber :
                                          (e.GraduateApplication != null ? e.GraduateApplication.NationalIdNumber : "-"),

                    Score = e.Score,
                    Result = e.Result,

                    ContactEmail = e.ExamApplicationId != null ? e.ExamApplication.Email : e.GraduateApplication.ContactInfo?.Email,
                    ContactMobile = e.ExamApplicationId != null ? e.ExamApplication.MobileNumber : e.GraduateApplication.ContactInfo?.MobileNumber,
                    TelegramChatId = e.ExamApplicationId != null ? e.ExamApplication.TelegramChatId : e.GraduateApplication.TelegramChatId
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Admin/ExamEnrollments/ManageResults
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public async Task<ActionResult> ManageResults(ExamResultsViewModel viewModel, string command)
        {
            var selectedCandidates = viewModel.Candidates?.Where(c => c.IsSelected).ToList() ?? new List<EnrolledCandidateResultViewModel>();

            if (command == "SaveGrades")
            {
                var exam = await db.Exams.Include(e => e.ExamType).FirstOrDefaultAsync(e => e.Id == viewModel.ExamId);
                if (exam == null) return HttpNotFound();

                int updatedCount = 0;
                int passedCount = 0;

                // إعادة حساب الدرجة العظمى للتأكد (أمان)
                double maxScore = db.Questions.Where(q => q.ExamId == exam.Id).Sum(q => (double?)q.Points) ?? 0;
                if (maxScore == 0) maxScore = viewModel.TotalPossibleScore > 0 ? viewModel.TotalPossibleScore : 100;

                double passingPercent = exam.PassingPercentage > 0 ? exam.PassingPercentage : 50;

                foreach (var candidate in viewModel.Candidates)
                {
                    var enrollmentInDb = await db.ExamEnrollments
                        .Include(e => e.ExamApplication)
                        .Include(e => e.GraduateApplication)
                        .FirstOrDefaultAsync(e => e.Id == candidate.EnrollmentId);

                    if (enrollmentInDb != null)
                    {
                        // تحديث الدرجة
                        enrollmentInDb.Score = candidate.Score;

                        if (candidate.Score.HasValue)
                        {
                            // === معادلة النسبة المئوية ===
                            // (الدرجة الحاصل عليها / المجموع الكلي) * 100
                            double studentPercentage = (candidate.Score.Value / maxScore) * 100;

                            // المقارنة
                            bool isPassed = studentPercentage >= (passingPercent - 0.01);

                            string resultText = isPassed ? "ناجح" : "راسب";
                            enrollmentInDb.Result = resultText;

                            if (isPassed) passedCount++;

                            // === عكس النتيجة على ملف المتقدم الأصلي ===
                            if (enrollmentInDb.ExamApplication != null)
                            {
                                enrollmentInDb.ExamApplication.ExamScore = candidate.Score;
                                enrollmentInDb.ExamApplication.ExamResult = resultText;

                                if (isPassed)
                                {
                                    enrollmentInDb.ExamApplication.Status = "ناجح (بانتظار استكمال النواقص)";
                                }
                                else
                                {
                                    // === 🔴 إصلاح مشكلة عدم التحديث عند الرسوب 🔴 ===
                                    // التأكد من تعيين الحالة إلى "راسب" صراحةً
                                    enrollmentInDb.ExamApplication.Status = "راسب";
                                }
                            }

                            updatedCount++;
                        }
                        else
                        {
                            // إذا تم مسح الدرجة، نعيد الحالة للافتراضي
                            enrollmentInDb.Result = null;
                            if (enrollmentInDb.ExamApplication != null)
                            {
                                enrollmentInDb.ExamApplication.ExamScore = null;
                                enrollmentInDb.ExamApplication.ExamResult = null;
                                enrollmentInDb.ExamApplication.Status = "مقبول للامتحان";
                            }
                        }
                    }
                }

                await db.SaveChangesAsync();

                AuditService.LogAction("Save Grades", "ExamEnrollments",
                    $"Exam ID {viewModel.ExamId}: Updated {updatedCount} records. Max Score: {maxScore}. Passed: {passedCount}.");

                TempData["SuccessMessage"] = $"تم حفظ النتائج. (الناجحون: {passedCount} - الراسبون: {updatedCount - passedCount})";
            }

            // ... (SendResultNotifications, SendTelegram, DeleteSelected remain the same) ...
            // ب. إرسال إشعارات البريد الإلكتروني (معدل: يجلب البيانات من القاعدة)
            // ---------------------------------------------------------
            else if (command == "SendResultNotifications")
            {
                if (!selectedCandidates.Any())
                {
                    TempData["ErrorMessage"] = "الرجاء تحديد متقدم واحد على الأقل.";
                    return RedirectToAction("ManageResults", new { examId = viewModel.ExamId });
                }

                // 1. جلب IDs للمحددين
                var selectedIds = selectedCandidates.Select(c => c.EnrollmentId).ToList();

                // 2. جلب البيانات الحقيقية من قاعدة البيانات (لضمان وجود النتيجة والايميل)
                var enrollmentsToSend = await db.ExamEnrollments
                    .Include(e => e.ExamApplication)
                    .Include(e => e.GraduateApplication.ContactInfo)
                    .Include(e => e.GraduateApplication.User)
                    .Where(e => selectedIds.Contains(e.Id))
                    .ToListAsync();

                int successCount = 0;

                foreach (var enrollment in enrollmentsToSend)
                {
                    // تحديد الايميل والاسم
                    string email = enrollment.ExamApplication != null ? enrollment.ExamApplication.Email :
                                   (enrollment.GraduateApplication?.ContactInfo?.Email ?? enrollment.GraduateApplication?.User?.Email);

                    string name = enrollment.ExamApplication != null ? enrollment.ExamApplication.FullName :
                                  enrollment.GraduateApplication?.ArabicName;

                    string result = enrollment.Result;
                    double? score = enrollment.Score;

                    // شرط الإرسال: وجود ايميل + وجود نتيجة مرصودة
                    if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(result))
                    {
                        string subject = $"نتيجة امتحان: {viewModel.ExamTitle}";
                        string statusColor = result == "ناجح" ? "green" : "red";
                        string body = $@"
                            <div style='font-family: Arial, sans-serif; direction: rtl; text-align: right; padding: 20px; border: 1px solid #ddd;'>
                                <h3>السيد/ة {name} المحترم/ة،</h3>
                                <p>نود إعلامكم بنتيجة الامتحان الذي تقدمتم له في نقابة المحامين:</p>
                                <hr/>
                                <p><strong>الامتحان:</strong> {viewModel.ExamTitle}</p>
                                <p><strong>التاريخ:</strong> {viewModel.ExamDate:yyyy-MM-dd}</p>
                                <p><strong>الدرجة:</strong> {score}</p>
                                <p><strong>النتيجة النهائية:</strong> <span style='color:{statusColor}; font-size: 1.2em; font-weight:bold;'>{result}</span></p>
                                <hr/>
                                <p>مع تحيات،<br/>لجنة التدريب - نقابة المحامين</p>
                            </div>";

                        try
                        {
                            await EmailService.SendEmailAsync(email, subject, body);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Email Error for {email}: {ex.Message}");
                        }
                    }
                }

                if (successCount > 0)
                    TempData["SuccessMessage"] = $"تم إرسال النتائج إلى {successCount} بريد إلكتروني بنجاح.";
                else
                    TempData["ErrorMessage"] = "لم يتم إرسال أي بريد. تأكد من رصد النتائج أولاً (حفظ الدرجات) وأن المتقدمين لديهم بريد إلكتروني.";
            }

            // ---------------------------------------------------------
            // ج. إرسال إشعارات تليجرام (معدل: يجلب البيانات من القاعدة)
            // ---------------------------------------------------------
            else if (command == "SendTelegram")
            {
                if (!selectedCandidates.Any())
                {
                    TempData["ErrorMessage"] = "الرجاء تحديد متقدم واحد على الأقل.";
                    return RedirectToAction("ManageResults", new { examId = viewModel.ExamId });
                }

                var selectedIds = selectedCandidates.Select(c => c.EnrollmentId).ToList();

                // جلب البيانات من القاعدة
                var enrollmentsToSend = await db.ExamEnrollments
                    .Include(e => e.ExamApplication)
                    .Include(e => e.GraduateApplication)
                    .Where(e => selectedIds.Contains(e.Id))
                    .ToListAsync();

                int successCount = 0;

                foreach (var enrollment in enrollmentsToSend)
                {
                    long? chatId = enrollment.ExamApplication != null ? enrollment.ExamApplication.TelegramChatId :
                                   enrollment.GraduateApplication?.TelegramChatId;

                    string name = enrollment.ExamApplication != null ? enrollment.ExamApplication.FullName :
                                  enrollment.GraduateApplication?.ArabicName;

                    string result = enrollment.Result;

                    if (chatId.HasValue && chatId > 0 && !string.IsNullOrEmpty(result))
                    {
                        string emoji = result == "ناجح" ? "✅" : "❌";
                        string msg = $"📢 *إشعار نتيجة امتحان*\n" +
                                     $"----------------------------\n" +
                                     $"📄 الامتحان: {viewModel.ExamTitle}\n" +
                                     $"👤 الاسم: {name}\n" +
                                     $"📅 التاريخ: {viewModel.ExamDate:yyyy-MM-dd}\n" +
                                     $"📊 الدرجة: *{enrollment.Score}*\n" +
                                     $"🔖 النتيجة: *{result}* {emoji}\n" +
                                     $"----------------------------\n" +
                                     $"نقابة المحامين الفلسطينيين";

                        try
                        {
                            await TelegramService.SendMessageAsync(chatId.Value, msg);
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Telegram Error: {ex.Message}");
                        }
                    }
                }

                if (successCount > 0)
                    TempData["SuccessMessage"] = $"تم إرسال النتائج إلى {successCount} حساب تليجرام.";
                else
                    TempData["InfoMessage"] = "لم يتم الإرسال. تأكد من رصد النتائج أولاً، وأن المستخدمين قد ربطوا حساباتهم بتليجرام.";
            }
            else if (command == "DeleteSelected")
            {
                if (selectedCandidates.Any())
                {
                    var ids = selectedCandidates.Select(c => c.EnrollmentId).ToList();
                    var records = await db.ExamEnrollments.Where(e => ids.Contains(e.Id)).ToListAsync();
                    db.ExamEnrollments.RemoveRange(records);
                    await db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم حذف السجلات المحددة.";
                }
            }

            return RedirectToAction("ManageResults", new { examId = viewModel.ExamId });
        }


        /// <summary>
        /// /////
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            ExamEnrollment enrollment = db.ExamEnrollments.Include(e => e.ExamApplication).Include(e => e.GraduateApplication).FirstOrDefault(e => e.Id == id);
            if (enrollment == null) return HttpNotFound();
            return View(enrollment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteConfirmed(int id)
        {
            ExamEnrollment enrollment = db.ExamEnrollments.Find(id);
            if (enrollment != null)
            {
                int examId = enrollment.ExamId;
                db.ExamEnrollments.Remove(enrollment);
                db.SaveChanges();
                AuditService.LogAction("Delete Enrollment", "ExamEnrollments", $"Deleted Enrollment ID {id} for Exam {examId}.");

                TempData["SuccessMessage"] = "تم حذف تسجيل المتقدم بنجاح.";
                return RedirectToAction("ManageResults", new { examId = examId });
            }
            return RedirectToAction("Index", "Exams");
        }

        // ==================================================================
        // 💡💡 إضافة يدوية لمتقدم (Manual Enrollment) 💡💡
        // ==================================================================

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(int? examId)
        {
            if (examId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var exam = db.Exams.Find(examId);
            if (exam == null) return HttpNotFound();

            ViewBag.ExamTitle = exam.Title;
            return View(new ManualEnrollmentViewModel { ExamId = exam.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(ManualEnrollmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                // البحث عن الشخص بالرقم الوطني
                var person = db.GraduateApplications.FirstOrDefault(g => g.NationalIdNumber == model.NationalIdNumber);
                if (person == null)
                {
                    // محاولة البحث في ExamApplications إذا لم يكن في GraduateApplications
                    var applicant = db.ExamApplications.FirstOrDefault(a => a.NationalIdNumber == model.NationalIdNumber);

                    if (applicant != null)
                    {
                        // تحقق من التكرار
                        if (db.ExamEnrollments.Any(e => e.ExamId == model.ExamId && e.ExamApplicationId == applicant.Id))
                        {
                            ModelState.AddModelError("", "هذا المتقدم مسجل بالفعل في هذا الامتحان.");
                            return View(model);
                        }

                        var enrollment = new ExamEnrollment { ExamId = model.ExamId, ExamApplicationId = applicant.Id };
                        db.ExamEnrollments.Add(enrollment);
                        AuditService.LogAction("Manual Enrollment", "ExamEnrollments", $"Manually enrolled Graduate ID {person.Id} ({person.ArabicName}) in Exam {model.ExamId}");

                    }
                    else
                    {
                        ModelState.AddModelError("", "لم يتم العثور على شخص بهذا الرقم الوطني في النظام.");
                        return View(model);
                    }
                }
                else
                {
                    // تحقق من التكرار
                    if (db.ExamEnrollments.Any(e => e.ExamId == model.ExamId && e.GraduateApplicationId == person.Id))
                    {
                        ModelState.AddModelError("", "هذا العضو مسجل بالفعل في هذا الامتحان.");
                        return View(model);
                    }

                    var enrollment = new ExamEnrollment { ExamId = model.ExamId, GraduateApplicationId = person.Id };
                    db.ExamEnrollments.Add(enrollment);
 
                    AuditService.LogAction("Manual Enrollment", "ExamEnrollments", $"Manually enrolled Graduate ID {person.Id} ({person.ArabicName}) in Exam {model.ExamId}");

                }

                db.SaveChanges();

                TempData["SuccessMessage"] = "تمت إضافة المتقدم للامتحان بنجاح.";
                return RedirectToAction("Index", new { examId = model.ExamId });
            }
            return View(model);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}

