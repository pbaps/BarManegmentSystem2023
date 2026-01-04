using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Members.ViewModels
{// === بداية الإضافة: موديلات فرعية لبيانات المشرف ===
    public class MyTraineeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SerialNo { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class PendingSupervisionRequestViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime SubmissionDate { get; set; }
    }
    // === نهاية الإضافة ===
    // (مودل فرعي للمحاضرات)
    public class UpcomingLectureViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public string TeamsLink { get; set; }
    }
    // === بداية الإضافة: مودل فرعي لسجلات التدريب
    // ===
    public class PendingTrainingLogViewModel
    {
        public int LogId { get; set; }
        public string TraineeName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime SubmissionDate { get; set; }
    }
    // === نهاية الإضافة ===
    // (مودل فرعي للأبحاث)
    public class ResearchTaskViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }

        // --- تم التعديل ---
        // (المودل LegalResearch لا يحتوي على درجة أو تاريخ استحقاق، لذا جعلناها اختيارية)
        public double? Grade { get; set; }
        public DateTime? DueDate { get; set; }
    }

    // (مودل فرعي للطلبات)
    public class ServiceRequestViewModel
    {
        public int Id { get; set; }
        public string RequestType { get; set; }
        public string Status { get; set; }
        public DateTime SubmissionDate { get; set; }
    }

    // (مودل فرعي للامتحانات)
    public class EnrolledExamViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationInMinutes { get; set; }

        // 💡 حقول إضافية للنتائج
        public string Result { get; set; } // ناجح/راسب
        public double? Score { get; set; }
        public double TotalScore { get; set; }
    }

    // === بداية الإضافة: مودل فرعي جديد لفترات الإيقاف ===
    public class MemberSuspensionViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; } // قد تكون القيمة فارغة إذا كان الإيقاف مفتوحاً
        public string Reason { get; set; }
        public string Status { get; set; } // (سارية / منتهية)
    }
    // === نهاية الإضافة ===
    // === (المودل الرئيسي للوحة التحكم) ===
    public class MemberDashboardViewModel
    {
        public GraduateApplication GraduateInfo { get; set; }
        public List<EnrolledExamViewModel> EnrolledExams { get; set; }
        public List<UpcomingLectureViewModel> UpcomingLectures { get; set; }
        public List<ResearchTaskViewModel> ResearchTasks { get; set; }
        public List<ServiceRequestViewModel> MyServiceRequests { get; set; }


        // 💡 الإضافة: سجل الامتحانات السابقة (النتائج)
        public List<EnrolledExamViewModel> FinishedExams { get; set; }
        // === بداية الإضافة ===
        public List<MemberReceiptViewModel> MyReceipts { get; set; }
        // === نهاية الإضافة ===
        // === بداية الإضافة ===
        public List<MemberSuspensionViewModel> MySuspensions { get; set; }
        // === نهاية الإضافة ===
        // === بداية الإضافة: سجل تجديدات المزاولة
        // ===
        public List<PracticingLawyerRenewal> PracticingRenewals { get; set; }
        // === نهاية الإضافة ===
        // === بداية الإضافة: بيانات خاصة بالمشرف
        // ===
        public List<MyTraineeViewModel> MyTrainees { get; set; }
        public List<PendingSupervisionRequestViewModel> PendingSupervisionRequests { get; set; }
        // (لاحقاً: public List<PendingTrainingLogViewModel> PendingTrainingLogs { get; set; })
        public List<TrainingLog> MyTrainingLogs { get; set; } // (قائمة السجلات الأخيرة)

        public List<PendingTrainingLogViewModel> PendingTrainingLogs { get; set; }
        // 💡 الإضافة الجديدة: الاختبارات الوظيفية المتاحة للتقدم
        public List<AvailableExamViewModel> AvailableJobTests { get; set; }
        // 💡💡 === بداية الإضافة === 💡💡
        [Display(Name = "حصصي من التصديقات")]
        public List<MemberShareViewModel> MyContractShares { get; set; }
        // 💡💡 === نهاية الإضافة === 💡💡
        // 💡💡 === بداية الإضافة: إضافة قائمة القروض === 💡💡
        [Display(Name = "القروض الخاصة بي")]
        public List<MemberLoanViewModel> MyLoans { get; set; }
        // 💡💡 === نهاية الإضافة === 💡💡

        public MemberDashboardViewModel()
        {
            EnrolledExams = new List<EnrolledExamViewModel>();
            UpcomingLectures = new List<UpcomingLectureViewModel>();
            ResearchTasks = new List<ResearchTaskViewModel>();
            MyServiceRequests = new List<ServiceRequestViewModel>();

            // === بداية الإضافة ===
            MyReceipts = new List<MemberReceiptViewModel>();
            // === نهاية الإضافة ===
            // === بداية الإضافة ===
            MySuspensions = new List<MemberSuspensionViewModel>();
            // === نهاية الإضافة ===
            MyTrainingLogs = new List<TrainingLog>();
            // === بداية الإضافة: تهيئة القائمة الجديدة
            // ===
            PracticingRenewals = new List<PracticingLawyerRenewal>();
            // === نهاية الإضافة ===


            PendingTrainingLogs = new List<PendingTrainingLogViewModel>();
            MyTrainees = new List<MyTraineeViewModel>();
            PendingSupervisionRequests = new List<PendingSupervisionRequestViewModel>();
            // 💡 (تهيئة القائمة الجديدة)
            MyContractShares = new List<MemberShareViewModel>();
            // 💡 (تهيئة القائمة الجديدة)
            MyLoans = new List<MemberLoanViewModel>();
        }
    }



    // === بداية الإضافة: مودل فرعي جديد للإيصالات ===
    public class MemberReceiptViewModel
    {
        public int Id { get; set; } // هذا هو ID القسيمة (VoucherId) للربط
        public string ReceiptFullNumber { get; set; } // هذا هو الرقم المتسلسل "Year/Sequence"
        public DateTime PaymentDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrencySymbol { get; set; }
    }
    // === نهاية الإضافة ===

    // 💡 كلاس جديد لتمثيل الامتحان المتاح
    public class AvailableExamViewModel
    {
        public int ExamId { get; set; }
        public string Title { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int Duration { get; set; }
        public string RequirementsNote { get; set; } // ملاحظات الشروط
        public bool IsEligible { get; set; } // هل يحق له التقدم؟
        public string IneligibilityReason { get; set; } // سبب الرفض إن وجد
    }
}