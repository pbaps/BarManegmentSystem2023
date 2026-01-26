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
            // 1. التحقق من الجلسة الأساسية
            if (Session["TraineeName"] == null)
            {
                // محاولة استعادة الجلسة إذا كان EnrollmentId موجوداً
                if (Session["EnrollmentId"] != null)
                {
                    if (!RestoreSession((int)Session["EnrollmentId"]))
                    {
                        return RedirectToAction("Index", "ExamLogin");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "ExamLogin");
                }
            }

            // 2. إعداد الموديل
            var viewModel = new ExamDashboardViewModel
            {
                TraineeName = Session["TraineeName"].ToString(),
                ActiveExams = new List<EnrolledExamViewModel>(),
                FinishedExams = new List<EnrolledExamViewModel>()
            };

            var applicantType = Session["ApplicantType"] as string; // "Member" or "ExternalApplicant"
            var applicantId = (int)Session["ApplicantId"];
            var now = DateTime.Now;

            // 3. جلب جميع الامتحانات (مع ExamType لتجنب Null Reference)
            var allEnrollments = db.ExamEnrollments
                .Include(e => e.Exam)
                .Include(e => e.Exam.ExamType) // ضروري جداً
                .Where(e => applicantType == "Member" ? e.GraduateApplicationId == applicantId : e.ExamApplicationId == applicantId)
                .OrderByDescending(e => e.Exam.StartTime)
                .ToList();

            // 4. التصنيف (فعال / منتهي)
            foreach (var enrollment in allEnrollments)
            {
                var examVM = new EnrolledExamViewModel
                {
                    Id = enrollment.Id,
                    Exam = enrollment.Exam,
                    Result = enrollment.Result,
                    Score = enrollment.Score
                };

                // المنطق: الامتحان منتهي إذا انتهى وقته أو تم رصد نتيجة
                if (enrollment.Exam.EndTime < now || !string.IsNullOrEmpty(enrollment.Result))
                {
                    viewModel.FinishedExams.Add(examVM);
                }
                else if (enrollment.Exam.IsActive) // الامتحان فعال ومستقبلي
                {
                    viewModel.ActiveExams.Add(examVM);
                }
            }

            return View(viewModel);
        }

        // دالة مساعدة لاستعادة الجلسة في حال ضياعها (لتحسين تجربة المستخدم)
        private bool RestoreSession(int enrollmentId)
        {
            var enrollment = db.ExamEnrollments
                .Include(e => e.GraduateApplication)
                .Include(e => e.ExamApplication)
                .FirstOrDefault(e => e.Id == enrollmentId);

            if (enrollment == null) return false;

            if (enrollment.GraduateApplicationId.HasValue)
            {
                Session["ApplicantId"] = enrollment.GraduateApplicationId;
                Session["ApplicantType"] = "Member";
                Session["TraineeName"] = enrollment.GraduateApplication.ArabicName;
            }
            else if (enrollment.ExamApplicationId.HasValue)
            {
                Session["ApplicantId"] = enrollment.ExamApplicationId;
                Session["ApplicantType"] = "ExternalApplicant";
                Session["TraineeName"] = enrollment.ExamApplication.FullName;
            }
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}