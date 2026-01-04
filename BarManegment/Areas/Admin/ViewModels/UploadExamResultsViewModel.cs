using System.ComponentModel.DataAnnotations;
using System.Web;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class UploadExamResultsViewModel
    {
        [Required]
        public int ExamId { get; set; }
        public string ExamTitle { get; set; }

        [Required(ErrorMessage = "الرجاء اختيار ملف إكسل.")]
        [Display(Name = "ملف النتائج (Excel)")]
        public HttpPostedFileBase UploadedFile { get; set; }
    }
}