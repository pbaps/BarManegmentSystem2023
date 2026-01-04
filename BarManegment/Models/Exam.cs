using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class Exam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamTypeId { get; set; }
        [ForeignKey("ExamTypeId")]
        public virtual ExamType ExamType { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } // مثال: "امتحان القبول - دورة أكتوبر 2025"

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationInMinutes { get; set; }
        public bool IsActive { get; set; } = false;
        public bool ShowResultInstantly { get; set; } = false;
        // === بداية الإضافة: حقل نسبة النجاح ===
        [Required(ErrorMessage = "نسبة النجاح مطلوبة")]
        [Display(Name = "نسبة النجاح (%)")]
        [Range(1, 100, ErrorMessage = "النسبة يجب أن تكون بين 1 و 100.")]
        public double PassingPercentage { get; set; } = 50; // قيمة افتراضية 50%
                                                            // === نهاية الإضافة ===
                                                            // === 💡 الإضافة الجديدة: شروط الاختبار الوظيفي ===

        [Display(Name = "الحد الأدنى لسنوات المزاولة")]
        public int? MinPracticeYears { get; set; } // (اختياري: يترك فارغاً للامتحانات العادية)

        [Display(Name = "الحالة المهنية المطلوبة")]
        public int? RequiredApplicationStatusId { get; set; }

        [ForeignKey("RequiredApplicationStatusId")]
        public virtual ApplicationStatus RequiredApplicationStatus { get; set; }

        [Display(Name = "ملاحظات الشروط")]
        [StringLength(500)]
        public string RequirementsNote { get; set; } // نص يظهر للمستخدم (مثلاً: يجب أن يكون مسدداً للرسوم)

        // === نهاية الإضافة ===
        public virtual ICollection<Question> Questions { get; set; }
        public virtual ICollection<ExamEnrollment> Enrollments { get; set; }
    }
}