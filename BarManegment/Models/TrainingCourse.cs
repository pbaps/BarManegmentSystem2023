using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    // هذا النموذج يمثل الدورة ككل (الموضوع الرئيسي)
    public class TrainingCourse
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الدورة مطلوب")]
        [Display(Name = "اسم الدورة")]
        [StringLength(200)]
        public string CourseName { get; set; }

        [Display(Name = "وصف الدورة")]
        public string Description { get; set; }

        public virtual ICollection<TrainingSession> Sessions { get; set; }
    }
}