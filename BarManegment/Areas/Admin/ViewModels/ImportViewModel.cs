using System.ComponentModel.DataAnnotations;
using System.Web;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ImportViewModel
    {
        [Required(ErrorMessage = "يرجى اختيار ملف Excel")]
        [Display(Name = "ملف البيانات (Excel)")]
        public HttpPostedFileBase File { get; set; }

        [Required]
        [Display(Name = "نوع البيانات")]
        public string EntityType { get; set; }
    }
}