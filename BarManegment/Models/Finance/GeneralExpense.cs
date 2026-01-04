using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class GeneralExpense
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "رقم السند")]
        public string VoucherNumber { get; set; } // تسلسل خاص بسندات الصرف

        [Required]
        [Display(Name = "تاريخ الصرف")]
        public DateTime ExpenseDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "المستفيد (المدفوع له)")]
        public string PayeeName { get; set; } // شركة الكهرباء، الموظف فلان، إلخ

        [Required]
        [Display(Name = "المبلغ")]
        [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن يكون المبلغ أكبر من صفر")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "البيان (الشرح)")]
        public string Description { get; set; }

        // --- الطرف المدين (المصروف) ---
        [Required]
        [Display(Name = "حساب المصروف")]
        public int ExpenseAccountId { get; set; }

        [ForeignKey("ExpenseAccountId")]
        public virtual Account ExpenseAccount { get; set; }

        [Display(Name = "مركز التكلفة")]
        public int? CostCenterId { get; set; }
        public virtual CostCenter CostCenter { get; set; }

        // --- الطرف الدائن (مصدر المال) ---
        [Required]
        [Display(Name = "طريقة الدفع")]
        public string PaymentMethod { get; set; } // نقدي / شيك / حوالة

        [Display(Name = "حساب الخزينة/البنك")]
        public int TreasuryAccountId { get; set; } // الحساب الذي نقصت منه الأموال

        [ForeignKey("TreasuryAccountId")]
        public virtual Account TreasuryAccount { get; set; }

        [Display(Name = "رقم الشيك/الحوالة")]
        public string ReferenceNumber { get; set; }

        // بيانات النظام
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPosted { get; set; } // هل تم توليد القيد؟
    }
}