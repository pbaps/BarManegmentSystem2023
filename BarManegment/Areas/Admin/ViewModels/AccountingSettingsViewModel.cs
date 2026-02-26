using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class AccountingSettingsViewModel
    {
        // =========================================================
        // 1. الحسابات الأساسية (GL Accounts)
        // =========================================================
        [Display(Name = "حساب الصندوق الرئيسي (الخزينة)")]
        public int? MainBoxAccountId { get; set; }

        [Display(Name = "حساب البنك الافتراضي (للإيداع والصرف)")]
        public int? DefaultBankAccountId { get; set; }

        [Display(Name = "حساب إيراد الطوابع المؤجل (Prepaid)")]
        public int? StampPrepaidAccountId { get; set; }

        [Display(Name = "حساب أمانات المحامين (ذمم دائنة)")]
        public int? StampLawyerShareAccountId { get; set; }

        [Display(Name = "حساب إيرادات النقابة من الطوابع")]
        public int? StampRevenueAccountId { get; set; }

        [Display(Name = "حساب ذمم القروض (المدين)")]
        public int? LoanReceivableAccountId { get; set; }

        [Display(Name = "حساب مصروفات المساعدات المالية")]
        public int? FinancialAidExpenseAccountId { get; set; }

        [Display(Name = "حساب المشتريات / التوريدات")]
        public int? PurchaseAccountId { get; set; }

        [Display(Name = "حساب رواتب الموظفين")]
        public int? PayrollExpenseAccountId { get; set; }

        // =========================================================
        // 2. ربط أنواع الرسوم (Fee Types)
        // =========================================================
        [Display(Name = "نوع رسم امتحان القبول")]
        public int? ExamRegistrationFeeTypeId { get; set; }

        [Display(Name = "نوع رسم تصديق العقود (الافتراضي)")]
        public int? ContractFeeTypeId { get; set; }

        [Display(Name = "نوع رسم بيع الطوابع للمتعهد")]
        public int? StampContractorFeeTypeId { get; set; }

        [Display(Name = "نوع رسم تجديد مزاولة المحامي")]
        public int? LawyerRenewalFeeTypeId { get; set; }

        [Display(Name = "نوع رسم تسجيل متدرب جديد")]
        public int? TraineeRegistrationFeeTypeId { get; set; }

        // =========================================================
        // 3. ربط أنواع العقود والعمليات الأخرى
        // =========================================================
        [Display(Name = "نوع عقد 'وكالة جواز السفر'")]
        public int? PassportAgencyContractTypeId { get; set; }
    }
}