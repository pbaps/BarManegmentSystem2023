using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    // هذا الجدول يسجل كل عملية بيع طابع فردي لمحامي
    public class StampSale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "رقم الطابع")]
        [Index("IX_StampSale_StampId", IsUnique = true)]
        public int StampId { get; set; } // <-- ✅ تم التعديل إلى int
        [ForeignKey("StampId")]
        public virtual Stamp Stamp { get; set; }

        [Required]
        [Display(Name = "المتعهد البائع")]
        public int ContractorId { get; set; }
        [ForeignKey("ContractorId")]
        public virtual StampContractor Contractor { get; set; }

        [Display(Name = "تاريخ البيع")]
        public DateTime SaleDate { get; set; }

        // --- بيانات المحامي المشتري ---
        [Display(Name = "ملف المحامي (إن وجد)")]
        public int? GraduateApplicationId { get; set; }
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication GraduateApplication { get; set; }

        // --- ⬇️ ⬇️ قم بإضافة هذين السطرين هنا ⬇️ ⬇️ ---
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication Lawyer { get; set; }
        // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---

        [Required(ErrorMessage = "رقم عضوية المحامي مطلوب")]
        [Display(Name = "رقم عضوية المحامي")]
        public string LawyerMembershipId { get; set; } // تم التعديل من BarId

        [Required(ErrorMessage = "اسم المحامي مطلوب")]
        [Display(Name = "اسم المحامي")]
        public string LawyerName { get; set; }

        // --- ⬇️ ⬇️ تأكد من وجود هذه الحقول ⬇️ ⬇️ ---
        [Display(Name = "اسم البنك")]
        [StringLength(100)]
        public string LawyerBankName { get; set; }

        [Display(Name = "فرع البنك")]
        [StringLength(100)]
        public string LawyerBankBranch { get; set; } // <-- (الإضافة 1)

        [Display(Name = "رقم الحساب")]
        [StringLength(50)]
        public string LawyerAccountNumber { get; set; } // <-- (الإضافة 2)

        [Display(Name = "رقم الآيبان (IBAN)")]
        [StringLength(34)]
        public string LawyerIban { get; set; }
        // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---

        // --- بيانات مالية (تُحسب آلياً) ---
        [Display(Name = "قيمة الطابع")]
        public decimal StampValue { get; set; }

        [Display(Name = "حصة المحامي")]
        public decimal AmountToLawyer { get; set; }

        [Display(Name = "حصة النقابة")]
        public decimal AmountToBar { get; set; }

        [Display(Name = "هل تم الصرف للمحامي؟")]
        public bool IsPaidToLawyer { get; set; }



        // --- ⬇️ ⬇️ بداية الإضافة الجديدة ⬇️ ⬇️ ---

        [Display(Name = "تاريخ الإرسال للبنك")]
        public DateTime? BankSendDate { get; set; }

        [Required]
        [Display(Name = "معاملة محجوزة؟")]
        [DefaultValue(false)]
        public bool IsOnHold { get; set; } = false;

        [Display(Name = "سبب الحجز")]
        [StringLength(500)]
        public string HoldReason { get; set; } // (مثال: "مستحقات قرض", "مراجعة إدارية")

        // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---

        // --- بيانات المدخل ---
        [Required]
        public int RecordedByUserId { get; set; }
        [Required]
        public string RecordedByUserName { get; set; }
    }
}