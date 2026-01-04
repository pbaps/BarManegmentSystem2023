using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    public class ApplicationStatus
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(50)]
        public string Name { get; set; } // مثال: قيد المراجعة، مقبول، مرفوض
        public virtual ICollection<GraduateApplication> GraduateApplications { get; set; }
    }
}
