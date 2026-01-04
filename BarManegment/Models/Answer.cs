// في ملف Answer.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class Answer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }
        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; }

        [Required]
        public string AnswerText { get; set; }

        public bool IsCorrect { get; set; }
    }
}