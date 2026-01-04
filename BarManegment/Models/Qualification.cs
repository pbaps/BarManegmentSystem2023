using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class Qualification
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "نوع المؤهل مطلوب")]
        [Display(Name = "نوع المؤهل")]
        public int QualificationTypeId { get; set; }
        [ForeignKey("QualificationTypeId")]
        public virtual QualificationType QualificationType { get; set; }

        [Required(ErrorMessage = "اسم الجامعة مطلوب")]
        [Display(Name = "اسم الجامعة / المدرسة")]
        [StringLength(200)]
        public string UniversityName { get; set; }

        // === بداية الإضافة ===
        [Display(Name = "الكلية")]
        [StringLength(200)]
        public string Faculty { get; set; }

        [Display(Name = "التخصص")]
        [StringLength(200)]
        public string Specialization { get; set; }
        // === نهاية الإضافة ===

        [Required(ErrorMessage = "سنة التخرج مطلوبة")]
        [Display(Name = "سنة التخرج")]
        public int GraduationYear { get; set; }

        [Display(Name = "الدرجة / المعدل")]
        [Range(0, 100, ErrorMessage = "يجب أن تكون النسبة بين 0 و 100")]
        public double? GradePercentage { get; set; }

        [Required]
        public int GraduateApplicationId { get; set; }
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication GraduateApplication { get; set; }
    }
}
