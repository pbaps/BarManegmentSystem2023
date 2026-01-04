// في ملف BarManegment/Models/VoucherDetail.cs

using BarManegment.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// في ملف BarManegment/Models/VoucherDetail.cs
namespace BarManegment.Models
{
    public class VoucherDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PaymentVoucherId { get; set; }
        [ForeignKey("PaymentVoucherId")]
        public virtual PaymentVoucher PaymentVoucher { get; set; }

        [Required]
        [Display(Name = "نوع الرسم")]
        public int FeeTypeId { get; set; }
        [ForeignKey("FeeTypeId")]
        public virtual FeeType FeeType { get; set; }

        // ====> قم بإضافة الأسطر التالية <====
        [Required]
        [Display(Name = "حساب البنك")]
        public int BankAccountId { get; set; }
        [ForeignKey("BankAccountId")]
        public virtual BankAccount BankAccount { get; set; }
        // ====> نهاية الإضافة <====

        [Required]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        // ===
        [Display(Name = "البيان / الوصف")]
        public string Description { get; set; }
        // === نهاية الإضافة ===
    }
}