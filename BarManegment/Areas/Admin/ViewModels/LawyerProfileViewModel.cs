using BarManegment.Models;
using System;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class LawyerProfileViewModel
    {
        // البيانات الأساسية
        public int Id { get; set; }
        public string ArabicName { get; set; }
        public string EnglishName { get; set; }
        public string NationalIdNumber { get; set; }
        public string MembershipId { get; set; }
        public DateTime? PracticeStartDate { get; set; }
        public string Status { get; set; }
        public string PersonalPhotoPath { get; set; }
        public DateTime BirthDate { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public Gender Gender { get; set; }

        // إحصائيات سريعة (للعرض في أعلى الصفحة)
        public int ActiveTraineesCount { get; set; }
        public int YearsOfExperience { get; set; }
        public decimal TotalLoansAmount { get; set; }
        public string LastRenewalYear { get; set; }

        // القوائم والسجلات
        public List<PracticingLawyerRenewal> PracticingRenewals { get; set; }
        public List<Receipt> PaymentHistory { get; set; }
        public List<GraduateApplication> MyTrainees { get; set; }
        public List<TrainingLog> PendingTrainingLogs { get; set; }
        public List<Attachment> Attachments { get; set; }
        public List<Qualification> Qualifications { get; set; }

        // ✅ تم نقل القروض هنا بدلاً من ViewBag
        public List<LoanApplication> Loans { get; set; }

        public List<AgendaItem> CouncilDecisions { get; set; }

        public LawyerProfileViewModel()
        {
            PracticingRenewals = new List<PracticingLawyerRenewal>();
            PaymentHistory = new List<Receipt>();
            MyTrainees = new List<GraduateApplication>();
            PendingTrainingLogs = new List<TrainingLog>();
            Attachments = new List<Attachment>();
            Qualifications = new List<Qualification>();
            Loans = new List<LoanApplication>();
            ContactInfo = new ContactInfo();
            CouncilDecisions = new List<AgendaItem>();
        }
    }
}