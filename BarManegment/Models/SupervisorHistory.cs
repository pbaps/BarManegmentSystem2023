using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class SupervisorHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GraduateApplicationId { get; set; }
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication GraduateApplication { get; set; }

        public int? OldSupervisorId { get; set; }
        [ForeignKey("OldSupervisorId")]
        public virtual GraduateApplication OldSupervisor { get; set; }

        [Required]
        public int NewSupervisorId { get; set; }
        [ForeignKey("NewSupervisorId")]
        public virtual GraduateApplication NewSupervisor { get; set; }

        public DateTime ChangeDate { get; set; }
        public string Reason { get; set; }
    }
}
