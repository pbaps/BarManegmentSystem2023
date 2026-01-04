using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ExamEnrollmentViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        public string ExamTypeName { get; set; }
        public List<CandidateViewModel> Candidates { get; set; }

        public ExamEnrollmentViewModel()
        {
            Candidates = new List<CandidateViewModel>();
        }
    }

    public class CandidateViewModel
    {
        // سيحمل هذا المعرف إما ExamApplicationId أو GraduateApplicationId
        public int ApplicantId { get; set; }
        public string Name { get; set; }
        public string Identifier { get; set; } // الرقم الوطني أو رقم العضوية
        public bool IsEnrolled { get; set; }
    }
}
