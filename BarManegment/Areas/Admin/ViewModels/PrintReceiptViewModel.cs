using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ReceiptDetailViewModel
    {
        public string FeeTypeName { get; set; }
        public decimal Amount { get; set; }

        [Display(Name = "البيان")]
        public string Description { get; set; }
    }

    public class PrintReceiptViewModel
    {
        public int ReceiptId { get; set; }
        public string ReceiptFullNumber { get; set; }

        [Display(Name = "اسم مقدم الطلب")]
        public string ApplicantName { get; set; } // (كان TraineeName)

        [Display(Name = "حالة مقدم الطلب")]
        public string ApplicantStatus { get; set; } // (حقل جديد)

        public DateTime BankPaymentDate { get; set; }
        public string BankReceiptNumber { get; set; }
        public DateTime CreationDate { get; set; }
        public string IssuedByUserName { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrencySymbol { get; set; }

        [Display(Name = "المبلغ (تفقيط)")]
        public string TotalAmountInWords { get; set; } // (مطلوب لسطر 289)
                                                       // 💡 الإضافة الجديدة لحل الخطأ:
        public string PaymentMethod { get; set; }
        public List<ReceiptDetailViewModel> Details { get; set; }

        public PrintReceiptViewModel()
        {
            Details = new List<ReceiptDetailViewModel>();
        }
    }
}