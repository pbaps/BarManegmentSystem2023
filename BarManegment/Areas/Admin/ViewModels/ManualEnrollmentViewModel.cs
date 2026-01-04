using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ManualEnrollmentViewModel
    {
        [Required]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "الرقم الوطني مطلوب")]
        [Display(Name = "الرقم الوطني للمتقدم")]
        public string NationalIdNumber { get; set; }
    }
}