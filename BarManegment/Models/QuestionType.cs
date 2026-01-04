using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    public class QuestionType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } // "اختيار من متعدد", "صح / خطأ", "مقالي"
    }
}
