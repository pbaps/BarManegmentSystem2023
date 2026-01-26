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


        // ✅✅✅ الإضافة الجديدة: تفعيل رسوم الامتحان ✅✅✅
        [Display(Name = "تفعيل رسوم تسجيل الامتحان")]
        public bool IsExamFeeEnabled { get; set; }

        // =========================================================
        // 💰 إعدادات الربط المحاسبي (جديد)
        // =========================================================
        [Display(Name = "حساب إيراد الطوابع المؤجل (Prepaid)")]
        public int? StampPrepaidAccountId { get; set; }

        [Display(Name = "حساب أمانات المحامين (ذمم دائنة)")]
        public int? StampLawyerShareAccountId { get; set; }

        [Display(Name = "حساب إيرادات النقابة من الطوابع")]
        public int? StampRevenueAccountId { get; set; }

        [Display(Name = "حساب البنك الافتراضي للصرف")]
        public int? DefaultBankPaymentAccountId { get; set; }

        [Display(Name = "نوع رسم امتحان القبول")]
        public int? ExamRegistrationFeeTypeId { get; set; }

        [Display(Name = "نوع رسم تصديق العقود")]
        public int? ContractFeeTypeId { get; set; }

        [Display(Name = "نوع عقد 'وكالة جواز السفر'")]
        public int? PassportAgencyContractTypeId { get; set; }

        [Display(Name = "نوع رسم بيع الطوابع للمتعهد")]
        public int? StampContractorFeeTypeId { get; set; }

        // ✅ إعدادات الحضور الذكي (جديد)
        [Display(Name = "خط العرض (Latitude)")]
        [Required(ErrorMessage = "مطلوب لتحديد الموقع")]
        public string OfficeLatitude { get; set; }

        [Display(Name = "خط الطول (Longitude)")]
        [Required(ErrorMessage = "مطلوب لتحديد الموقع")]
        public string OfficeLongitude { get; set; }

        [Display(Name = "نطاق السماح (متر)")]
        [Range(10, 5000, ErrorMessage = "النطاق يجب أن يكون بين 10 و 5000 متر")]
        public int AllowedRadiusMeters { get; set; } = 100;
    }
}
