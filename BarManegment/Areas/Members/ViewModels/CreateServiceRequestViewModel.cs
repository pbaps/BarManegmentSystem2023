using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Members.ViewModels
{
    public class CreateServiceRequestViewModel
    {
        [Required(ErrorMessage = "الرجاء اختيار نوع الطلب")]
        [Display(Name = "نوع الطلب")]
        public string RequestType { get; set; } // "نقل", "وقف", "استكمال"

        [Display(Name = "المشرف الحالي")]
        public string CurrentSupervisorName { get; set; } // (للعرض فقط)

        [Display(Name = "المشرف الجديد المقترح")]
        public int? NewSupervisorId { get; set; }

        [Display(Name = "المرفق (مطلوب لطلبات النقل والاستكمال)")]
        public HttpPostedFileBase AttachmentFile { get; set; }

        // (سنقوم بتعبئة هذه القائمة في المتحكم)
        public SelectList SupervisorList { get; set; }

        [Display(Name = "السبب/الملاحظات (اختياري)")]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }
    }
}