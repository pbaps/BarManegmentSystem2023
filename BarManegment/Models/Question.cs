using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; }

        [Required]
        public int QuestionTypeId { get; set; }
        [ForeignKey("QuestionTypeId")]
        public virtual QuestionType QuestionType { get; set; }

        [Required]
        public string QuestionText { get; set; }

        [Display(Name = "الدرجة المخصصة للسؤال")]
        public double Points { get; set; } = 1.0;

        public virtual ICollection<Answer> Answers { get; set; }
    }
}
