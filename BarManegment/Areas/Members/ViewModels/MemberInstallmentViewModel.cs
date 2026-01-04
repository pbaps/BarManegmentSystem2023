using System;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Members.ViewModels
{
    public class MemberInstallmentViewModel
    {
        [Display(Name = "رقم القسط")]
        public int InstallmentNumber { get; set; }

        [Display(Name = "تاريخ الاستحقاق")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "قيمة القسط")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal Amount { get; set; }

        [Display(Name = "الحالة")]
        public string Status { get; set; } // (مستحق، مدفوع، متأخر)

        [Display(Name = "رقم القسيمة")]
        public int? PaymentVoucherId { get; set; }

        [Display(Name = "رقم الإيصال")]
        public int? ReceiptId { get; set; }
    }
}