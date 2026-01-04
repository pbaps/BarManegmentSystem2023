using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize]
    public class HomeController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/Dashboard
        public ActionResult Index(int yearRange = 10)
        {
            var viewModel = new AdminDashboardViewModel();
            viewModel.SelectedYearRange = yearRange;

            // 1. تحديد هوية المستخدم (Super Admin)
            int currentUserId = (int)Session["UserId"];
            var currentUser = db.Users.Include(u => u.UserType).FirstOrDefault(u => u.Id == currentUserId);
            bool isSuperAdmin = (currentUser != null && currentUser.UserType.NameEnglish == "Administrator");
            ViewBag.IsSuperAdmin = isSuperAdmin;

            // =========================================================
            //  تعبئة البيانات (Data Population)
            // =========================================================

            // A. قسم القبول والتدريب
            if (PermissionHelper.HasPermission("GraduateApplications") || PermissionHelper.HasPermission("RegisteredTrainees"))
            {
                var statusDict = db.ApplicationStatuses.ToDictionary(s => s.Name, s => s.Id);

                int newStatusId = statusDict.ContainsKey("طلب جديد") ? statusDict["طلب جديد"] : 0;
                int activeTraineeId = statusDict.ContainsKey("متدرب مقيد") ? statusDict["متدرب مقيد"] : 0;
                int practicingId = statusDict.ContainsKey("محامي مزاول") ? statusDict["محامي مزاول"] : 0;
                int nonPracticingId = statusDict.ContainsKey("محامي غير مزاول") ? statusDict["محامي غير مزاول"] : 0;
                int pendingCommitteeId = statusDict.ContainsKey("بانتظار الموافقة النهائية") ? statusDict["بانتظار الموافقة النهائية"] : 0;

                viewModel.NewApplicationsCount = db.GraduateApplications.Count(a => a.ApplicationStatusId == newStatusId);
                viewModel.TotalApplicationsCount = db.GraduateApplications.Count();
                viewModel.ActiveTraineesCount = db.GraduateApplications.Count(a => a.ApplicationStatusId == activeTraineeId);
                viewModel.PracticingLawyersCount = db.GraduateApplications.Count(a => a.ApplicationStatusId == practicingId);
                viewModel.NonPracticingLawyersCount = db.GraduateApplications.Count(a => a.ApplicationStatusId == nonPracticingId);
                viewModel.PendingCommitteeApprovalCount = db.GraduateApplications.Count(a => a.ApplicationStatusId == pendingCommitteeId);
            }

            // B. قسم الامتحانات (جديد 💡)
            if (PermissionHelper.HasPermission("ExamApplications") || PermissionHelper.HasPermission("Exams"))
            {
                // طلبات القبول للامتحان
                viewModel.NewExamApplicationsCount = db.ExamApplications.Count(e => e.Status == "قيد المراجعة");

                // الامتحانات النشطة حالياً
                viewModel.OpenExamsCount = db.Exams.Count(e => e.IsActive && e.StartTime > DateTime.Now);

                // عدد المسجلين في الامتحانات القادمة (اختياري حسب الحاجة)
                // viewModel.RegisteredForExamCount = db.ExamResults.Count(r => r.Exam.IsActive);
            }

            // C. طلبات النقل واليمين
            if (PermissionHelper.HasPermission("SupervisorChangeRequests"))
            {
                viewModel.PendingSupervisorRequestsCount = db.SupervisorChangeRequests.Count(r => r.Status == "بانتظار موافقة اللجنة");
            }

            if (PermissionHelper.HasPermission("OathRequests"))
            {
                viewModel.PendingOathRequestsCount = db.OathRequests.Count(o => o.Status == "بانتظار موافقة لجنة اليمين");
            }

            // D. المالية
            if (PermissionHelper.HasPermission("PaymentVouchers"))
            {
                viewModel.UnpaidVouchersCount = db.PaymentVouchers.Count(v => v.Status == "صادر");
            }

            if (PermissionHelper.HasPermission("Receipts"))
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // حساب إجمالي السندات لليوم
                // ملاحظة: تأكد من اسم الحقل الذي يمثل القيمة المالية في جدول Receipts (هنا افترضت Amount)
                viewModel.TotalRevenueToday = db.Receipts
                    .Where(r => r.CreationDate >= today && r.CreationDate < tomorrow)
                   .Select(r => r.PaymentVoucher.TotalAmount)
                    .DefaultIfEmpty(0)
                    .Sum();
            }

            // E. سجل النظام (Mapping to AuditLogModel)
            // E. سجل النظام
            if (PermissionHelper.HasPermission("AuditLogs") || isSuperAdmin)
            {
                // 1. جلب البيانات من قاعدة البيانات (SQL) كـ Entities
                var logsFromDb = db.AuditLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(8)
                    .ToList(); // 👈 هذا الأمر يجلب البيانات للذاكرة

                // 2. التحويل إلى ViewModel داخل الذاكرة (C#)
                viewModel.RecentActivities = logsFromDb.Select(log => new AuditLogModel
                {
                    Id = log.Id,
                    Action = log.Action,
                    Controller = log.Controller,
                    Timestamp = log.Timestamp,
                    User = log.User
                }).ToList();

                viewModel.TotalUsersCount = db.Users.Count();
            }
            // F. الرسوم البيانية (Super Admin Only)
            if (isSuperAdmin)
            {
                viewModel.HistoricalData = GetHistoricalRegistrationCounts(yearRange);

                // توزيع المحامين حسب المحافظة
                var statusDict = db.ApplicationStatuses.ToDictionary(s => s.Name, s => s.Id);
                int practicingId = statusDict.ContainsKey("محامي مزاول") ? statusDict["محامي مزاول"] : 0;
                int activeTraineeId = statusDict.ContainsKey("متدرب مقيد") ? statusDict["متدرب مقيد"] : 0;

                viewModel.LawyersByGovernorate = db.ContactInfos
                    .Where(c => c.Id != 0 && !string.IsNullOrEmpty(c.Governorate) &&
                                db.GraduateApplications.Any(g => g.Id == c.Id && g.ApplicationStatusId == practicingId))
                    .GroupBy(c => c.Governorate)
                    .Select(g => new { Governorate = g.Key, Count = g.Count() })
                    .ToList()
                    .ToDictionary(x => x.Governorate, x => x.Count);

                viewModel.TraineesByGovernorate = db.ContactInfos
                    .Where(c => c.Id != 0 && !string.IsNullOrEmpty(c.Governorate) &&
                                db.GraduateApplications.Any(g => g.Id == c.Id && g.ApplicationStatusId == activeTraineeId))
                    .GroupBy(c => c.Governorate)
                    .Select(g => new { Governorate = g.Key, Count = g.Count() })
                    .ToList()
                    .ToDictionary(x => x.Governorate, x => x.Count);
            }



            return View(viewModel);
        }

        // دالة مساعدة لجلب البيانات التاريخية
        private List<HistoricalChartData> GetHistoricalRegistrationCounts(int years)
        {
            var results = new List<HistoricalChartData>();
            int currentYear = DateTime.Now.Year;
            int startYear = currentYear - years + 1;

            var dates = db.GraduateApplications.AsNoTracking()
                .Where(g => g.TrainingStartDate.HasValue || g.PracticeStartDate.HasValue)
                .Select(g => new {
                    TrainingStart = g.TrainingStartDate,
                    PracticeStart = g.PracticeStartDate
                })
                .ToList();

            for (int year = startYear; year <= currentYear; year++)
            {
                results.Add(new HistoricalChartData
                {
                    Year = year,
                    TraineeCount = dates.Count(d => d.TrainingStart.HasValue && d.TrainingStart.Value.Year <= year),
                    LawyerCount = dates.Count(d => d.PracticeStart.HasValue && d.PracticeStart.Value.Year <= year)
                });
            }
            return results;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}