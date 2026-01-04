using System.ComponentModel.DataAnnotations;
using System.Web; // Required for HttpPostedFileBase

namespace BarManegment.Areas.Admin.ViewModels
{
    public class OathRequestCreateViewModel
    {
        [Required]
        public int TraineeId { get; set; }
        public string TraineeName { get; set; } // للعرض في الواجهة

        [Required(ErrorMessage = "الرجاء إرفاق نموذج انتهاء التمرين (7 أوراق).")]
        [Display(Name = "نموذج انتهاء التمرين")]
        public HttpPostedFileBase CompletionFormFile { get; set; }

        [Required(ErrorMessage = "الرجاء إرفاق شهادة المشرف.")]
        [Display(Name = "شهادة المشرف")]
        public HttpPostedFileBase SupervisorCertificateFile { get; set; }
    }
}