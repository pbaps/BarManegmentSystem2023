using BarManegment.Models;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class TrainingCommitteeIndexViewModel
    {
        public string SearchTerm { get; set; }

        // القسم الأول: بانتظار الموافقة النهائية من اللجنة
        public List<GraduateApplication> AwaitingCommitteeApprovalApplications { get; set; }

        // القسم الثاني: بانتظار استكمال النواقص من الخريج
        public List<GraduateApplication> AwaitingCompletionApplications { get; set; }

        // القسم الثالث: تمت الموافقة عليها وتنتظر الدفع
        public List<GraduateApplication> ApprovedApplications { get; set; }
 
        public List<ExamApplication> ExemptedApplications { get; set; }

        public TrainingCommitteeIndexViewModel()
        {
            AwaitingCommitteeApprovalApplications = new List<GraduateApplication>();
            AwaitingCompletionApplications = new List<GraduateApplication>();
            ApprovedApplications = new List<GraduateApplication>();
            ExemptedApplications = new List<ExamApplication>();
        }
    }
}