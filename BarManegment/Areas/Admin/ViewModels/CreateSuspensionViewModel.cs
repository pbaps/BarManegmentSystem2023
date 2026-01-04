using System;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    // ViewModel لنموذج إضافة إيقاف بقرار
    public class CreateSuspensionViewModel
    {
        [Required]
        public int TraineeId { get; set; }
        public string TraineeName { get; set; } // للعرض

        [Required(ErrorMessage = "سبب الإيقاف مطلوب")]
        [Display(Name = "سبب الإيقاف (رقم القرار)")]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }

        [Required(ErrorMessage = "تاريخ البدء مطلوب")]
        [Display(Name = "من تاريخ")]
        [DataType(DataType.Date)]
        public DateTime SuspensionStartDate { get; set; } = DateTime.Now.Date;

        [Required(ErrorMessage = "تاريخ الانتهاء مطلوب")]
        [Display(Name = "إلى تاريخ")]
        [DataType(DataType.Date)]
        public DateTime SuspensionEndDate { get; set; } = DateTime.Now.Date.AddMonths(1);
    }
}