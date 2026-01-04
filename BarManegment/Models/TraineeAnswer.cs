using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class TraineeAnswer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamEnrollmentId { get; set; }
        [ForeignKey("ExamEnrollmentId")]
        public virtual ExamEnrollment ExamEnrollment { get; set; }

        [Required]
        public int QuestionId { get; set; }
        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; }

        // للإجابات من نوع اختيار من متعدد أو صح/خطأ
        public int? SelectedAnswerId { get; set; }
        [ForeignKey("SelectedAnswerId")]
        public virtual Answer SelectedAnswer { get; set; }

        // للإجابات المقالية
        public string EssayAnswerText { get; set; }

        public double? Score { get; set; } // الدرجة التي حصل عليها
    }
}
