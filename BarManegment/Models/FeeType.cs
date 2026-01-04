
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class FeeType
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الرسم مطلوب")]
        [Display(Name = "اسم الرسم")]
        public string Name { get; set; }

        [Required(ErrorMessage = "قيمة الرسم مطلوبة")]
        [Display(Name = "القيمة الافتراضية")]
        [Range(0, double.MaxValue, ErrorMessage = "القيمة يجب أن تكون موجبة.")]
        public decimal DefaultAmount { get; set; }

        [Required(ErrorMessage = "العملة مطلوبة")]
        [Display(Name = "العملة")]
        public int CurrencyId { get; set; }
        [ForeignKey("CurrencyId")]
        public virtual Currency Currency { get; set; }

        [Required(ErrorMessage = "الحساب البنكي مطلوب")]
        [Display(Name = "الحساب البنكي المخصص")]
        public int BankAccountId { get; set; }
        [ForeignKey("BankAccountId")]
        public virtual BankAccount BankAccount { get; set; }

        [Display(Name = "الحالة")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "نسبة المحامي")]
        [DefaultValue(0)]
        public decimal LawyerPercentage { get; set; } = 0;

        [Display(Name = "نسبة النقابة")]
        [DefaultValue(1)]
        public decimal BarSharePercentage { get; set; } = 1; // (الافتراضي 100%)

        // ربط نوع الرسم بالحساب المحاسبي (الإيراد)
        public int? RevenueAccountId { get; set; }
        [ForeignKey("RevenueAccountId")]
        public virtual Account RevenueAccount { get; set; }

    }
}