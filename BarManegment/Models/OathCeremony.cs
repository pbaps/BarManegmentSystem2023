using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    public class OathCeremony ///لإدارة مواعيد أداء اليمين
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "تاريخ الموعد مطلوب")]
        [Display(Name = "تاريخ موعد اليمين")]
        [DataType(DataType.Date)]
        public DateTime CeremonyDate { get; set; }

        [StringLength(300)]
        [Display(Name = "الموقع / ملاحظات")]
        public string Location { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; } = true; // لتحديد المواعيد المتاحة

        // المتدربون الذين سيؤدون اليمين في هذا الموعد
        public virtual ICollection<GraduateApplication> Attendees { get; set; }

        public OathCeremony()
        {
            Attendees = new HashSet<GraduateApplication>();
        }
    }
}