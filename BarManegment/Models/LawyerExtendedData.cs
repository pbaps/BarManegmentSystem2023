using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    // --- 1. بيانات المحامي الشخصية والمالية (تكميلية لـ GraduateApplication) ---
    [Table("LawyerPersonalData")]
    public class LawyerPersonalData
    {
        [Key, ForeignKey("Lawyer")]
        public int LawyerId { get; set; }

        [Required]
        [Display(Name = "الحالة الاجتماعية")]
        [StringLength(50)]
        public string MaritalStatus { get; set; }

        // === 💡 الإضافة الجديدة التي كانت ناقصة ===
        [Display(Name = "محافظة النزوح")]
        public string DisplacementGovernorate { get; set; }
        // ========================

        public virtual GraduateApplication Lawyer { get; set; }
        public virtual ICollection<LawyerSpouse> Spouses { get; set; }
        public virtual ICollection<LawyerChild> Children { get; set; }
        public virtual LawyerOffice Office { get; set; }
        public virtual SecurityHealthRecord HealthRecord { get; set; }
    }

    // --- 2. بيانات الزوج/الزوجة (للمتزوجين) ---
    [Table("LawyerSpouses")]
    public class LawyerSpouse
    {
        [Key]
        public int Id { get; set; }

        public int LawyerId { get; set; }
        [ForeignKey("LawyerId")]
        public virtual LawyerPersonalData LawyerData { get; set; }

        [Required, Display(Name = "اسم الزوج/ة")]
        public string FullName { get; set; }

        [Display(Name = "رقم الهوية")]
        public string NationalId { get; set; }

        [Display(Name = "نوع العمل")]
        public string OccupationType { get; set; } // ربة منزل/موظف/عمل خاص

        [Display(Name = "مكان العمل (تفصيلي)")]
        public string WorkPlace { get; set; }
    }

    // --- 3. بيانات الأبناء ---
    [Table("LawyerChildren")]
    public class LawyerChild
    {
        [Key]
        public int Id { get; set; }

        public int LawyerId { get; set; } // تم التعديل من FamilyRecordId

        [ForeignKey("LawyerId")]
        public virtual LawyerPersonalData LawyerData { get; set; } // تم التعديل من FamilyRecord

        [Required]
        [Display(Name = "اسم الابن/الابنة")]
        public string FullName { get; set; } // تم التعديل من Name (ليطابق المتحكم)

        [Display(Name = "رقم الهوية")]
        public string NationalId { get; set; }

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Display(Name = "الجنس")]
        public string Gender { get; set; } // (ذكر / أنثى)
    }

    // --- 4. بيانات المكتب والممتلكات ---
    [Table("LawyerOffice")]
    public class LawyerOffice
    {
        [Key, ForeignKey("LawyerData")]
        public int LawyerId { get; set; }
        public virtual LawyerPersonalData LawyerData { get; set; }

        [Display(Name = "اسم المكتب")]
        public string OfficeName { get; set; }

        // العنوان
        public string Governorate { get; set; }
        public string Area { get; set; }
        public string Street { get; set; }
        public string Building { get; set; }
        public string Floor { get; set; }

        [Display(Name = "نوع العقار")]
        public string PropertyType { get; set; } // شقة/مكتب في شقة/مكتب مستقل

        [Display(Name = "ملكية العقار")]
        public string OwnershipType { get; set; } // ايجار/ملك/شراء مع شريك

        // حالة العقار (بسبب الأضرار)
        [Display(Name = "حالة العقار الحالية")]
        public string CurrentCondition { get; set; } // سليم/ضرر جزئي/ضرر كلي

        [Display(Name = "تفاصيل الضرر")]
        public string DamageDetails { get; set; }

        // الشركاء (علاقة خارجية)
        public virtual ICollection<OfficePartner> Partners { get; set; }
    }

    [Table("OfficePartners")]
    public class OfficePartner
    {
        [Key]
        public int Id { get; set; }

        public int LawyerId { get; set; }
        [ForeignKey("LawyerId")]
        public virtual LawyerOffice Office { get; set; }

        [Display(Name = "اسم الشريك")]
        public string PartnerName { get; set; }

        [Display(Name = "رقم العضوية/الهوية")]
        public string PartnerIdentification { get; set; }
    }

    // --- 5. السجل الأمني والصحي ---
    [Table("SecurityHealthRecord")]
    public class SecurityHealthRecord
    {
        [Key, ForeignKey("LawyerData")]
        public int LawyerId { get; set; }
        public virtual LawyerPersonalData LawyerData { get; set; }

        [Display(Name = "هل تعرض المحامي للاعتقال؟")]
        public bool WasDetained { get; set; }

        [Display(Name = "تاريخ البدء")]
        [DataType(DataType.Date)]
        public DateTime? DetentionStartDate { get; set; }

        [Display(Name = "تاريخ الانتهاء")]
        [DataType(DataType.Date)]
        public DateTime? DetentionEndDate { get; set; }

        [Display(Name = "مكان الاعتقال")]
        public string DetentionPlace { get; set; }

        [Display(Name = "مسار مرفق الإفادة")]
        public string DetentionAffidavitPath { get; set; }

        public bool WasInjured { get; set; }

        // === 💡 الإضافات الصحية التي كانت تسبب الخطأ ===
        [Display(Name = "الحالة الصحية العامة")]
        public string GeneralHealthStatus { get; set; } // سليم، مريض، مصاب

        [Display(Name = "هل تمتلك تأمين صحي؟")]
        public bool HasHealthInsurance { get; set; }

        [Display(Name = "رقم التأمين الصحي")]
        public string HealthInsuranceNumber { get; set; }

        [Display(Name = "هل تتناول أدوية مزمنة؟")]
        public bool IsTakingMedication { get; set; }

        [Display(Name = "قائمة الأدوية")]
        public string MedicationsList { get; set; }
        // =========================

        public virtual ICollection<InjuryRecord> Injuries { get; set; }
    }

    [Table("InjuryRecords")]
    public class InjuryRecord
    {
        [Key]
        public int Id { get; set; }

        public int LawyerId { get; set; }
        [ForeignKey("LawyerId")]
        public virtual SecurityHealthRecord HealthRecord { get; set; }

        [Display(Name = "اسم المصاب")]
        public string InjuredName { get; set; }

        [Display(Name = "العلاقة بالمحامي")]
        public string Relationship { get; set; } // شخصيا، زوجة، ابن، ابنة

        [Display(Name = "مكان الإصابة")]
        public string InjuryLocation { get; set; }

        [Display(Name = "مسار التقرير الطبي")]
        public string MedicalReportPath { get; set; }
    }
}