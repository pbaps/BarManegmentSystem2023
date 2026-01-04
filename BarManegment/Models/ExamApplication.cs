using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class ExamApplication
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [Display(Name = "الاسم الكامل")]
        [StringLength(200)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "الرقم الوطني - الهوية مطلوب")]
        [Display(Name = "رقم الهوية ")]
        [StringLength(50)]
        [Index(IsUnique = true)]
        public string NationalIdNumber { get; set; }

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "الجنس مطلوب")]
        [Display(Name = "الجنس")]
        public int GenderId { get; set; }
        [ForeignKey("GenderId")]
        public virtual Gender Gender { get; set; }

        [Required(ErrorMessage = "رقم الجوال مطلوب")]
        [Display(Name = "رقم الجوال")]
        [StringLength(20)]
        public string MobileNumber { get; set; }

        [Display(Name = "رقم الواتساب")]
        [StringLength(20)]
        public string WhatsAppNumber { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [Display(Name = "البريد الإلكتروني")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        // المرفقات
        public string HighSchoolCertificatePath { get; set; }
        public string BachelorCertificatePath { get; set; }
        public string PersonalIdPath { get; set; }

        public DateTime ApplicationDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // "قيد المراجعة", "مقبول للامتحان", "مرفوض"
        // === بداية الإضافة: حقل كلمة المرور المؤقتة ===
        [StringLength(256)]
        public string TemporaryPassword { get; set; }
        // === نهاية الإضافة ===
        public string RejectionReason { get; set; }

        public double? ExamScore { get; set; }
        public string ExamResult { get; set; } // "ناجح", "راسب"
                                               // === بداية الإضافة: حقل لتتبع إنشاء الحساب ===
                                               // === بداية الإضافة: حقل معرّف تليجرام ===
        [Display(Name = "معرف دردشة تليجرام")]
        public long? TelegramChatId { get; set; }
        // === نهاية الإضافة ===
        public bool IsAccountCreated { get; set; } = false;
        // === نهاية الإضافة ===
        public virtual ICollection<ExamQualification> Qualifications { get; set; }
    }
}