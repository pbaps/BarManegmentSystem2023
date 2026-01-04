using System.ComponentModel.DataAnnotations;
using BarManegment.Models;
using System.Collections.Generic;
using System;

namespace BarManegment.Areas.ExamPortal.ViewModels
{
    public class ExamLoginViewModel
    {
        [Required(ErrorMessage = "الرقم الوطني مطلوب")]
        [Display(Name = "الرقم الوطني (اسم المستخدم)")]
        public string NationalIdNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [Display(Name = "كلمة المرور")] // <-- تم التعديل
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class ExamDashboardViewModel
    {
        public string TraineeName { get; set; }
        public string ExamTitle { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsSubmitted { get; set; }
        public string Result { get; set; }
        public int DurationInMinutes { get; internal set; }

        // قائمة بالامتحانات المتاحة الآن أو القادمة
        public List<ExamEnrollment> ActiveExams { get; set; }

        // قائمة بالامتحانات التي تم تقديمها أو انتهى وقتها
        public List<ExamEnrollment> FinishedExams { get; set; }

        public ExamDashboardViewModel()
        {
            ActiveExams = new List<ExamEnrollment>();
            FinishedExams = new List<ExamEnrollment>();
        }

    }

    public class TakeExamViewModel
    {
        public int EnrollmentId { get; set; }
        public string ExamTitle { get; set; }
        public int TotalQuestions { get; set; }
        public int CurrentQuestionIndex { get; set; }
        public Question CurrentQuestion { get; set; }
        public TraineeAnswer SavedAnswer { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class ExamResultViewModel
    {
        public string ExamTitle { get; set; }
        public bool ShowResultInstantly { get; set; }
        public string Result { get; set; }
        public double? Score { get; set; }
        public double TotalPossibleScore { get; set; }
    }
}
