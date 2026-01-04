using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    // 1. المتعهد (الشخص/المكان الذي يبيع الطوابع)
    [Table("StampContractors")]
    public class StampContractor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "اسم المتعهد")]
        public string Name { get; set; }

        [Display(Name = "رقم الجوال")]
        public string Phone { get; set; }

        [Display(Name = "رقم الهوية")]
        public string NationalId { get; set; }

        [Display(Name = "المحافظة")]
        public string Governorate { get; set; }

        [Display(Name = "الموقع (محكمة/نيابة)")]
        public string Location { get; set; }

        public bool IsActive { get; set; }

        // (اختياري) ربط المتعهد بحساب مستخدم ليدخل للنظام
        // public string UserId { get; set; }

        public virtual ICollection<StampBookIssuance> Issuances { get; set; }
    }

    // 2. دفتر الطوابع (المجموعة)
    [Table("StampBooks")]
    public class StampBook
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "الرقم التسلسلي الأولي")]
        public long StartSerial { get; set; } // مثال: 10001

        [Display(Name = "الرقم التسلسلي النهائي")]
        public long EndSerial { get; set; } // مثال: 10050

        [Display(Name = "الكمية")]
        public int Quantity { get; set; } // 50

        [Display(Name = "قيمة الطابع الواحد")]
        public decimal ValuePerStamp { get; set; } // 100

        [Display(Name = "تاريخ الإدخال")]
        public DateTime DateAdded { get; set; }

        [Display(Name = "مرجع قرار المجلس")]
        public string CouncilDecisionRef { get; set; }

        [Display(Name = "حالة الدفتر")]
        public string Status { get; set; } // "في المخزن", "تم صرفه", "مُستهلك"

        // العلاقات
        public virtual ICollection<Stamp> Stamps { get; set; }
        public virtual ICollection<StampBookIssuance> Issuances { get; set; }
    }

    // 3. سجل صرف الدفاتر (العملية التي تربط المتعهد بالدفتر)
    [Table("StampBookIssuances")]
    public class StampBookIssuance
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Contractor")]
        public int ContractorId { get; set; }

        [ForeignKey("StampBook")]
        public int StampBookId { get; set; }

        [ForeignKey("PaymentVoucher")]
        [Display(Name = "رقم إيصال القبض")]
        public int PaymentVoucherId { get; set; } // الربط مع المحاسبة

        [Display(Name = "تاريخ الصرف")]
        public DateTime IssuanceDate { get; set; }

        // الخصائص الملاحية
        public virtual StampContractor Contractor { get; set; }
        public virtual StampBook StampBook { get; set; }
        public virtual PaymentVoucher PaymentVoucher { get; set; }
    }


    // 4. الطابع الفردي (أهم جدول لتتبع المبيعات)
    [Table("Stamps")]
    public class Stamp
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("StampBook")]
        public int StampBookId { get; set; }

        [Index(IsUnique = true)] // ضمان عدم تكرار الرقم
        [Display(Name = "الرقم التسلسلي")]
        public long SerialNumber { get; set; }

        [Display(Name = "القيمة")]
        public decimal Value { get; set; }

        [Display(Name = "الحالة")]
        public string Status { get; set; }
        // "في المخزن" (Available)
        // "مع المتعهد" (WithContractor)
        // "مباع" (Sold)
        // "محجوز" (Reserved) - (بسبب مديونية)
        // "مدفوع للمحامي" (PaidOut)
        // "ملغي" (Cancelled)

        // --- التتبع ---
        [Display(Name = "المتعهد الحالي")]
        public int? ContractorId { get; set; } // مع أي متعهد هو الآن

        [Display(Name = "مباع للمحامي")]
        [ForeignKey("Lawyer")]
        public int? SoldToLawyerId { get; set; } // (FK to GraduateApplication)

        [Display(Name = "تاريخ البيع")]
        public DateTime? DateSold { get; set; }

        [Display(Name = "هل تم تحويل حصة المحامي؟")]
        public bool IsPaidToLawyer { get; set; }

        // الخصائص الملاحية
        public virtual StampBook StampBook { get; set; }
        public virtual GraduateApplication Lawyer { get; set; }
    }
}