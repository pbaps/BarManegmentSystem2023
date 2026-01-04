using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class ManualGrade
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TraineeAnswerId { get; set; }
        [ForeignKey("TraineeAnswerId")]
        public virtual TraineeAnswer TraineeAnswer { get; set; }

        [Required]
        public int GraderId { get; set; }
        [ForeignKey("GraderId")]
        public virtual UserModel Grader { get; set; }

        [StringLength(50)]
        public string Status { get; set; } // "معين", "تم التصحيح"
    }
}
