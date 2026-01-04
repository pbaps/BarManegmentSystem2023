using BarManegment.Models;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    // ViewModel لعرض تفاصيل طلب التغيير لاتخاذ قرار بشأنه
    public class RequestReviewViewModel
    {
        public SupervisorChangeRequest Request { get; set; }

        // بيانات القرار
        [Display(Name = "ملاحظات اللجنة")]
        public string CommitteeNotes { get; set; }

        [Display(Name = "فترة السماح (بالأيام)")]
        public int? GracePeriodInDays { get; set; }



 

        // بيانات المتدرب صاحب الطلب
        public GraduateApplication Trainee { get; set; }

        // (يمكن إضافة بيانات أخرى هنا لاحقاً إذا احتجت)
    }
}