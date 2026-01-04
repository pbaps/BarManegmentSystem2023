using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using BarManegment.Models;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class BankAccountViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم البنك مطلوب")]
        [Display(Name = "اسم البنك")]
        [StringLength(100)]
        public string BankName { get; set; }

        [Required(ErrorMessage = "اسم الحساب مطلوب")]
        [Display(Name = "اسم الحساب")]
        [StringLength(150)]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "رقم الحساب مطلوب")]
        [Display(Name = "رقم الحساب")]
        [StringLength(50)]
        public string AccountNumber { get; set; }

        [Display(Name = "رقم الآيبان (IBAN)")]
        [StringLength(50)]
        public string Iban { get; set; }

        [Required(ErrorMessage = "العملة مطلوبة")]
        [Display(Name = "العملة")]
        public int CurrencyId { get; set; }
        public SelectList Currencies { get; set; }

        [Display(Name = "الحالة")]
        public bool IsActive { get; set; } = true;
    }
}
