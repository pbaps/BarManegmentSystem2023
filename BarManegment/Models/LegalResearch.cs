using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class LegalResearch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المتدرب")]
        public int GraduateApplicationId { get; set; }
        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication Trainee { get; set; }

        [Required(ErrorMessage = "عنوان البحث مطلوب")]
        [StringLength(500)]
        [Display(Name = "عنوان البحث")]
        public string Title { get; set; }

        [Display(Name = "تاريخ التقديم")]
        [DataType(DataType.Date)]
        public DateTime SubmissionDate { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "حالة البحث")]
        public string Status { get; set; } // مثال: "مُقدم", "تم تعيين لجنة", "بانتظار القرار", "مقبول", "مرفوض"

        [Display(Name = "مسار ملف البحث النهائي")]
        [StringLength(500)]
        public string FinalDocumentPath { get; set; }

        // --- التعديل: ربط البحث باللجنة ---
        [Display(Name = "لجنة المناقشة")]
        public int? DiscussionCommitteeId { get; set; } // Nullable للسماح بتعيينها لاحقاً

        [ForeignKey("DiscussionCommitteeId")]
        public virtual DiscussionCommittee Committee { get; set; } // علاقة باللجنة

      // علاقة البحث بالقرارات (البحث الواحد قد يكون له عدة قرارات بسبب التعديلات)
        public virtual ICollection<CommitteeDecision> Decisions { get; set; }
        public LegalResearch()
        {
            // ...
            Decisions = new HashSet<CommitteeDecision>();
        }
    }
}