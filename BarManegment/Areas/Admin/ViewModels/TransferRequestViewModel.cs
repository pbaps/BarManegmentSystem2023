using System;
using System.ComponentModel.DataAnnotations;
using System.Web; // For HttpPostedFileBase

namespace BarManegment.Areas.Admin.ViewModels
{
    public class TransferRequestViewModel
    {
        [Required]
        public int TraineeId { get; set; }
        public string TraineeName { get; set; }
        public string CurrentSupervisorName { get; set; }

        [Required(ErrorMessage = "الرجاء اختيار المشرف الجديد")]
        [Display(Name = "المشرف الجديد المقترح")]
        public int NewSupervisorId { get; set; }
        // حقل نصي ليتم ملؤه بواسطة jQuery Autocomplete
        [Display(Name = "ابحث عن المشرف الجديد (بالاسم أو الرقم)")]
        public string NewSupervisorSearch { get; set; }

        [Display(Name = "السبب / الملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Reason { get; set; }

        [Display(Name = "مرفق موافقة المشرف القديم (اختياري)")]
        public HttpPostedFileBase OldSupervisorApprovalFile { get; set; }

        [Display(Name = "مرفق موافقة المشرف الجديد (مطلوب)")]
        [Required(ErrorMessage = "مرفق موافقة المشرف الجديد مطلوب")]
        public HttpPostedFileBase NewSupervisorApprovalFile { get; set; }
    }
}