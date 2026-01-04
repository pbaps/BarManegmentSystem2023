using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("FeeDistributions")]
    public class FeeDistribution
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "إيصال القبض")]
        public int ReceiptId { get; set; }
        [ForeignKey("ReceiptId")]
        public virtual Receipt Receipt { get; set; }

        [Required]
        [Display(Name = "المعاملة")]
        public int ContractTransactionId { get; set; }
        [ForeignKey("ContractTransactionId")]
        public virtual ContractTransaction ContractTransaction { get; set; }

        [Display(Name = "المحامي المستفيد")]
        public int? LawyerId { get; set; } // (Null إذا كانت حصة نقابة)
        [ForeignKey("LawyerId")]
        public virtual GraduateApplication Lawyer { get; set; }

        [Required]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "نوع الحصة")]
        [StringLength(100)]
        public string ShareType { get; set; } // (مثال: "حصة محامي", "حصة نقابة")

        [Required]
        [Display(Name = "أرسلت للبنك؟")]
        [DefaultValue(false)]
        public bool IsSentToBank { get; set; } = false;

        [Display(Name = "تاريخ الإرسال للبنك")]
        public DateTime? BankSendDate { get; set; }

        // 💡💡 === بداية الإضافة === 💡💡
        [Required]
        [Display(Name = "معاملة محجوزة؟")]
        [DefaultValue(false)]
        public bool IsOnHold { get; set; } = false;

        [Display(Name = "سبب الحجز")]
        [StringLength(500)]
        public string HoldReason { get; set; } // (مثال: "مستحقات قرض", "مراجعة إدارية")
        // 💡💡 === نهاية الإضافة === 💡💡
    }
}