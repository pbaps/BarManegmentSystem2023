using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class FeeTypeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الرسم مطلوب")]
        [Display(Name = "اسم الرسم")]
        public string Name { get; set; }

        [Required(ErrorMessage = "قيمة الرسم مطلوبة")]
        [Display(Name = "القيمة الافتراضية")]
        [Range(0, double.MaxValue, ErrorMessage = "القيمة يجب أن تكون موجبة.")]
        public decimal DefaultAmount { get; set; }

        [Required(ErrorMessage = "العملة مطلوبة")]
        [Display(Name = "العملة")]
        public int CurrencyId { get; set; }
        public SelectList Currencies { get; set; }

        [Required(ErrorMessage = "الحساب البنكي مطلوب")]
        [Display(Name = "الحساب البنكي")]
        public int BankAccountId { get; set; }
        public SelectList BankAccounts { get; set; }

        [Display(Name = "الحالة")]
        public bool IsActive { get; set; } = true;
        // --- ⬇️ ⬇️ بداية الإضافة ⬇️ ⬇️ ---
        [Display(Name = "نسبة المحامي (مثال: 0.5 لـ 50%)")]
        [Range(0.0, 1.0, ErrorMessage = "النسبة يجب أن تكون بين 0.0 و 1.0")]
        public decimal LawyerPercentage { get; set; }

        [Display(Name = "نسبة النقابة (مثال: 0.5 لـ 50%)")]
        [Range(0.0, 1.0, ErrorMessage = "النسبة يجب أن تكون بين 0.0 و 1.0")]
        public decimal BarSharePercentage { get; set; }
        // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---

 
    }
}