using System.ComponentModel.DataAnnotations;

// === بداية التعديل: التأكد من أن هذا هو المسار الصحيح ===
namespace BarManegment.Models
{
    public class Gender
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "النوع")]
        public string Name { get; set; }
    }
}