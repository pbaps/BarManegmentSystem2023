using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class OralExamEnrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المتدرب")]
        public int GraduateApplicationId { get; set; }
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication Trainee { get; set; }

        [Required]
        [Display(Name = "لجنة الاختبار الشفوي")]
        public int OralExamCommitteeId { get; set; }
        [ForeignKey("OralExamCommitteeId")]
        public virtual OralExamCommittee OralExamCommittee { get; set; }

        [Display(Name = "تاريخ التسجيل/الامتحان")]
        [DataType(DataType.Date)]
        public DateTime ExamDate { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "النتيجة")]
        public string Result { get; set; } // "ناجح", "راسب", "لم يحضر"

        [Display(Name = "الدرجة (اختياري)")]
        public double? Score { get; set; }

        [Display(Name = "ملاحظات اللجنة")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
    }
}