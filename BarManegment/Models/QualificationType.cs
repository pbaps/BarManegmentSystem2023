using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    public class QualificationType
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "نوع المؤهل")]
        public string Name { get; set; }

        [Display(Name = "الحد الأدنى لنسبة القبول")]
        [Range(0, 100, ErrorMessage = "يجب أن تكون النسبة بين 0 و 100")]
        public double? MinimumAcceptancePercentage { get; set; }

        public virtual ICollection<Qualification> Qualifications { get; set; }
    }
}
