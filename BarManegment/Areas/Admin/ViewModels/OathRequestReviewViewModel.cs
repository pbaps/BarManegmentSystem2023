// ... (using directives)
using BarManegment.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class OathRequestReviewViewModel
    {
        public OathRequest OathRequest { get; set; }

        // --- تم تعديل الاسم من AvailableOathFees إلى AvailableFees ---
        [Display(Name = "الرسوم المقترحة لإصدار القسيمة")]
        public List<FeeSelectionViewModel> AvailableFees { get; set; }

        [Display(Name = "ملاحظات اللجنة (للموافقة/الرفض)")]
        [DataType(DataType.MultilineText)]
        public string CommitteeNotes { get; set; }

        public OathRequestReviewViewModel()
        {
            // --- تم تعديل الاسم ---
            AvailableFees = new List<FeeSelectionViewModel>();
        }
    }

    // (كلاس FeeSelectionViewModel يبقى كما هو)
    // --- هذا هو الكلاس الذي سنقوم بتحديثه ---
    public class FeeSelectionViewModel
    {
        [Required]
        public int FeeTypeId { get; set; }

        [Display(Name = "اسم الرسم")]
        public string FeeTypeName { get; set; }

        [Display(Name = "المبلغ")]
        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; } // سيصبح قابلاً للتعديل

        public bool IsSelected { get; set; }

        // --- حقول إضافية للعرض ---
        public string CurrencySymbol { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string Iban { get; set; }
    }
}