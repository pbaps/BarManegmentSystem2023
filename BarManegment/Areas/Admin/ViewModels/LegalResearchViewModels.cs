using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class SubmitResearchViewModel
    {
        [Required]
        public int GraduateApplicationId { get; set; }

        [Display(Name = "اسم المتدرب")]
        public string TraineeName { get; set; }

        [Required(ErrorMessage = "عنوان البحث مطلوب")]
        [StringLength(500, ErrorMessage = "العنوان طويل جدًا")]
        [Display(Name = "عنوان البحث القانوني")]
        public string Title { get; set; }

        [Required(ErrorMessage = "تاريخ التقديم مطلوب")]
        [Display(Name = "تاريخ تقديم البحث")]
        [DataType(DataType.Date)]
        public DateTime SubmissionDate { get; set; } = DateTime.Now;
    }

    public class AssignCommitteeViewModel
    {
        [Required]
        public int ResearchId { get; set; }

        [Display(Name = "عنوان البحث")]
        public string ResearchTitle { get; set; }

        [Display(Name = "اسم المتدرب")]
        public string TraineeName { get; set; }

        [Required(ErrorMessage = "يجب اختيار لجنة مناقشة")]
        [Display(Name = "لجنة المناقشة")]
        public int SelectedCommitteeId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التشكيل")]
        public DateTime FormationDate { get; set; }

        public IEnumerable<SelectListItem> AvailableCommittees { get; set; }
    }

    public class RecordDecisionViewModel
    {
        [Required]
        public int ResearchId { get; set; }

        [Display(Name = "عنوان البحث")]
        public string ResearchTitle { get; set; }

        [Display(Name = "اسم المتدرب")]
        public string TraineeName { get; set; }

        public int CommitteeId { get; set; }

        [Required(ErrorMessage = "الرجاء تحديد نتيجة المناقشة")]
        [Display(Name = "النتيجة")]
        public string Result { get; set; }

        [Required(ErrorMessage = "الرجاء تحديد تاريخ القرار")]
        [Display(Name = "تاريخ القرار")]
        [DataType(DataType.Date)]
        public DateTime DecisionDate { get; set; } = DateTime.Now;

        [Display(Name = "ملاحظات اللجنة التفصيلية")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
    }
}