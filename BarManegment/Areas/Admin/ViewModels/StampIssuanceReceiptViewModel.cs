using System;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    // ViewModel مخصص لطباعة إيصال صرف الطوابع النقدي
    public class StampIssuanceReceiptViewModel
    {
        public int ReceiptId { get; set; }
        public string ReceiptFullNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ContractorName { get; set; }
        public string IssuedByUserName { get; set; }
        public decimal TotalAmount { get; set; }
        public string TotalAmountInWords { get; set; }
        public string CurrencySymbol { get; set; }

        public string BankReceiptNumber { get; set; } // <-- ✅ أضف هذا السطر المفقود هنا

        public List<StampIssuanceReceiptDetail> Details { get; set; }


        public StampIssuanceReceiptViewModel()
        {
            Details = new List<StampIssuanceReceiptDetail>();
        }
    }

    public class StampIssuanceReceiptDetail
    {
        public string Description { get; set; } // e.g., "دفتر طوابع (من... إلى...)"
        public decimal Amount { get; set; }
    }
}