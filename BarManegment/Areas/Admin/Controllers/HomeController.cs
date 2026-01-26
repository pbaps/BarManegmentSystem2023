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
        public ActionResult Index(int yearRange = 5)
        {
            var viewModel = new AdminDashboardViewModel();
            viewModel.SelectedYearRange = yearRange;

            int currentUserId = (int)Session["UserId"];
            var currentUser = db.Users.Include(u => u.UserType).FirstOrDefault(u => u.Id == currentUserId);
            bool isSuperAdmin = (currentUser != null && currentUser.UserType.NameEnglish == "Administrator");
            ViewBag.IsSuperAdmin = isSuperAdmin;

            // =========================================================
            // 1. شؤون الأعضاء والتدريب
            // =========================================================
            if (PermissionHelper.HasPermission("GraduateApplications") || PermissionHelper.HasPermission("RegisteredTrainees"))
            {
                viewModel.NewApplicationsCount = db.GraduateApplications.Count(a => a.ApplicationStatus.Name == "طلب جديد");
                viewModel.TotalApplicationsCount = db.GraduateApplications.Count();
                viewModel.ActiveTraineesCount = db.GraduateApplications.Count(a => a.ApplicationStatus.Name == "متدرب مقيد");
                viewModel.PracticingLawyersCount = db.GraduateApplications.Count(a => a.ApplicationStatus.Name == "محامي مزاول");
                viewModel.NonPracticingLawyersCount = db.GraduateApplications.Count(a => a.ApplicationStatus.Name == "محامي غير مزاول");
                viewModel.PendingCommitteeApprovalCount = db.GraduateApplications.Count(a => a.ApplicationStatus.Name == "بانتظار الموافقة النهائية");

                viewModel.PendingSupervisorRequestsCount = db.SupervisorChangeRequests.Count(r => r.Status == "بانتظار موافقة اللجنة");
                viewModel.PendingOathRequestsCount = db.OathRequests.Count(o => o.Status == "بانتظار موافقة لجنة اليمين");
            }

            // =========================================================
            // 2. الامتحانات
            // =========================================================
            if (PermissionHelper.HasPermission("Exams"))
            {
                viewModel.NewExamApplicationsCount = db.ExamApplications.Count(e => e.Status == "قيد المراجعة");
                viewModel.OpenExamsCount = db.Exams.Count(e => e.IsActive && e.StartTime > DateTime.Now);
            }

            // =========================================================
            // 3. المالية (تمت معالجة الخطأ المحتمل هنا)
            // =========================================================
            if (PermissionHelper.HasPermission("Finance") || PermissionHelper.HasPermission("PaymentVouchers"))
            {
                viewModel.UnpaidVouchersCount = db.PaymentVouchers.Count(v => v.Status == "صادر");

                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                viewModel.TotalRevenueToday = db.Receipts
                    .Where(r => r.CreationDate >= today && r.CreationDate < tomorrow)
                    .Select(r => r.PaymentVoucher.TotalAmount)
                    .DefaultIfEmpty(0)
                    .Sum();

                // ✅ التحقق من وجود جدول الشيكات قبل الاستعلام لتجنب الأخطاء إذا لم يتم إضافته
                try
                {
                    // تأكد أنك أضفت DbSet<CheckPortfolio> في ApplicationDbContext

                    viewModel.DueChecksCount = db.ChecksPortfolio // <--- تأكد من حرف s هنا
  .Count(c => c.DueDate <= DateTime.Now && c.Status == CheckStatus.UnderCollection);
                }
                catch
                {
                    // في حال لم يتم ترحيل جدول الشيكات بعد، نجعل القيمة 0
                    viewModel.DueChecksCount = 0;
                }
            }

            // =========================================================
            // 4. العقود والطوابع
            // =========================================================
            if (PermissionHelper.HasPermission("ContractTransactions"))
            {
                viewModel.PendingContractsCount = db.ContractTransactions.Count(c => c.Status == "بانتظار التصديق");
            }

            if (PermissionHelper.HasPermission("StampInventory"))
            {
                viewModel.AvailableStampsCount = db.Stamps.Count(s => s.Status == "في المخزن");
            }

            // =========================================================
            // 5. سجل النظام والرسوم البيانية (Admin Only)
            // =========================================================
            if (isSuperAdmin)
            {
                var logsFromDb = db.AuditLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(6)
                    .ToList();

                viewModel.RecentActivities = logsFromDb.Select(log => new AuditLogModel
                {
                    Id = log.Id,
                    Action = log.Action,
                    Controller = log.Controller,
                    Timestamp = log.Timestamp,
                    User = log.User
                }).ToList();

                viewModel.HistoricalData = GetHistoricalRegistrationCounts(yearRange);

                // ✅✅✅ تصحيح الخطأ الثاني هنا: عكس الاستعلام ✅✅✅
                // بدلاً من البحث في ContactInfos، نبحث في GraduateApplications ونجمع حسب المحافظة
                viewModel.LawyersByGovernorate = db.GraduateApplications
                    .Include(g => g.ContactInfo) // التأكد من تحميل بيانات الاتصال
                    .Where(g => g.ApplicationStatus.Name == "محامي مزاول" && g.ContactInfo != null && !string.IsNullOrEmpty(g.ContactInfo.Governorate))
                    .GroupBy(g => g.ContactInfo.Governorate)
                    .Select(g => new { Governorate = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.Governorate, x => x.Count);
            }

            return View(viewModel);
        }

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