using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("FinancialAidTypes")]
    public class FinancialAidType
    {
        public int Id { get; set; }

        [Required, Display(Name = "نوع المساعدة")]
        public string Name { get; set; } // مثال: مساعدة علاجية، طارئة

        [Display(Name = "الحد الأقصى (اختياري)")]
        public decimal? MaxAmount { get; set; }
    }
}