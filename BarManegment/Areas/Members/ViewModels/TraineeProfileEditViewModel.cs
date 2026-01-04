using BarManegment.Models;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;
using System.Collections.Generic;

namespace BarManegment.Areas.Members.ViewModels
{
    public class TraineeProfileEditViewModel
    {
        // بيانات المتدرب الأساسية (للعرض)
        public int Id { get; set; }
        public string ArabicName { get; set; }
        public string NationalIdNumber { get; set; }

        // بيانات الاتصال
        [Required]
        public ContactInfo ContactInfo { get; set; }

        // بيانات المشرف
        [Required(ErrorMessage = "يجب اختيار محامي مشرف.")]
        [Display(Name = "المحامي المشرف")]
        public int? SupervisorId { get; set; }

        [Display(Name = "ابحث عن اسم المشرف...")]
        public string SupervisorName { get; set; } // يستخدم فقط للبحث

        // المرفقات
        [Display(Name = "نموذج (لا مانع) موقع من المشرف")]
        public HttpPostedFileBase SupervisorApprovalFile { get; set; }

        public string SupervisorApprovalFilePath { get; set; } // لعرض الرابط بعد الرفع

        // قوائم مساعدة
        public SelectList Genders { get; set; }
        public SelectList NationalIdTypes { get; set; }
    }
}
