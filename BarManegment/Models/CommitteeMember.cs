using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class CommitteeMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DiscussionCommitteeId { get; set; }
        [ForeignKey("DiscussionCommitteeId")]
        public virtual DiscussionCommittee DiscussionCommittee { get; set; }

        // --- الأسطر المُصححة ---
        [Required]
        [Display(Name = "العضو (المحامي)")]
        public int MemberLawyerId { get; set; } // استخدم هذا الاسم

        [ForeignKey("MemberLawyerId")]
        public virtual GraduateApplication MemberLawyer { get; set; } // اربط بـ GraduateApplication
        // --- نهاية التصحيح ---

        [Required, StringLength(100)]
        [Display(Name = "الدور في اللجنة")]
        public string Role { get; set; }
    }
}