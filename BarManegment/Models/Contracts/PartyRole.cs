using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("PartyRoles")]
    public class PartyRole
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        [Display(Name = "صفة الطرف")]
        public string Name { get; set; } // (بائع، مشتري، موكل...)
    }
}