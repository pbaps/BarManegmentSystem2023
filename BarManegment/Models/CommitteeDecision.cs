using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class CommitteeDecision
    {
        [Key] // مفتاح أساسي مستقل للقرار
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // <-- أضف هذا السطر
        public int Id { get; set; }

        // --- هذا هو المفتاح الأجنبي المطلوب ---
        [Required]
        [Display(Name = "البحث")]
        public int LegalResearchId { get; set; } // ربط بالبحث

        [ForeignKey("LegalResearchId")]
        public virtual LegalResearch LegalResearch { get; set; } // علاقة بالبحث
        // --- نهاية الإضافة ---

        [Required, StringLength(100)]
        [Display(Name = "نتيجة المناقشة")]
        public string Result { get; set; }

        [Display(Name = "تاريخ القرار")]
        [DataType(DataType.Date)]
        public DateTime DecisionDate { get; set; }

        [Display(Name = "ملاحظات اللجنة")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
    }
}