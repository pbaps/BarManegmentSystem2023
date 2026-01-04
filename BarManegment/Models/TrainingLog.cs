using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    // سجل التدريب العملي الشهري
    public class TrainingLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المتدرب")]
        public int GraduateApplicationId { get; set; }
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication Trainee { get; set; }

        [Display(Name = "المشرف (عند التقديم)")]
        public int? SupervisorId { get; set; }
        [ForeignKey("SupervisorId")]
        public virtual GraduateApplication Supervisor { get; set; }

        [Required]
        [Display(Name = "سنة السجل")]
        public int Year { get; set; }

        [Required]
        [Display(Name = "شهر السجل")]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required(ErrorMessage = "يجب ملء ملخص الأعمال")]
        [Display(Name = "ملخص الأعمال (جلسات، لوائح، استشارات)")]
        [DataType(DataType.MultilineText)]
        public string WorkSummary { get; set; }

        [Display(Name = "ملف مرفق (اختياري)")]
        public string FilePath { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // (مثال: "مسودة", "بانتظار موافقة المشرف", "معتمد", "مرفوض")

        [Display(Name = "تاريخ التقديم")]
        public DateTime SubmissionDate { get; set; }

        [Display(Name = "ملاحظات المشرف")]
        [DataType(DataType.MultilineText)]
        public string SupervisorNotes { get; set; }

        [Display(Name = "تاريخ المراجعة")]
        public DateTime? ReviewDate { get; set; }
    }
}