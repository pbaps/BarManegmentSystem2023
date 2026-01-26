using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("ContractTypes")]
    public class ContractType
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم نوع العقد مطلوب")]
        [Display(Name = "نوع العقد")]
        [StringLength(200)]
        public string Name { get; set; }

        [Required(ErrorMessage = "الرسوم الافتراضية مطلوبة")]
        [Display(Name = "الرسوم الافتراضية")]
        public decimal DefaultFee { get; set; }

        // إضافة للتحكم في نوع الاحتساب (ثابت أم نسبة)
        [Display(Name = "هل الرسم ثابت؟")]
        public bool IsFixedFee { get; set; } = true;

        [Display(Name = "نسبة الرسم (إذا لم يكن ثابتاً)")]
        public decimal Percentage { get; set; } // مثال 0.005

        [Required]
        [Display(Name = "العملة")]
        public int CurrencyId { get; set; }
        [ForeignKey("CurrencyId")]
        public virtual Currency Currency { get; set; }

        [Required(ErrorMessage = "حصة المحامي مطلوبة")]
        [Display(Name = "نسبة حصة المحامي (مثال: 0.60)")]
        public decimal LawyerPercentage { get; set; }

        [Required(ErrorMessage = "حصة النقابة مطلوبة")]
        [Display(Name = "نسبة حصة النقابة (مثال: 0.40)")]
        public decimal BarSharePercentage { get; set; }
    }
}