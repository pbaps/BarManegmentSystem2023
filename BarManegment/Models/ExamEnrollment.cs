using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class ExamEnrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; }

        // يمكن أن يكون المتقدم إما من طلبات الامتحان أو من طلبات الخريجين (المتدربين)
        public int? ExamApplicationId { get; set; }
        [ForeignKey("ExamApplicationId")]
        public virtual ExamApplication ExamApplication { get; set; }

        public int? GraduateApplicationId { get; set; }
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication GraduateApplication { get; set; }

        public double? Score { get; set; }
        public string Result { get; set; } // "ناجح", "راسب", "لم يقدم"
    }
}