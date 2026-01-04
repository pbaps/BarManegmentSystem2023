using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class GraduateApplication
    {
        [Key]
        [Display(Name = "رقم الطلب")]
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم باللغة العربية مطلوب")]
        [Display(Name = "الاسم باللغة العربية")]
        [StringLength(200)]
        public string ArabicName { get; set; }

        [Display(Name = "الاسم باللغة الإنجليزية")]
        [StringLength(200)]
        public string EnglishName { get; set; }

        [Required(ErrorMessage = "الرقم الوطني مطلوب")]
        [Display(Name = "الرقم الوطني")]
        [StringLength(50)]
        public string NationalIdNumber { get; set; }

        [Required(ErrorMessage = "نوع الهوية مطلوب")]
        [Display(Name = "نوع الهوية")]
        public int NationalIdTypeId { get; set; }
        [ForeignKey("NationalIdTypeId")]
        public virtual NationalIdType NationalIdType { get; set; }

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Display(Name = "مكان الميلاد")]
        [StringLength(100)]
        public string BirthPlace { get; set; }

        [Display(Name = "الجنسية")]
        [StringLength(100)]
        public string Nationality { get; set; }

        [Required(ErrorMessage = "الجنس مطلوب")]
        [Display(Name = "الجنس")]
        public int GenderId { get; set; }
        [ForeignKey("GenderId")]
        public virtual Gender Gender { get; set; }

        [Required(ErrorMessage = "حالة الطلب مطلوبة")]
        [Display(Name = "حالة الطلب")]
        public int ApplicationStatusId { get; set; }
        [ForeignKey("ApplicationStatusId")]
        public virtual ApplicationStatus ApplicationStatus { get; set; }

        [Display(Name = "الصورة الشخصية")]
        [StringLength(500)]
        public string PersonalPhotoPath { get; set; }

        [Display(Name = "تاريخ تقديم الطلب")]
        public DateTime SubmissionDate { get; set; }

        // === بداية الإضافة: حقل المحامي المشرف ===
        [Display(Name = "المحامي المشرف")]
        public int? SupervisorId { get; set; }
        [ForeignKey("SupervisorId")]
        public virtual GraduateApplication Supervisor { get; set; }

        // علاقة عكسية لتسهيل حساب عدد المتدربين
        public virtual ICollection<GraduateApplication> Trainees { get; set; }
        // === نهاية الإضافة ===

        // === بداية التعديل: جعل العلاقة مع المستخدم اختيارية ===
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual UserModel User { get; set; }
        // === نهاية التعديل ===
 
        // === بداية الإضافة: حقل تاريخ بدء التدريب ===
        [Display(Name = "تاريخ بدء التدريب")]
        [DataType(DataType.Date)]
        public DateTime? TrainingStartDate { get; set; }
 
        // === بداية الإضافة: حقل تاريخ بدء المزاولة ===
        [Display(Name = "تاريخ بدء المزاولة")]
        [DataType(DataType.Date)]
        public DateTime? PracticeStartDate { get; set; }
        // === نهاية الإضافة ===
        [Display(Name = "رقم العضوية (مزاول)")]
        [StringLength(20)]
        [Index("IX_MembershipId")]// اختياري: لضمان عدم تكرار رقم العضوية
        public string MembershipId { get; set; } // الرقم الجديد بعد اليمين

        [Display(Name = "موعد أداء اليمين")]
        public int? OathCeremonyId { get; set; }
        [ForeignKey("OathCeremonyId")]
        public virtual OathCeremony OathCeremony { get; set; }

        [Display(Name = "ملاحظات إدارية")]
        public string Notes { get; set; }

        // --- نهاية الإضافة ---
        // === بداية التعديل: إزالة قيد الفهرس الفريد ===
        [Display(Name = "الرقم المتسلسل")]
        [StringLength(20)]
        public string TraineeSerialNo { get; set; }
        // === نهاية التعديل ===
        // === بداية الإضافة: حقل معرّف تليجرام ===
        [Display(Name = "معرف دردشة تليجرام")]
        public long? TelegramChatId { get; set; }
        // === نهاية الإضافة ===
        // --- ⬇️ ⬇️ بداية الإضافة: بيانات البنك ⬇️ ⬇️ ---
        [Display(Name = "اسم البنك")]
        [StringLength(100)]
        public string BankName { get; set; }

        [Display(Name = "فرع البنك")]
        [StringLength(100)]
        public string BankBranch { get; set; }

        [Display(Name = "رقم الحساب")]
        [StringLength(50)]
        public string AccountNumber { get; set; }

        [Display(Name = "رقم الآيبان (IBAN)")]
        [StringLength(34)] // (الطول القياسي للـ IBAN)
        public string Iban { get; set; }
        // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---
        // === بداية الإضافة: علاقة سجل تغيير المشرفين ===
        public virtual ICollection<SupervisorHistory> SupervisorChanges { get; set; }
        // === نهاية الإضافة ===
        // === بداية الإضافة: علاقة مع طلب الامتحان ===
        public int? ExamApplicationId { get; set; }
        [ForeignKey("ExamApplicationId")]
        public virtual ExamApplication ExamApplication { get; set; }
        // === نهاية الإضافة ===
        public virtual ContactInfo ContactInfo { get; set; }

        // داخل class GraduateApplication
        [Display(Name = "رقم المحفظة الإلكترونية")]
        public string WalletNumber { get; set; }

        [Display(Name = "مزود خدمة المحفظة")]
        public int? WalletProviderId { get; set; }

        [ForeignKey("WalletProviderId")]
        public virtual SystemLookup WalletProvider { get; set; } // سنستخدم SystemLookup أو جدول منفصل


        public virtual ICollection<Qualification> Qualifications { get; set; }
        public virtual ICollection<Attachment> Attachments { get; set; }
        public virtual ICollection<OathRequest> OathRequests { get; set; }
        public virtual ICollection<LegalResearch> LegalResearches { get; set; }
        public virtual ICollection<LoanApplication> LoanApplications { get; set; }

        public virtual LawyerPersonalData LawyerPersonalData { get; set; }
        // (الكونستركتور - إذا كان موجودًا، أضف السطر التالي)
        public GraduateApplication()
        {
            OathRequests = new HashSet<OathRequest>(); // تهيئة القائمة
            Trainees = new HashSet<GraduateApplication>(); // (إضافة تهيئة)
            SupervisorChanges = new HashSet<SupervisorHistory>(); // (إضافة تهيئة)
            LegalResearches = new HashSet<LegalResearch>();
            LoanApplications = new HashSet<LoanApplication>();
        }
    }
}