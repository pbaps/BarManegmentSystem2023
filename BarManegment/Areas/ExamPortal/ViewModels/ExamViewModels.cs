using System.ComponentModel.DataAnnotations;
using BarManegment.Models;
using System.Collections.Generic;
using System;

namespace BarManegment.Areas.ExamPortal.ViewModels
{
    // 1. فيو موديل تسجيل الدخول
    public class ExamLoginViewModel
    {
        [Required(ErrorMessage = "الرقم الوطني مطلوب")]
        [Display(Name = "الرقم الوطني (اسم المستخدم)")]
        public string NationalIdNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [Display(Name = "كلمة المرور")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    // 2. فيو موديل لوحة التحكم (Dashboard)
    public class ExamDashboardViewModel
    {
        public string TraineeName { get; set; }

        // ✅ تم إزالة التكرار واستخدام النوع الصحيح (EnrolledExamViewModel)
        public List<EnrolledExamViewModel> ActiveExams { get; set; }
        public List<EnrolledExamViewModel> FinishedExams { get; set; }

        public ExamDashboardViewModel()
        {
            ActiveExams = new List<EnrolledExamViewModel>();
            FinishedExams = new List<EnrolledExamViewModel>();
        }
    }

    // 3. ✅ الكلاس الجديد المطلوب للقوائم (كان مفقوداً)
    public class EnrolledExamViewModel
    {
        public int Id { get; set; } // EnrollmentId
        public Exam Exam { get; set; } // بيانات الامتحان
        public string Result { get; set; } // النتيجة
        public double? Score { get; set; } // العلامة
    }

    // 4. فيو موديل تقديم الامتحان
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

    // 5. فيو موديل النتيجة
    public class ExamResultViewModel
    {
        public string ExamTitle { get; set; }
        public bool ShowResultInstantly { get; set; }
        public string Result { get; set; }
        public double? Score { get; set; }
        public double TotalPossibleScore { get; set; }
    }
}