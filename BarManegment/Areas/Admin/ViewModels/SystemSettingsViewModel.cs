using System;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class SystemSettingsViewModel
    {
        // --- الإعدادات الموجودة مسبقاً ---
        [Required(ErrorMessage = "تاريخ البدء مطلوب")]
        [Display(Name = "تاريخ بدء التسجيل للامتحان")]
        [DataType(DataType.Date)]
        public DateTime ExamRegistrationStartDate { get; set; }

        [Required(ErrorMessage = "تاريخ الانتهاء مطلوب")]
        [Display(Name = "تاريخ انتهاء التسجيل للامتحان")]
        [DataType(DataType.Date)]
        public DateTime ExamRegistrationEndDate { get; set; }

        // --- 💡 الإضافة الجديدة: معايير القبول ---

        [Required(ErrorMessage = "معدل الثانوية مطلوب")]
        [Display(Name = "الحد الأدنى لمعدل الثانوية العامة (%)")]
        [Range(50, 100, ErrorMessage = "المعدل يجب أن يكون بين 50 و 100")]
        public double MinHighSchoolScore { get; set; }

        [Required(ErrorMessage = "معدل البكالوريوس مطلوب")]
        [Display(Name = "الحد الأدنى لمعدل البكالوريوس (%)")]
        [Range(50, 100, ErrorMessage = "المعدل يجب أن يكون بين 50 و 100")]
        public double MinBachelorScore { get; set; }

        [Required(ErrorMessage = "تاريخ انتهاء فترة السماح مطلوب")]
        [Display(Name = "تاريخ انتهاء فترة السماح لتجديد المزاولة")]
        [DataType(DataType.Date)]
        public DateTime RenewalGracePeriodEndDate { get; set; }

        // 💡 الإضافة الجديدة: عدد ساعات التدريب المطلوبة
        [Required(ErrorMessage = "عدد ساعات التدريب مطلوب")]
        [Display(Name = "عدد ساعات التدريب والدورات المطلوبة")]
        [Range(0, 1000, ErrorMessage = "يرجى إدخال قيمة منطقية")]
        public int RequiredTrainingHours { get; set; }


        // 👇👇👇 الإضافات الجديدة للرواتب 👇👇👇
        [Required(ErrorMessage = "نسبة الزيادة مطلوبة")]
        [Display(Name = "نسبة الزيادة السنوية الافتراضية (%)")]
        [Range(0, 100, ErrorMessage = "النسبة يجب أن تكون بين 0 و 100")]
        public decimal AnnualIncrementPercent { get; set; }

        [Required(ErrorMessage = "نسبة الاستقطاع مطلوبة")]
        [Display(Name = "نسبة استقطاع الموظف للتأمين (%)")]
        [Range(0, 100, ErrorMessage = "النسبة يجب أن تكون بين 0 و 100")]
        public decimal EmployeePensionPercent { get; set; }

        [Required(ErrorMessage = "نسبة المساهمة مطلوبة")]
        [Display(Name = "نسبة مساهمة النقابة في التأمين (%)")]
        [Range(0, 100, ErrorMessage = "النسبة يجب أن تكون بين 0 و 100")]
        public decimal EmployerPensionPercent { get; set; }
    }
}
