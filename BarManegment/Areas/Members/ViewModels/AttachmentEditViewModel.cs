using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Members.ViewModels
{
    public class AttachmentEditViewModel
    {
        public int Id { get; set; }
        public int GraduateApplicationId { get; set; }

        [Display(Name = "نوع المرفق")]
        [Required]
        public int AttachmentTypeId { get; set; }
        public SelectList AttachmentTypes { get; set; }

        public string OriginalFileName { get; set; }
        public string FilePath { get; set; }

        [Display(Name = "استبدال الملف (اختياري)")]
        public HttpPostedFileBase NewFile { get; set; }
    }
}