using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class TraineeAttendanceReportViewModel
    {
        public int TraineeId { get; set; }

        [Display(Name = "اسم المتدرب")]
        public string TraineeName { get; set; }

        [Display(Name = "الرقم النقابي")]
        public string TraineeSerialNo { get; set; }

        [Display(Name = "رقم الهوية")]
        public string NationalIdNumber { get; set; }

        public List<AttendedSessionViewModel> AttendedSessions { get; set; }

        [Display(Name = "إجمالي الجلسات")]
        public int TotalSessions { get; set; }

        [Display(Name = "إجمالي الساعات المعتمدة")]
        public double TotalCreditHours { get; set; } // 💡 تم التغيير إلى double

        public TraineeAttendanceReportViewModel()
        {
            AttendedSessions = new List<AttendedSessionViewModel>();
        }
    }

    public class AttendedSessionViewModel
    {
        public string CourseName { get; set; }
        public string SessionTitle { get; set; }
        public DateTime SessionDate { get; set; }
        public string InstructorName { get; set; }
        public double CreditHours { get; set; } // 💡 تم التغيير إلى double
        public string AttendanceStatus { get; set; }
    }
}