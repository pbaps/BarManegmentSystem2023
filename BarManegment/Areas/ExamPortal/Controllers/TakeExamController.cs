using BarManegment.Models;
using BarManegment.Areas.ExamPortal.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BarManegment.Services;

namespace BarManegment.Areas.ExamPortal.Controllers
{
    public class TakeExamController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult StartExam(int? enrollmentId)
        {
            if (enrollmentId == null)
            {
                if (Session["EnrollmentId"] == null) return RedirectToAction("Index", "ExamLogin");
                enrollmentId = (int)Session["EnrollmentId"];
            }

            var enrollment = db.ExamEnrollments.Include(e => e.Exam).FirstOrDefault(e => e.Id == enrollmentId);

            // التحقق الأمني: هل هذا الامتحان يخص المستخدم الحالي؟
            var applicantId = (int?)Session["ApplicantId"];
            if (applicantId.HasValue)
            {
                if (enrollment.ExamApplicationId != applicantId && enrollment.GraduateApplicationId != applicantId)
                {                    // 💡 تسجيل محاولة وصول غير مصرح بها
                    AuditService.LogAction("Unauthorized Access", "TakeExam", $"Applicant ID {applicantId} tried to access Enrollment ID {enrollmentId}");
                    TempData["ErrorMessage"] = "وصول غير مصرح به.";
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            // التحقق من الحالة
            if (enrollment == null || !string.IsNullOrEmpty(enrollment.Result))
            {
                return RedirectToAction("Result", new { enrollmentId = enrollmentId });
            }

            if (!enrollment.Exam.IsActive || DateTime.Now < enrollment.Exam.StartTime || DateTime.Now > enrollment.Exam.EndTime)
            {
                TempData["ErrorMessage"] = "الامتحان غير متاح حالياً (إما لم يبدأ أو انتهى وقته).";
                return RedirectToAction("Index", "Dashboard");
            }

            // إعداد الجلسة للامتحان
            Session["EnrollmentId"] = enrollment.Id;
            Session["ExamEndTime"] = enrollment.Exam.EndTime;
            // 💡 تسجيل بدء الامتحان
            AuditService.LogAction("Start Exam", "TakeExam", $"Started Exam: {enrollment.Exam.Title} (Enrollment ID: {enrollment.Id})");
            return RedirectToAction("Question", new { q = 1 });
        }

        public ActionResult Question(int q = 1)
        {
            if (Session["EnrollmentId"] == null || Session["ExamEndTime"] == null)
                return RedirectToAction("Index", "ExamLogin");

            int enrollmentId = (int)Session["EnrollmentId"];
            var enrollment = db.ExamEnrollments
                .Include(e => e.Exam.Questions)
                .FirstOrDefault(e => e.Id == enrollmentId);

            if (enrollment == null || !string.IsNullOrEmpty(enrollment.Result))
            {
                return RedirectToAction("Result", new { enrollmentId = enrollmentId });
            }

            var questions = enrollment.Exam.Questions.OrderBy(qu => qu.Id).ToList();

            if (q < 1 || q > questions.Count)
            {
                return RedirectToAction("SubmitExam");
            }

            var currentQuestion = questions[q - 1];

            var viewModel = new TakeExamViewModel
            {
                EnrollmentId = enrollmentId,
                ExamTitle = enrollment.Exam.Title,
                TotalQuestions = questions.Count,
                CurrentQuestionIndex = q,
                CurrentQuestion = db.Questions
                    .Include(qu => qu.Answers)
                    .Include(qu => qu.QuestionType)
                    .FirstOrDefault(qu => qu.Id == currentQuestion.Id),
                SavedAnswer = db.TraineeAnswers
                    .FirstOrDefault(a => a.ExamEnrollmentId == enrollmentId && a.QuestionId == currentQuestion.Id),
                EndTime = (DateTime)Session["ExamEndTime"]
            };

            return View(viewModel);
        }

        [HttpPost]
        public JsonResult SaveAnswer(int enrollmentId, int questionId, int? selectedAnswerId, string essayAnswerText)
        {
            // تحقق من أن الامتحان لم ينته بعد
            // (اختياري: يمكن إضافة تحقق من الوقت هنا)

            var answer = db.TraineeAnswers
                .FirstOrDefault(a => a.ExamEnrollmentId == enrollmentId && a.QuestionId == questionId);

            if (answer == null)
            {
                answer = new TraineeAnswer
                {
                    ExamEnrollmentId = enrollmentId,
                    QuestionId = questionId
                };
                db.TraineeAnswers.Add(answer);
            }

            answer.SelectedAnswerId = selectedAnswerId;
            answer.EssayAnswerText = essayAnswerText;
            db.SaveChanges();

            return Json(new { success = true });
        }

        public ActionResult SubmitExam()
        {
            if (Session["EnrollmentId"] == null) return RedirectToAction("Index", "ExamLogin");
            int enrollmentId = (int)Session["EnrollmentId"];

            var enrollment = db.ExamEnrollments.Include(e => e.Exam).FirstOrDefault(e => e.Id == enrollmentId);
            if (enrollment == null) return HttpNotFound();

            // حساب النتيجة
            var traineeAnswers = db.TraineeAnswers
                .Include(a => a.Question.QuestionType)
                .Include(a => a.SelectedAnswer)
                .Where(a => a.ExamEnrollmentId == enrollmentId).ToList();

            double totalScore = 0;
            bool hasEssay = false;

            foreach (var answer in traineeAnswers)
            {
                if (answer.Question.QuestionType.Name == "مقالي")
                {
                    hasEssay = true;
                }
                else
                {
                    // تصحيح تلقائي
                    if (answer.SelectedAnswer != null && answer.SelectedAnswer.IsCorrect)
                    {
                        totalScore += answer.Question.Points;
                    }
                }
            }

            enrollment.Score = totalScore;

            if (hasEssay)
            {
                enrollment.Result = "بانتظار التصحيح اليدوي";
            }
            else
            {
                // حساب النسبة المئوية والنجاح
                // ملاحظة: يجب التأكد من مجموع درجات الامتحان الكلي لحساب النسبة بدقة
                double totalPossibleScore = db.Questions.Where(q => q.ExamId == enrollment.ExamId).Sum(q => (double?)q.Points) ?? 0;

                double percentage = (totalPossibleScore > 0) ? (totalScore / totalPossibleScore) * 100 : 0;
                enrollment.Result = (percentage >= enrollment.Exam.PassingPercentage) ? "ناجح" : "راسب";
            }

            db.SaveChanges();
            Session.Remove("ExamEndTime");

            return RedirectToAction("Result", new { enrollmentId = enrollmentId });
        }

        public ActionResult Result(int? enrollmentId)
        {
            if (enrollmentId == null)
            {
                if (Session["EnrollmentId"] == null) return RedirectToAction("Index", "ExamLogin");
                enrollmentId = (int)Session["EnrollmentId"];
            }

            var enrollment = db.ExamEnrollments.Include(e => e.Exam.ExamType).FirstOrDefault(e => e.Id == enrollmentId);
            if (enrollment == null) return HttpNotFound();

            var applicantId = (int?)Session["ApplicantId"];
            if (applicantId.HasValue && enrollment.ExamApplicationId != applicantId && enrollment.GraduateApplicationId != applicantId)
            {
                // حماية بسيطة لمنع عرض نتائج الآخرين
                return RedirectToAction("Index", "Dashboard");
            }

            double totalPossibleScore = db.Questions.Where(q => q.ExamId == enrollment.ExamId).Sum(q => (double?)q.Points) ?? 0;
            ViewBag.TotalPossibleScore = totalPossibleScore;

            return View(enrollment);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}