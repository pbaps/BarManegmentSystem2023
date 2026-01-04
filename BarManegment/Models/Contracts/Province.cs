using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("Provinces")]
    public class Province
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        [Display(Name = "اسم المحافظة")]
        public string Name { get; set; }
    }
}