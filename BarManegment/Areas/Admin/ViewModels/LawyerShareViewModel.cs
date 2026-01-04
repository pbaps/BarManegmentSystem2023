using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class LawyerShareViewModel
    {
        [Display(Name = "معرف المحامي")]
        public int LawyerId { get; set; }

        [Display(Name = "اسم المحامي")]
        public string LawyerName { get; set; }

        // --- ⬇️ ⬇️ بداية التعديل (إضافة حقول البنك) ⬇️ ⬇️ ---
        [Display(Name = "اسم البنك")]
        public string BankName { get; set; }

        [Display(Name = "فرع البنك")]
        public string BankBranch { get; set; }

        [Display(Name = "رقم الحساب")]
        public string AccountNumber { get; set; }

        [Display(Name = "IBAN (آيبان)")]
        public string Iban { get; set; }
        // --- ⬆️ ⬆️ نهاية التعديل ⬆️ ⬆️ ---
        [Display(Name = "إجمالي المستحق")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "العملة")]
        public string CurrencySymbol { get; set; } // (نفترض أن كل الحصص بنفس العملة حالياً)

        [Display(Name = "عدد المعاملات")]
        public int TransactionCount { get; set; }

        // 💡 (إضافة جديدة)
        [Display(Name = "الرقم الوطني")]
        public string IdentificationNumber { get; set; }
    }
}