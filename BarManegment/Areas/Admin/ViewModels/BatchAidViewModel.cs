using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class BatchAidViewModel
    {
        [Required, Display(Name = "نوع المساعدة")]
        public int AidTypeId { get; set; }

        [Required, Display(Name = "المبلغ لكل محامي")]
        public decimal AmountPerLawyer { get; set; }

        [Display(Name = "العملة")]
        public int CurrencyId { get; set; }

        [Display(Name = "ملاحظات عامة")]
        public string Notes { get; set; }

        // الحساب البنكي للنقابة الذي سيتم التحويل منه (مهم للطباعة والقيود)
        [Display(Name = "حساب النقابة (المصدر)")]
        public int SourceBankAccountId { get; set; }

        public string BatchTitle { get; set; } // عنوان الكشف (مثال: مساعدات شهر 5)

        // قائمة معرفات المحامين الذين سيتم صرف المساعدة لهم
        public List<int> SelectedLawyerIds { get; set; }
    }
}