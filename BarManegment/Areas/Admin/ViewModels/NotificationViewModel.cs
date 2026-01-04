using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class NotificationViewModel
    {
        public int ExamId { get; set; }

        [Required(ErrorMessage = "موضوع الرسالة مطلوب.")]
        [Display(Name = "الموضوع")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "نص الرسالة مطلوب.")]
        [Display(Name = "نص الرسالة")]
        [AllowHtml]
        public string Body { get; set; }
    }
}
