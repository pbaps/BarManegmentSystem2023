using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    // جدول لتسجيل فترات الإيقاف الإداري (بقرار مجلس)
    public class TraineeSuspension
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المتدرب")]
        public int GraduateApplicationId { get; set; }
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication Trainee { get; set; }

        [Required(ErrorMessage = "سبب الإيقاف مطلوب")]
        [Display(Name = "السبب والقرار")]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }

        [Required(ErrorMessage = "تاريخ بدء الإيقاف مطلوب")]
        [Display(Name = "تاريخ البدء")]
        [DataType(DataType.Date)]
        public DateTime SuspensionStartDate { get; set; }

        // === تم التعديل: جعله اختيارياً (Nullable) ===
        [Display(Name = "تاريخ الانتهاء")]
        [DataType(DataType.Date)]
        public DateTime? SuspensionEndDate { get; set; } // (مهم جداً لطلبات الوقف المفتوحة)
        // === نهاية التعديل ===

        [Display(Name = "تاريخ تسجيل القرار")]
        public DateTime DecisionDate { get; set; } = DateTime.Now;

        [Display(Name = "الموظف المسجل")]
        public int? CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public virtual UserModel CreatedByUser { get; set; }
        // ===
        [Required]
        [StringLength(100)]
        [Display(Name = "حالة الطلب")]
        public string Status { get; set; } // (مثل: "بانتظار الموافقة", "معتمد", "مرفوض")
        // === نهاية الإضافة ===
    }
}