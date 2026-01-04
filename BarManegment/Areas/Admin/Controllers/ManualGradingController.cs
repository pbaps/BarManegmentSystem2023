using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class ManualGradingController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ... (Index and Grade GET actions remain the same) ...
        public ActionResult Index()
        {
            var unassignedAnswers = db.TraineeAnswers
                .Include(a => a.Question.QuestionType)
                .Where(a => a.Question.QuestionType.Name == "مقالي" &&
                            !db.ManualGrades.Any(g => g.TraineeAnswerId == a.Id))
                .ToList();

            if (unassignedAnswers.Any())
            {
                var potentialGraderIds = db.Users
                    .Where(u => u.IsActive && (u.UserType.NameEnglish == "Administrator" ||
                                u.UserType.NameEnglish == "Employee" ||
                                u.UserType.NameEnglish == "Grader"))
                    .Select(u => u.Id)
                    .ToList();

                if (potentialGraderIds.Any())
                {
                    for (int i = 0; i < unassignedAnswers.Count; i++)
                    {
                        var graderId = potentialGraderIds[i % potentialGraderIds.Count];
                        var answerId = unassignedAnswers[i].Id;

                        db.ManualGrades.Add(new ManualGrade { TraineeAnswerId = answerId, GraderId = graderId, Status = "معين" });
                    }
                    db.SaveChanges();
                }
            }

            int currentUserId = (int)Session["UserId"];
            var gradingQueue = db.ManualGrades
                .Include(g => g.Grader)
                .Include(g => g.TraineeAnswer.Question.Exam)
                .Include(g => g.TraineeAnswer.ExamEnrollment.ExamApplication)
                .Include(g => g.TraineeAnswer.ExamEnrollment.GraduateApplication)
                .Where(g => g.GraderId == currentUserId && g.Status == "معين")
                .ToList()
                .Select(g => new ManualGradingIndexViewModel
                {
                    ManualGradeId = g.Id,
                    ExamTitle = g.TraineeAnswer.Question.Exam.Title,
                    ApplicantName = g.TraineeAnswer.ExamEnrollment.ExamApplication != null
                                    ? g.TraineeAnswer.ExamEnrollment.ExamApplication.FullName
                                    : g.TraineeAnswer.ExamEnrollment.GraduateApplication?.ArabicName,
                    GraderId = g.GraderId,
                    GraderName = g.Grader.FullNameArabic
                })
                .ToList();

            return View(gradingQueue);
        }

        public ActionResult Grade(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var assignment = db.ManualGrades.Include(g => g.TraineeAnswer.Question).FirstOrDefault(g => g.Id == id);
            if (assignment == null) return HttpNotFound();

            var viewModel = new GradeEssayViewModel
            {
                ManualGradeId = assignment.Id,
                QuestionText = assignment.TraineeAnswer.Question.QuestionText,
                EssayAnswer = assignment.TraineeAnswer.EssayAnswerText,
                QuestionPoints = assignment.TraineeAnswer.Question.Points
            };
            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Grade(GradeEssayViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var assignment = db.ManualGrades.Include(g => g.TraineeAnswer.ExamEnrollment).FirstOrDefault(g => g.Id == viewModel.ManualGradeId);
                if (assignment == null) return HttpNotFound();

                assignment.TraineeAnswer.Score = viewModel.Score;
                assignment.Status = "تم التصحيح";

                db.SaveChanges();

                int enrollmentId = assignment.TraineeAnswer.ExamEnrollmentId;
                var enrollment = db.ExamEnrollments.Include(e => e.Exam).FirstOrDefault(e => e.Id == enrollmentId);

                bool allManualGradingIsComplete = !db.ManualGrades.Any(g => g.TraineeAnswer.ExamEnrollmentId == enrollmentId && g.Status == "معين");

                if (allManualGradingIsComplete && enrollment != null)
                {
                    // === بداية التعديل: آلية احتساب الدرجة النهائية الصحيحة ===

                    // 1. جلب مجموع الدرجات التلقائية (الذي تم حسابه عند تسليم الامتحان)
                    double autoGradedScore = enrollment.Score ?? 0;

                    // 2. جلب جميع الإجابات المقالية التي تم تصحيحها
                    var manuallyGradedAnswers = db.TraineeAnswers
                        .Where(a => a.ExamEnrollmentId == enrollmentId && a.Question.QuestionType.Name == "مقالي")
                        .ToList();

                    // 3. جمع درجات الأسئلة المقالية
                    double manuallyGradedScore = manuallyGradedAnswers.Sum(a => a.Score ?? 0);

                    // 4. احتساب الدرجة النهائية
                    double finalScore = autoGradedScore + manuallyGradedScore;
                    enrollment.Score = finalScore;

                    // 5. احتساب النتيجة النهائية (ناجح/راسب)
                    double totalPossibleScore = db.Questions.Where(q => q.ExamId == enrollment.ExamId).Sum(q => q.Points);
                    double percentage = (totalPossibleScore > 0) ? (finalScore / totalPossibleScore) * 100 : 0;
                    enrollment.Result = (percentage >= enrollment.Exam.PassingPercentage) ? "ناجح" : "راسب";

                    // === نهاية التعديل ===

                    db.SaveChanges();
                }

                TempData["SuccessMessage"] = "تم حفظ الدرجة بنجاح.";
                return RedirectToAction("Index");
            }
            return View(viewModel);
        }
    }
}

