using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class OathRequest ///لتتبع طلب أداء اليمين
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المتدرب")]
        public int GraduateApplicationId { get; set; }
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication Trainee { get; set; }

        [Display(Name = "تاريخ تقديم الطلب")]
        [DataType(DataType.Date)]
        public DateTime RequestDate { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "حالة الطلب")]
        public string Status { get; set; } // بانتظار رفع النماذج، بانتظار موافقة اللجنة، بانتظار الدفع، بانتظار تحديد موعد، مكتمل

        [Display(Name = "مسار نموذج انتهاء التمرين (7 أوراق)")]
        [StringLength(500)]
        public string CompletionFormPath { get; set; }

        [Display(Name = "مسار شهادة المشرف")]
        [StringLength(500)]
        public string SupervisorCertificatePath { get; set; }

        [Display(Name = "ملاحظات اللجنة")]
        [DataType(DataType.MultilineText)]
        public string CommitteeNotes { get; set; }

        // لربط الطلب بالقسيمة المالية الخاصة به
        public int? PaymentVoucherId { get; set; }
        [ForeignKey("PaymentVoucherId")]
        public virtual PaymentVoucher PaymentVoucher { get; set; }
    }
}