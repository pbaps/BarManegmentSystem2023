using System;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    // نموذج لطباعة إيصال سداد القسط
    public class PrintLoanReceiptViewModel
    {
        [Display(Name = "رقم الإيصال")]
        public string ReceiptFullNumber { get; set; }

        [Display(Name = "اسم المحامي")]
        public string LawyerName { get; set; }

        [Display(Name = "تاريخ السداد")]
        public DateTime PaymentDate { get; set; }

        [Display(Name = "رقم وصل البنك")]
        public string BankReceiptNumber { get; set; }

        [Display(Name = "رقم القرض")]
        public int LoanId { get; set; }

        [Display(Name = "نوع القرض")]
        public string LoanTypeName { get; set; }

        [Display(Name = "رقم القسط")]
        public int InstallmentNumber { get; set; }

        [Display(Name = "قيمة القسط")]
        public decimal AmountPaid { get; set; }

        [Display(Name = "العملة")]
        public string CurrencySymbol { get; set; }

        [Display(Name = "المبلغ بالكلمات")]
        public string AmountInWords { get; set; }

        [Display(Name = "اسم الموظف")]
        public string EmployeeName { get; set; }

        public int ReceiptId { get; set; }
        public string ReceiptNumber { get; set; }
        public DateTime ReceiptDate { get; set; }
        public decimal Amount { get; set; }
 
        public string PayerName { get; set; }
        public string Description { get; set; }
 
        public string LoanType { get; set; }
 
    }
}