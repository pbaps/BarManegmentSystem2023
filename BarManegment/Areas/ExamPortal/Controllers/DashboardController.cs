using BarManegment.Models;
using BarManegment.Areas.ExamPortal.ViewModels;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Collections.Generic;
using System;

namespace BarManegment.Areas.ExamPortal.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            // 1. التحقق من الجلسة
            if (Session["TraineeName"] == null)
                return RedirectToAction("Index", "ExamLogin");

            var viewModel = new ExamDashboardViewModel
            {
                TraineeName = Session["TraineeName"].ToString()
            };

            // استعادة هوية المتقدم من الجلسة أو إعادة بنائها
            var applicantType = Session["ApplicantType"] as string;
            var applicantId = (int?)Session["ApplicantId"];

            if (applicantId == null)
            {
                // محاولة الاستعادة باستخدام EnrollmentId القديم إذا وجد
                if (Session["EnrollmentId"] != null)
                {
                    int enrollmentId = (int)Session["EnrollmentId"];
                    var enrollment = db.ExamEnrollments.Find(enrollmentId);
                    if (enrollment != null)
                    {
                        applicantId = enrollment.ExamApplicationId ?? enrollment.GraduateApplicationId;
                        applicantType = enrollment.ExamApplicationId.HasValue ? "ExamApplicant" : "Graduate";
                        Session["ApplicantId"] = applicantId;
                        Session["ApplicantType"] = applicantType;
                    }
                    else
                    {
                        return RedirectToAction("Index", "ExamLogin");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "ExamLogin");
                }
            }

            // 2. جلب الامتحانات
            List<ExamEnrollment> allEnrollments;
            var now = DateTime.Now;

            if (applicantType == "Graduate")
            {
                allEnrollments = db.ExamEnrollments
                    .Include(e => e.Exam)
                    .Where(e => e.GraduateApplicationId == applicantId.Value)
                    .OrderByDescending(e => e.Exam.StartTime)
                    .ToList();
            }
            else // ExamApplicant
            {
                allEnrollments = db.ExamEnrollments
                    .Include(e => e.Exam)
                    .Where(e => e.ExamApplicationId == applicantId.Value)
                    .OrderByDescending(e => e.Exam.StartTime)
                    .ToList();
            }

            // 3. التصنيف
            viewModel.ActiveExams = allEnrollments
                .Where(e => e.Exam.IsActive &&
                            e.Exam.EndTime > now &&
                            string.IsNullOrEmpty(e.Result))
                .ToList();

            viewModel.FinishedExams = allEnrollments
                .Where(e => !e.Exam.IsActive ||
                            e.Exam.EndTime <= now ||
                            !string.IsNullOrEmpty(e.Result))
                .ToList();

            return View(viewModel);
        }

        // بدء الامتحان (توجيه)
        public ActionResult StartTraineeExam(int? examId)
        {
            // هنا نفترض أن examId هو EnrollmentId في سياق هذا الداشبورد
            // ولكن الداشبورد يمرر enrollment.Id
            return RedirectToAction("StartExam", "TakeExam", new { enrollmentId = examId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}