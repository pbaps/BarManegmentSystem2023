using System;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class CourseDetailsViewModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public int SessionId { get; set; }
        public string SessionTitle { get; set; }
        public DateTime SessionDate { get; set; }
        public List<TraineeAttendanceViewModel> Trainees { get; set; }

        public CourseDetailsViewModel()
        {
            Trainees = new List<TraineeAttendanceViewModel>();
        }
    }

    public class TraineeAttendanceViewModel
    {
        public int TraineeId { get; set; }
        // === تم استبدال الرقم المتسلسل برقم الطلب ===
        public string TraineeSerialNo { get; set; }
        public string TraineeName { get; set; }
        public bool IsAttended { get; set; }
    }
}
