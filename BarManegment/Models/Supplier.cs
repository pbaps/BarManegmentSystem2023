using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "اسم المورد")]
        public string Name { get; set; }

        [Display(Name = "رقم الهاتف")]
        public string Phone { get; set; }

        [Display(Name = "العنوان")]
        public string Address { get; set; }

        // الربط المالي (الذمة الدائنة للمورد)
        [Display(Name = "حساب الأستاذ")]
        public int? AccountId { get; set; }

        [ForeignKey("AccountId")]
        public virtual Account Account { get; set; }

        public bool IsActive { get; set; } = true;
    }
}