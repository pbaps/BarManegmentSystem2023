using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    // جدول لتسجيل الرسوم المؤجلة بقرار استثنائي
    public class DeferredFee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المتدرب")]
        public int GraduateApplicationId { get; set; }
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication Trainee { get; set; }

        [Required]
        [Display(Name = "نوع الرسم")]
        public int FeeTypeId { get; set; }
        [ForeignKey("FeeTypeId")]
        public virtual FeeType FeeType { get; set; }

        [Required]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "البيان/السبب")]
        public string Reason { get; set; } // مثال: "رسوم نقل إشراف مؤجلة"

        [Display(Name = "تاريخ التأجيل")]
        public DateTime DateDeferred { get; set; } = DateTime.Now;

        [Display(Name = "تمت إضافته للقسيمة")]
        public bool IsCharged { get; set; } = false; // (لمعرفة إذا تمت إضافته لقسيمة اليمين)

        [Display(Name = "رقم قسيمة اليمين")]
        public int? OathPaymentVoucherId { get; set; }
        [ForeignKey("OathPaymentVoucherId")]
        public virtual PaymentVoucher OathPaymentVoucher { get; set; }
    }
}