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

        // القوائم والسجلات
        public List<PracticingLawyerRenewal> PracticingRenewals { get; set; }
        public List<Receipt> PaymentHistory { get; set; }
        public List<GraduateApplication> MyTrainees { get; set; }
        public List<TrainingLog> PendingTrainingLogs { get; set; }

        public List<Attachment> Attachments { get; set; }

        // 💡 الإضافة: قائمة المؤهلات التي كانت مفقودة وتسبب الخطأ
        public List<Qualification> Qualifications { get; set; }

        public LawyerProfileViewModel()
        {
            PracticingRenewals = new List<PracticingLawyerRenewal>();
            PaymentHistory = new List<Receipt>();
            MyTrainees = new List<GraduateApplication>();
            PendingTrainingLogs = new List<TrainingLog>();
            Attachments = new List<Attachment>();
            Qualifications = new List<Qualification>(); // تهيئة القائمة
            ContactInfo = new ContactInfo();
        }
    }
}