using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class SupervisorRequestViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الرجاء تحديد المتدرب.")]
        [Display(Name = "المتدرب")]
        public int TraineeId { get; set; }
        public string TraineeName { get; set; } // للعرض

        [Required(ErrorMessage = "الرجاء تحديد نوع الطلب.")]
        [Display(Name = "نوع الطلب")]
        public string RequestType { get; set; } // "نقل", "وقف", "استئناف"

        [Display(Name = "المشرف الجديد")]
        public int? NewSupervisorId { get; set; }
        public string NewSupervisorName { get; set; } // للبحث
        [Display(Name = "ابحث عن المشرف الجديد")]
        public string SupervisorSearch { get; set; } // تأكد من وجود هذا السطر بالاسم الصحيح

        [Display(Name = "فترة السماح (بالأيام)")]
        [Range(1, 365, ErrorMessage = "الرجاء إدخال عدد أيام صحيح.")]
        public int? GracePeriodInDays { get; set; }

        [Display(Name = "المشرف الحالي (تلقائي)")]
        public string CurrentSupervisorName { get; set; }
        // === نهاية الإضافة ===

        // حقول رفع الملفات
        [Display(Name = "مرفق موافقة المشرف القديم")]
        public HttpPostedFileBase OldSupervisorApprovalFile { get; set; }

        [Display(Name = "مرفق موافقة المشرف الجديد")]
        public HttpPostedFileBase NewSupervisorApprovalFile { get; set; }

        // --- أضف هذه الخاصية ---
        [Display(Name = "السبب / الملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }
        // --- نهاية الإضافة ---
        [Display(Name = "تأجيل الرسوم (حالة استثنائية)")]
        public bool DeferPayment { get; set; }
    }
}
