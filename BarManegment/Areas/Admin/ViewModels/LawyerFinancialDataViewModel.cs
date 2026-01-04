using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class LawyerFinancialDataViewModel
    {
        public int LawyerId { get; set; }

        [Display(Name = "اسم المحامي")]
        public string LawyerName { get; set; }

        [Display(Name = "رقم الهوية")]
        public string NationalId { get; set; }

        // === بيانات البنك ===
        [Display(Name = "اسم البنك")]
        public string BankName { get; set; }

        [Display(Name = "فرع البنك")]
        public string BankBranch { get; set; }

        [Display(Name = "رقم الحساب")]
        public string AccountNumber { get; set; }

        [Display(Name = "رقم الآيبان (IBAN)")]
        [StringLength(34, ErrorMessage = "رقم الآيبان طويل جداً")]
        public string Iban { get; set; }

        // === بيانات المحفظة ===
        [Display(Name = "مزود المحفظة")]
        public int? WalletProviderId { get; set; }

        [Display(Name = "رقم المحفظة / الجوال")]
        public string WalletNumber { get; set; }
    }
}