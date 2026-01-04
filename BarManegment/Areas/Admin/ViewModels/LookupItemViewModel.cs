using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    // ViewModel عام لعناصر الجداول المساعدة
    public class LookupItemViewModel
    {
        public int Id { get; set; } // ضروري للتعديل والحذف
        public string Type { get; set; } // اسم نوع الجدول (مثل Genders)

        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم")]
        public string Name { get; set; }

        // حقول إضافية يمكن استخدامها حسب الحاجة لأنواع مختلفة
        [Display(Name = "الرمز")]
        public string Symbol { get; set; } // مثال: للعملات

        [Display(Name = "النسبة المئوية")]
        [Range(0, 100, ErrorMessage = "القيمة يجب أن تكون بين 0 و 100")]
        public double? PercentageValue { get; set; } // مثال: لأنواع المؤهلات

        // يمكنك إضافة حقول أخرى هنا (IsActive, Order, Description, etc.)
    }
}