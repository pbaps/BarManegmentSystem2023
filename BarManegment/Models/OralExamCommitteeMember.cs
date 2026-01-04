using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class OralExamCommitteeMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OralExamCommitteeId { get; set; }
        [ForeignKey("OralExamCommitteeId")]
        public virtual OralExamCommittee OralExamCommittee { get; set; }

        [Required]
        [Display(Name = "العضو (المحامي)")]
        public int MemberLawyerId { get; set; } // FK لـ GraduateApplication (المحامي المزاول)

        [ForeignKey("MemberLawyerId")]
        public virtual GraduateApplication MemberLawyer { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "الدور في اللجنة")]
        public string Role { get; set; } // مثال: "رئيس اللجنة", "عضو ممتحن"
    }
}