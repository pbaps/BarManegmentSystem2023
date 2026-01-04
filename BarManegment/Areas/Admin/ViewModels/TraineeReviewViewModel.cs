using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class TraineeReviewViewModel
    {
        // البيانات الأساسية
        public int Id { get; set; }
        public string ArabicName { get; set; }
        public string EnglishName { get; set; }
        public string NationalIdNumber { get; set; }
        public DateTime BirthDate { get; set; }
        public string BirthPlace { get; set; }
        public string Nationality { get; set; }
        public string PersonalPhotoPath { get; set; }
        public string Status { get; set; }
        public long? TelegramChatId { get; set; }

        // === بداية الإضافة 1: إضافة الجنس ===
        public Gender Gender { get; set; }
        // === نهاية الإضافة ===

        // === بيانات إضافية لملف المتدرب ===
        public string TraineeSerialNo { get; set; } // (تأكدنا أنه string)
        public DateTime? TrainingStartDate { get; set; }

        // === بداية الإضافة 2 & 3: بيانات المزاولة ===
        public string MembershipId { get; set; } // رقم العضوية (مزاول)
        public DateTime? PracticeStartDate { get; set; } // تاريخ بدء المزاولة
                                                         // === نهاية الإضافة ===
                                                         // الإضافة الجديدة لحل مشكلة العرض
        public DateTime? SubmissionDate { get; set; }

        [Display(Name = "تاريخ انتهاء التدريب")]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? TrainingEndDate { get; set; }
        // بيانات الاتصال
        public ContactInfo ContactInfo { get; set; }
        // بيانات المشرف
        public GraduateApplication Supervisor { get; set; }
        // المؤهلات والمرفقات
        public List<Qualification> Qualifications { get; set; }
        public List<Attachment> Attachments { get; set; }

        // (خصائص قرار الرفض... كما هي)
        [Display(Name = "ملاحظات الرفض")]
        public string RejectionReason { get; set; }

        // === السجلات المرتبطة ===
        public List<Receipt> PaymentHistory { get; set; }
        public List<SupervisorChangeRequest> SupervisorChangeRequests { get; set; }
        public List<TraineeRenewal> Renewals { get; set; }
        public List<LegalResearch> LegalResearches { get; set; }
        public List<ExamEnrollment> ExamHistory { get; set; }
        public List<OralExamEnrollment> OralExamHistory { get; set; }
        public List<TraineeSuspension> CouncilSuspensions { get; set; } // (الإيقافات الإدارية)

        // === خصائص سير عمل أداء اليمين ===
        public List<OathRequest> OathRequestHistory { get; set; }
        public bool CanApplyForOath { get; set; } = false;
        public List<string> OathEligibilityIssues { get; set; }
        public bool IsPracticingLawyer { get; set; } = false;
        public List<TrainingLog> TrainingLogs { get; set; }
        // Constructor
        public TraineeReviewViewModel()
        {
            // (التهيئات السابقة)
            PaymentHistory = new List<Receipt>();
            Qualifications = new List<Qualification>();
            Attachments = new List<Attachment>();
            SupervisorChangeRequests = new List<SupervisorChangeRequest>();
            Renewals = new List<TraineeRenewal>();
            LegalResearches = new List<LegalResearch>();
            ExamHistory = new List<ExamEnrollment>();
            OralExamHistory = new List<OralExamEnrollment>();
            CouncilSuspensions = new List<TraineeSuspension>();
            OathRequestHistory = new List<OathRequest>();
            OathEligibilityIssues = new List<string>();
            TrainingLogs = new List<TrainingLog>();
        }
    }
}