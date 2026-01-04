using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    public class NationalIdType
    {
        [Key]
        public int Id { get; set; }
        [Display(Name = "اسم نوع الهوية")]
        [Required, StringLength(50)]
        public string Name { get; set; }
        public virtual ICollection<GraduateApplication> GraduateApplications { get; set; }
    }
}

