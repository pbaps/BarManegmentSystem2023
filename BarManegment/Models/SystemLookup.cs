using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    public class SystemLookup
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "الفئة")]
        public string Category { get; set; } // مثال: PaymentMethod, ExpenseType

        [Required]
        [Display(Name = "الاسم")]
        public string Name { get; set; } // مثال: نقدي، شيك، حوالة

        [Display(Name = "فعال؟")]
        public bool IsActive { get; set; } = true;
    }
}