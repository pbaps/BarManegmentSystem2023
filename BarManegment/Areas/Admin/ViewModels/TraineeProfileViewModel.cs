using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class TraineeProfileViewModel
    {
        public int Id { get; set; }
        public string ArabicName { get; set; }

        // === بداية التعديل: استخدام الرقم المتسلسل النصي ===
        public string TraineeSerialNo { get; set; }
        // === نهاية التعديل ===

        public DateTime? TrainingStartDate { get; set; }
        public int? CurrentSupervisorId { get; set; }
        public string CurrentSupervisorName { get; set; }

        [Display(Name = "اختر المشرف الجديد")]
        [Required(ErrorMessage = "الرجاء اختيار مشرف جديد.")]
        public int NewSupervisorId { get; set; }

        public List<SupervisorHistory> SupervisorHistory { get; set; }
        public List<TraineeAttendance> AttendanceRecords { get; set; }
        public List<Receipt> FinancialRecords { get; set; }
        public double TotalCreditHours { get; set; }
        public double RequiredCreditHours { get; set; }
        public int ProgressPercentage { get; set; }
        // === بداية الإضافة: حقل معرّف تليجرام ===
        [Display(Name = "معرف دردشة تليجرام")]
        public long? TelegramChatId { get; set; }
        // === نهاية الإضافة ===

        public TraineeProfileViewModel()
        {
            SupervisorHistory = new List<SupervisorHistory>();
            AttendanceRecords = new List<TraineeAttendance>();
            FinancialRecords = new List<Receipt>();
        }
    }
}
