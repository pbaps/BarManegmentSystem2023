using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    public class OralExamCommittee
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم اللجنة مطلوب")]
        [StringLength(200)]
        [Display(Name = "اسم/معرف اللجنة")]
        public string CommitteeName { get; set; } // مثال: "لجنة الاختبار الشفوي - دورة 1"

        [Display(Name = "تاريخ التشكيل")]
        [DataType(DataType.Date)]
        public DateTime FormationDate { get; set; }

        [Display(Name = "فعالة")]
        public bool IsActive { get; set; } = true;

        // الأعضاء (المحامون المزاولون)
        public virtual ICollection<OralExamCommitteeMember> Members { get; set; }

        // المتدربون المسجلون في هذه اللجنة
        public virtual ICollection<OralExamEnrollment> Enrollments { get; set; }

        public OralExamCommittee()
        {
            Members = new HashSet<OralExamCommitteeMember>();
            Enrollments = new HashSet<OralExamEnrollment>();
        }
    }
}