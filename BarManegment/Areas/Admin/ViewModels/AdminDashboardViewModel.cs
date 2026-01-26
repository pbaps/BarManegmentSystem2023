using BarManegment.Models;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class AdminDashboardViewModel
    {
        // --- إحصائيات الأعضاء والتدريب ---
        public int NewApplicationsCount { get; set; }
        public int TotalApplicationsCount { get; set; }
        public int ActiveTraineesCount { get; set; }
        public int PracticingLawyersCount { get; set; }
        public int NonPracticingLawyersCount { get; set; }
        public int PendingCommitteeApprovalCount { get; set; }
        public int PendingSupervisorRequestsCount { get; set; }

        // --- إحصائيات اليمين والامتحانات ---
        public int PendingOathRequestsCount { get; set; }
        public int NewExamApplicationsCount { get; set; }
        public int OpenExamsCount { get; set; }

        // --- إحصائيات العقود والطوابع (جديد) ---
        public int PendingContractsCount { get; set; } // عقود بانتظار التصديق
        public int AvailableStampsCount { get; set; } // رصيد الطوابع في المخزن

        // --- إحصائيات المالية والشيكات ---
        public int UnpaidVouchersCount { get; set; }
        public decimal TotalRevenueToday { get; set; }
        public int DueChecksCount { get; set; } // شيكات مستحقة

        // --- النظام والرسوم البيانية ---
        public int TotalUsersCount { get; set; }
        public List<AuditLogModel> RecentActivities { get; set; }
        public Dictionary<string, int> TraineesByGovernorate { get; set; }
        public Dictionary<string, int> LawyersByGovernorate { get; set; }
        public int SelectedYearRange { get; set; }
        public List<HistoricalChartData> HistoricalData { get; set; }

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