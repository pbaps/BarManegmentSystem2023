using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class QuestionViewModel
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }

        [Required(ErrorMessage = "الرجاء اختيار نوع السؤال.")]
        [Display(Name = "نوع السؤال")]
        public int QuestionTypeId { get; set; }
        public SelectList QuestionTypes { get; set; }

        [Required(ErrorMessage = "نص السؤال مطلوب.")]
        [Display(Name = "نص السؤال")]
        [AllowHtml]
        public string QuestionText { get; set; }

        [Display(Name = "الدرجة")]
        [Range(0.1, 100, ErrorMessage = "الدرجة يجب أن تكون قيمة موجبة.")]
        public double Points { get; set; } = 1.0;

        // For Multiple Choice
        public List<AnswerViewModel> Answers { get; set; }
        public int? CorrectAnswerIndex { get; set; }

        // For True/False
        public bool TrueFalseAnswer { get; set; }

        public QuestionViewModel()
        {
            Answers = new List<AnswerViewModel>
            {
                new AnswerViewModel(), new AnswerViewModel(), new AnswerViewModel(), new AnswerViewModel()
            };
        }
    }

    public class AnswerViewModel
    {
        public int Id { get; set; }
        public string AnswerText { get; set; }
    }
}