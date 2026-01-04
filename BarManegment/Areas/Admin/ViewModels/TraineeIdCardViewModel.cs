using System;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class TraineeIdCardViewModel
    {
        public int TraineeId { get; set; } // GraduateApplication.Id

        [Display(Name = "الاسم الرباعي")]
        public string TraineeName { get; set; } // GraduateApplication.ArabicName

        [Display(Name = "رقم المتدرب")]
        public string TraineeSerialNo { get; set; } // GraduateApplication.TraineeSerialNo

        [Display(Name = "رقم الهوية")]
        public string NationalIdNumber { get; set; } // GraduateApplication.NationalIdNumber

        [Display(Name = "المحافظة")]
        public string Governorate { get; set; } // GraduateApplication.ContactInfo?.Governorate

        [Display(Name = "المحامي المشرف")]
        public string SupervisorName { get; set; } // GraduateApplication.Supervisor?.ArabicName

        [Display(Name = "الحالة المهنية")]
        public string ProfessionalStatus { get; set; } // GraduateApplication.ApplicationStatus?.Name

        [Display(Name = "تاريخ بدء التدريب")]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? TrainingStartDate { get; set; } // GraduateApplication.TrainingStartDate

        [Display(Name = "تاريخ انتهاء التدريب")]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}")]
        public DateTime? TrainingEndDate { get; set; } // (تاريخ الانتهاء المتوقع)

        [Display(Name = "الصورة الشخصية")]
        public string PersonalPhotoPath { get; set; } // GraduateApplication.PersonalPhotoPath

        // === بداية الإضافة: رقم العضوية للمحامي المزاول ===
        [Display(Name = "رقم العضوية")]
        public string MembershipId { get; set; }
        public DateTime? PracticeStartDate { get; internal set; }
        // === نهاية الإضافة ===
        // 💡 الخصائص التي كانت ناقصة وتمت إضافتها لحل الخطأ
        public DateTime CardIssueDate { get; set; }
        public DateTime CardExpiryDate { get; set; }
        // === الإضافة الجديدة لبيانات QR Code
        // ===
        public string QRCodeData { get; set; }
        // === نهاية الإضافة ===
    }
}