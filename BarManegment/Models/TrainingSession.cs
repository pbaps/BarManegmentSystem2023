using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class TrainingSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TrainingCourseId { get; set; }
        [ForeignKey("TrainingCourseId")]
        public virtual TrainingCourse TrainingCourse { get; set; }

        [Required(ErrorMessage = "عنوان الجلسة مطلوب")]
        [Display(Name = "عنوان الجلسة / المحاضرة")]
        [StringLength(200)]
        public string SessionTitle { get; set; }

        [Display(Name = "اسم المحاضر")]
        [StringLength(150)]
        public string InstructorName { get; set; }

        [Required(ErrorMessage = "تاريخ الجلسة مطلوب")]
        [Display(Name = "تاريخ الانعقاد")]
        [DataType(DataType.DateTime)]
        public DateTime SessionDate { get; set; }

        [Required(ErrorMessage = "عدد الساعات مطلوب")]
        [Display(Name = "الساعات المعتمدة")]
        [Range(0.5, 100)]
        public double CreditHours { get; set; }

        // حقول خاصة بربط Microsoft Teams
        [Display(Name = "رابط اجتماع Teams")]
        public string TeamsMeetingUrl { get; set; }

        [Display(Name = "معرف الاجتماع (Meeting ID)")]
        public string TeamsMeetingId { get; set; }

        public virtual ICollection<TraineeAttendance> Attendances { get; set; }
    }
}