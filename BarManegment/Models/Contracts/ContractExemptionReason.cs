using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("ContractExemptionReasons")]
    public class ContractExemptionReason
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        [Display(Name = "سبب الإعفاء")]
        public string Reason { get; set; } // (حالة إنسانية، قرار مجلس...)
    }
}