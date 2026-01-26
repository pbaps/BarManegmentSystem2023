using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class SupervisorChangeRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المتدرب")]
        public int TraineeId { get; set; }
        [ForeignKey("TraineeId")]
        public virtual GraduateApplication Trainee { get; set; }

        [Required]
        [Display(Name = "نوع الطلب")]
        public string RequestType { get; set; } // "نقل", "وقف", "استئناف"

        [Display(Name = "المشرف القديم")]
        public int? OldSupervisorId { get; set; }
        [ForeignKey("OldSupervisorId")]
        public virtual GraduateApplication OldSupervisor { get; set; }

        [Display(Name = "المشرف الجديد")]
        public int? NewSupervisorId { get; set; }
        [ForeignKey("NewSupervisorId")]
        public virtual GraduateApplication NewSupervisor { get; set; }

        // ✅✅✅ الإضافة الجديدة لحل الخطأ ✅✅✅
        [Display(Name = "سبب الطلب")]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }

        [Required]
        [Display(Name = "تاريخ تقديم الطلب")]
        public DateTime RequestDate { get; set; }

        [Required]
        [Display(Name = "حالة الطلب")]
        public string Status { get; set; } // "قيد المراجعة", "موافق عليه", "مرفوض"

        [Display(Name = "ملاحظات اللجنة")]
        public string CommitteeNotes { get; set; }

        [Display(Name = "تاريخ انتهاء فترة السماح")]
        public DateTime? GracePeriodEndDate { get; set; }

        // حقول للمرفقات
        [Display(Name = "مرفق موافقة المشرف القديم")]
        public string OldSupervisorApprovalPath { get; set; }
        [Display(Name = "مرفق موافقة المشرف الجديد")]
        public string NewSupervisorApprovalPath { get; set; }
        [Display(Name = "تاريخ القرار")]
        public DateTime? DecisionDate { get; set; } // تمت إضافة هذه الخاصية

        // === بداية الإضافة: ربط القسيمة بالطلب ===
        [Display(Name = "رقم قسيمة الدفع")]
        public int? PaymentVoucherId { get; set; }
        [ForeignKey("PaymentVoucherId")]
        public virtual PaymentVoucher PaymentVoucher { get; set; }
        // === نهاية الإضافة ===
    }
}