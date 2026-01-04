 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class BankAccount
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم البنك مطلوب")]
        [Display(Name = "اسم البنك")]
        public string BankName { get; set; }

        [Required(ErrorMessage = "اسم الحساب مطلوب")]
        [Display(Name = "اسم الحساب")]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "رقم الحساب مطلوب")]
        [Display(Name = "رقم الحساب")]
        public string AccountNumber { get; set; }

        [Display(Name = "رقم الآيبان (IBAN)")]
        public string Iban { get; set; }

        [Required(ErrorMessage = "العملة مطلوبة")]
        [Display(Name = "العملة")]
        public int CurrencyId { get; set; }
        [ForeignKey("CurrencyId")]
        public virtual Currency Currency { get; set; }

        [Display(Name = "الحالة")]
        public bool IsActive { get; set; } = true;

        // ربط الحساب البنكي بالحساب المحاسبي (الأصل)
        public int? RelatedAccountId { get; set; }
        [ForeignKey("RelatedAccountId")]
        public virtual Account RelatedAccount { get; set; }
    }
}