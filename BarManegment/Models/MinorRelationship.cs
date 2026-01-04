using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    // هذا الجدول يحدد علاقة القاصر بالموكل (الطرف الأول)
    // (يستبدل PassportGuardianRole)
    [Table("MinorRelationships")]
    public class MinorRelationship
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "صفة القاصر")]
        public string Name { get; set; } // (مثل: ابن، ابنة، حفيد، ابن أخ...)
    }
}