using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class ExamQualification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamApplicationId { get; set; }
        [ForeignKey("ExamApplicationId")]
        public virtual ExamApplication ExamApplication { get; set; }

        [Required]
        [StringLength(100)]
        public string QualificationType { get; set; } // "الثانوية العامة", "البكالوريوس"

        [StringLength(200)]
        public string UniversityName { get; set; } // للبكالوريوس فقط

        [Required]
        public int GraduationYear { get; set; }

        [Required]
        [Range(0, 100)]
        public double GradePercentage { get; set; }
    }
}