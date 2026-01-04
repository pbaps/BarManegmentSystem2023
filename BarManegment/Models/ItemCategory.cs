using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    public class ItemCategory
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "اسم التصنيف")]
        public string Name { get; set; }
    }
}