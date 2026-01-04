using System;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class CreateContractorReceiptViewModel
    {
        [Required]
        public int VoucherId { get; set; }

        [Display(Name = "اسم المتعهد")]
        public string ContractorName { get; set; }

        [Display(Name = "المبلغ الإجمالي")]
        public decimal TotalAmount { get; set; }
        public string CurrencySymbol { get; set; }

        [Required(ErrorMessage = "تاريخ الدفع في البنك مطلوب")]
        [Display(Name = "تاريخ الدفع في البنك")]
        [DataType(DataType.Date)]
        public DateTime BankPaymentDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "رقم وصل البنك مطلوب")]
        [Display(Name = "رقم وصل البنك")]
        public string BankReceiptNumber { get; set; }
    }
}