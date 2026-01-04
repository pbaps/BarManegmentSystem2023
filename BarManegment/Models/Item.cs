using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "اسم الصنف")]
        public string Name { get; set; }

        [Display(Name = "الكود")]
        public string Code { get; set; } // Barcode or SKU

        [Display(Name = "التصنيف")]
        public int ItemCategoryId { get; set; }
        [ForeignKey("ItemCategoryId")]
        public virtual ItemCategory ItemCategory { get; set; }

        [Display(Name = "الكمية الحالية")]
        public int CurrentQuantity { get; set; } = 0;

        [Display(Name = "متوسط التكلفة")]
        public decimal AverageCost { get; set; } = 0; // متوسط سعر الوحدة

        [Display(Name = "حد الطلب")]
        public int ReorderLevel { get; set; } = 5; // للتنبيه عند نقص المخزون

        public bool IsActive { get; set; } = true;
    }
}