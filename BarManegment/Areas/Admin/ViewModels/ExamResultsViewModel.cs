using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ExamResultsViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }
        // === بداية الإضافة ===
        public DateTime ExamDate { get; set; }
        public double TotalPossibleScore { get; set; }
        // === نهاية الإضافة ===
        public List<EnrolledCandidateResultViewModel> Candidates { get; set; }

        public ExamResultsViewModel()
        {
            Candidates = new List<EnrolledCandidateResultViewModel>();
        }
    }

    public class EnrolledCandidateResultViewModel
    {
        public int EnrollmentId { get; set; }
        public string ApplicantName { get; set; }
        public string ApplicantIdentifier { get; set; }
        [Display(Name = "الدرجة")]
        public double? Score { get; set; }
        public string Result { get; set; }
        public bool IsSelected { get; set; }
        public string ContactEmail { get; set; }
        public string ContactMobile { get; set; }
        public long? TelegramChatId { get; set; } // <-- تمت الإضافة
 
        public string WhatsAppNumber { get; set; }
    }
}
