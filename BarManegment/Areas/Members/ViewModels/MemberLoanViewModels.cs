using System;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Members.ViewModels
{
    public class LoanApplicationCreateViewModel
    {
        [Display(Name = "نوع القرض")]
        [Required(ErrorMessage = "يجب اختيار نوع القرض")]
        public int LoanTypeId { get; set; }

        [Display(Name = "قيمة القرض المطلوبة")]
        [Required]
        [Range(1, 10000, ErrorMessage = "المبلغ يجب أن يكون بين 1 و 10000")]
        public decimal Amount { get; set; }

        [Display(Name = "عدد الأقساط الشهرية")]
        [Required]
        [Range(1, 60, ErrorMessage = "عدد الأقساط يجب أن يكون بين 1 و 60")]
        public int InstallmentCount { get; set; }

        [Display(Name = "ملاحظات / سبب القرض")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        // للعرض فقط
        public string MaxAmountDisplay { get; set; }
    }
}