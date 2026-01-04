using System;
using System.Collections.Generic;

namespace BarManegment.ViewModels
{
    public class PrintVoucherViewModel
    {
        public int VoucherId { get; set; }
        public string TraineeName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string TotalAmountCurrencySymbol { get; set; }
        public string IssuedByUserName { get; set; } // الخاصية الجديدة
        // --- 💡 الإضافة الجديدة ---
        public string PaymentMethod { get; set; }
        // --- نهاية الإضافة ---
        public List<VoucherPrintDetail> Details { get; set; }



    }

    public class VoucherPrintDetail
    {
        public string FeeTypeName { get; set; }
        public decimal Amount { get; set; }
        public string CurrencySymbol { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string Iban { get; set; }

  

    }
}
