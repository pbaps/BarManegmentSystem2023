using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class TraineeAttendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TraineeId { get; set; } // FK to GraduateApplication
        [ForeignKey("TraineeId")]
        public virtual GraduateApplication Trainee { get; set; }

        [Required]
        public int SessionId { get; set; } // FK to TrainingSession
        [ForeignKey("SessionId")]
        public virtual TrainingSession Session { get; set; }

        [Display(Name = "وقت الحضور")]
        public DateTime? AttendanceTime { get; set; }

        [Display(Name = "مدة الحضور (بالدقائق)")]
        public int? DurationInMinutes { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } // مثال: "حاضر", "غائب"
    }
}