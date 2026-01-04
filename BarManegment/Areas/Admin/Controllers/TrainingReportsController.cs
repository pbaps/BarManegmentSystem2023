using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Services;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class TrainingReportsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. صفحة البحث عن المتدربين
        public ActionResult Index(string searchTerm)
        {
            var query = db.GraduateApplications.AsNoTracking()
                .Include(t => t.ApplicationStatus)
                // نبحث في المتدربين المقيدين والموقوفين والمحامين المزاولين (لأرشفة تدريبهم)
                .Where(t => t.ApplicationStatus.Name == "متدرب مقيد" ||
                            t.ApplicationStatus.Name == "متدرب موقوف" ||
                            t.ApplicationStatus.Name == "محامي مزاول");

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a =>
                    a.ArabicName.Contains(searchTerm) ||
                    a.NationalIdNumber.Contains(searchTerm) ||
                    a.TraineeSerialNo.Contains(searchTerm)
                );
            }

            var trainees = query.OrderByDescending(a => a.TraineeSerialNo).Take(50).ToList();

            ViewBag.SearchTerm = searchTerm;
            return View(trainees);
        }

        // 2. عرض التقرير التفصيلي للمتدرب
        public ActionResult TraineeReport(int id)
        {
            var trainee = db.GraduateApplications.Find(id);
            if (trainee == null) return HttpNotFound();

            // جلب سجلات الحضور (فقط "حاضر")
            var attendance = db.TraineeAttendances.AsNoTracking()
                .Include(a => a.Session.TrainingCourse)
                .Where(a => a.TraineeId == id && a.Status == "حاضر")
                .OrderByDescending(a => a.Session.SessionDate)
                .ToList();

            var viewModel = new TraineeAttendanceReportViewModel
            {
                TraineeId = trainee.Id,
                TraineeName = trainee.ArabicName,
                TraineeSerialNo = trainee.TraineeSerialNo,
                NationalIdNumber = trainee.NationalIdNumber,
                AttendedSessions = attendance.Select(a => new AttendedSessionViewModel
                {
                    CourseName = a.Session.TrainingCourse.CourseName,
                    SessionTitle = a.Session.SessionTitle,
                    SessionDate = a.Session.SessionDate,
                    InstructorName = a.Session.InstructorName,
                    CreditHours = a.Session.CreditHours, // (double) متوافق الآن
                    AttendanceStatus = a.Status
                }).ToList()
            };

            viewModel.TotalSessions = viewModel.AttendedSessions.Count;

            // الجمع الآن سيعمل بشكل صحيح لأن كلا الطرفين double
            viewModel.TotalCreditHours = viewModel.AttendedSessions.Sum(s => s.CreditHours);

            AuditService.LogAction("View Training Report", "TrainingReports", $"Viewed report for trainee {trainee.ArabicName}");

            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}