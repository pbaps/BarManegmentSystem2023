using BarManegment.Models;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class AdminDashboardViewModel
    {
        // --- إحصائيات الأعضاء والتدريب ---
        public int NewApplicationsCount { get; set; }
        public int TotalApplicationsCount { get; set; } // ✅ تمت الإضافة
        public int ActiveTraineesCount { get; set; }
        public int PracticingLawyersCount { get; set; }
        public int NonPracticingLawyersCount { get; set; }
        public int PendingCommitteeApprovalCount { get; set; }
        public int PendingSupervisorRequestsCount { get; set; }

        // --- إحصائيات اليمين ---
        public int PendingOathRequestsCount { get; set; }

        // --- إحصائيات الامتحانات ---
        public int OpenExamsCount { get; set; }
        public int RegisteredForExamCount { get; set; }

        // --- إحصائيات المالية ---
        public int UnpaidVouchersCount { get; set; }
        public decimal TotalRevenueToday { get; set; }

        // --- النظام ---
        public int TotalUsersCount { get; set; }
        public List<AuditLogModel> RecentActivities { get; set; }

        // --- الرسوم البيانية ---
        public Dictionary<string, int> TraineesByGovernorate { get; set; }
        public Dictionary<string, int> LawyersByGovernorate { get; set; }
        public int SelectedYearRange { get; set; }
        public List<HistoricalChartData> HistoricalData { get; set; }

        // === 💡 الإضافة الجديدة: عدد طلبات امتحان القبول ===
        public int NewExamApplicationsCount { get; set; }
 
        // ==================================================

        public AdminDashboardViewModel()
        {
            RecentActivities = new List<AuditLogModel>();
            TraineesByGovernorate = new Dictionary<string, int>();
            LawyersByGovernorate = new Dictionary<string, int>();
            HistoricalData = new List<HistoricalChartData>();
        }
    }

    public class HistoricalChartData
    {
        public int Year { get; set; }
        public int TraineeCount { get; set; }
        public int LawyerCount { get; set; }
    }
}