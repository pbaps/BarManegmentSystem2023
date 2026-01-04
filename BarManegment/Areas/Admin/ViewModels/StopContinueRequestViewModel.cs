using System;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class StopContinueRequestViewModel
    {
        [Required]
        public int TraineeId { get; set; }
        public string TraineeName { get; set; }

        [Required(ErrorMessage = "الرجاء تحديد نوع الإجراء")]
        [Display(Name = "نوع الإجراء")]
        public string RequestType { get; set; } // "وقف" أو "استكمال"

        [Required(ErrorMessage = "الرجاء تحديد التاريخ")]
        [Display(Name = "تاريخ الوقف / الاستكمال المقترح")]
        [DataType(DataType.Date)]
        public DateTime ActionDate { get; set; } = DateTime.Now;

        [Display(Name = "السبب / الملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }
    }
}