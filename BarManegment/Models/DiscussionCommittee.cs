using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class DiscussionCommittee
    {
        [Key] // المفتاح الأساسي للجنة
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم اللجنة مطلوب")]
        [StringLength(200)]
        [Display(Name = "اسم/معرف اللجنة")]
        public string CommitteeName { get; set; }

        [Display(Name = "تاريخ تشكيل اللجنة")]
        [DataType(DataType.Date)]
        public DateTime FormationDate { get; set; }

        // --- حذف تاريخ المناقشة من هنا (يخص كل بحث على حدة) ---
        // public DateTime? DiscussionDate { get; set; }

        // --- التعديل: علاقة One-to-Many مع الأبحاث ---
        public virtual ICollection<LegalResearch> Researches { get; set; } // الأبحاث المعينة لهذه اللجنة

        // علاقة بأعضاء اللجنة
        public virtual ICollection<CommitteeMember> Members { get; set; }

        // --- حذف علاقة القرار من هنا ---
        // public virtual CommitteeDecision Decision { get; set; }

        [Display(Name = "فعالة")]
        public bool IsActive { get; set; } = true;

        public DiscussionCommittee()
        {
            Members = new HashSet<CommitteeMember>();
            Researches = new HashSet<LegalResearch>(); // تهيئة قائمة الأبحاث
        }
    }
}