using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{    // نموذج عرض لصفحة قائمة التصحيح
    public class ManualGradingIndexViewModel
    {
        public int ManualGradeId { get; set; }
        public string ExamTitle { get; set; }
        public string ApplicantName { get; set; }
        public string GraderName { get; set; } // اسم المصحح المعين
        public int GraderId { get; set; }
    }
    // نموذج عرض لصفحة تعيين المصححين
    public class AssignGradersViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        public List<GraderAssignmentViewModel> Graders { get; set; }

        public AssignGradersViewModel()
        {
            Graders = new List<GraderAssignmentViewModel>();
        }
    }

    public class GraderAssignmentViewModel
    {
        public int GraderId { get; set; }
        public string GraderName { get; set; }
        public bool IsAssigned { get; set; }
    }

    // نموذج عرض لصفحة وضع الدرجة
    // نموذج عرض لصفحة وضع الدرجة (يبقى كما هو)
    public class GradeEssayViewModel
    {
        public int ManualGradeId { get; set; }
        public string QuestionText { get; set; }
        public string EssayAnswer { get; set; }
        public double QuestionPoints { get; set; }

        [Required]
        [Range(0, 1000)]
        [Display(Name = "الدرجة الممنوحة")]
        public double Score { get; set; }
    }
}
 
