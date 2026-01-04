using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace BarManegment.Models
{
    public class Currency
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم العملة مطلوب")]
        [Display(Name = "اسم العملة")]
        [StringLength(50)]
        public string Name { get; set; } // مثال: شيكل إسرائيلي جديد

        [Required(ErrorMessage = "رمز العملة مطلوب")]
        [Display(Name = "رمز العملة")]
        [StringLength(10)]
        public string Symbol { get; set; } // مثال: ₪, $, JD
    }
}