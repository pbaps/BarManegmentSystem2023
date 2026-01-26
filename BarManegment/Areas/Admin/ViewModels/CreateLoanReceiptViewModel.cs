
using System;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class CreateLoanReceiptViewModel
    {
        public int InstallmentId { get; set; }
        public int VoucherId { get; set; }

        [Display(Name = "اسم المحامي")]
        public string LawyerName { get; set; }

        [Display(Name = "قيمة القسط")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "رقم إيصال البنك مطلوب")]
        [Display(Name = "رقم إيصال البنك")]
        public string BankReceiptNumber { get; set; }

        [Required(ErrorMessage = "تاريخ سداد البنك مطلوب")]
        [Display(Name = "تاريخ سداد البنك")]
        [DataType(DataType.Date)]
        public DateTime BankPaymentDate { get; set; } = DateTime.Now;

        public string Description { get; set; }
    }
}