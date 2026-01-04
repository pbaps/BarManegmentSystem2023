using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class CostCenter
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "رمز المركز")]
        public string Code { get; set; } // مثال: 101

        [Required]
        [Display(Name = "اسم المركز")]
        public string Name { get; set; } // مثال: لجنة التدريب

        [Display(Name = "المركز الرئيسي")]
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual CostCenter Parent { get; set; }

        public virtual ICollection<CostCenter> Children { get; set; }
    }
}