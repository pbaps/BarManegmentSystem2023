using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("LoanInstallments")]
    public class LoanInstallment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "طلب القرض")]
        public int LoanApplicationId { get; set; }
        [ForeignKey("LoanApplicationId")]
        public virtual LoanApplication LoanApplication { get; set; }

        [Required]
        [Display(Name = "رقم القسط")]
        public int InstallmentNumber { get; set; } // (1, 2, 3...)

        [Required]
        [Display(Name = "تاريخ الاستحقاق")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        [Display(Name = "قيمة القسط")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "حالة القسط")]
        public string Status { get; set; } // (مستحق، مدفوع، متأخر)

        [Display(Name = "مسار الكمبيالة (الخاصة بالقسط)")]
        [StringLength(500)]
        public string PromissoryNoteScannedPath { get; set; }

        // --- الربط المالي (الأهم) ---

        [Display(Name = "قسيمة الدفع")]
        public int? PaymentVoucherId { get; set; }
        [ForeignKey("PaymentVoucherId")]
        public virtual PaymentVoucher PaymentVoucher { get; set; }

        [Display(Name = "إيصال القبض")]
        public int? ReceiptId { get; set; }
        [ForeignKey("ReceiptId")]
        public virtual Receipt Receipt { get; set; }
    }
}