using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    // (هذا الـ ViewModel مشابه لـ LawyerShareViewModel لكنه خاص بالطوابع)
    public class LawyerStampShareViewModel
    {
        [Display(Name = "معرف المحامي")]
        public int LawyerId { get; set; }

        [Display(Name = "اسم المحامي")]
        public string LawyerName { get; set; }

        [Display(Name = "الرقم الوطني")]
        public string IdentificationNumber { get; set; }

        [Display(Name = "اسم البنك")]
        public string BankName { get; set; }

        [Display(Name = "فرع البنك")]
        public string BankBranch { get; set; }

        [Display(Name = "رقم الحساب")]
        public string AccountNumber { get; set; }

        [Display(Name = "IBAN (آيبان)")]
        public string Iban { get; set; }

        [Display(Name = "إجمالي المستحق")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "العملة")]
        public string CurrencySymbol { get; set; } = "₪"; // (نفترض أن الطوابع دائماً بالشيكل)

        [Display(Name = "عدد الطوابع المباعة")]
        public int TransactionCount { get; set; }
    }
}