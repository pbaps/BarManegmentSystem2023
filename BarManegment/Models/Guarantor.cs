using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("Guarantors")]
    public class Guarantor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "طلب القرض")]
        public int LoanApplicationId { get; set; }
        [ForeignKey("LoanApplicationId")]
        public virtual LoanApplication LoanApplication { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "نوع الكفيل")]
        public string GuarantorType { get; set; } // (محامي، موظف خارجي)

        // --- بيانات الكفيل (إذا كان محامي) ---
        [Display(Name = "الكفيل المحامي")]
        public int? LawyerGuarantorId { get; set; }
        [ForeignKey("LawyerGuarantorId")]
        public virtual GraduateApplication LawyerGuarantor { get; set; }

        [Display(Name = "استثناء (تجاوز المنع)")]
        [DefaultValue(false)]
        public bool IsOverride { get; set; } = false;

        // --- بيانات الكفيل (إذا كان موظف خارجي) ---
        [StringLength(200)]
        [Display(Name = "الاسم (خارجي)")]
        public string ExternalName { get; set; }

        [StringLength(50)]
        [Display(Name = "رقم الهوية (خارجي)")]
        public string ExternalIdNumber { get; set; }

        [StringLength(150)]
        [Display(Name = "الوظيفة (خارجي)")]
        public string JobTitle { get; set; }

        [StringLength(200)]
        [Display(Name = "مكان العمل (خارجي)")]
        public string Workplace { get; set; }

        [StringLength(50)]
        [Display(Name = "الرقم الوظيفي (خارجي)")]
        public string WorkplaceEmployeeId { get; set; }

        [Display(Name = "الراتب الصافي (خارجي)")]
        public decimal? NetSalary { get; set; }

        [StringLength(100)]
        [Display(Name = "البنك (خارجي)")]
        public string BankName { get; set; }

        [StringLength(100)]
        [Display(Name = "رقم الحساب (خارجي)")]
        public string BankAccountNumber { get; set; }

        // --- المرفقات ---
        [Display(Name = "مسار نموذج الكفالة (الموقع)")]
        [StringLength(500)]
        public string GuarantorFormScannedPath { get; set; }
    }
}