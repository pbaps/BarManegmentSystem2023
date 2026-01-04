using System;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class GeneralLedgerRow
    {
        public DateTime Date { get; set; }
        public string DocumentType { get; set; } // نوع المستند (سند صرف، قيد، إلخ)
        public string ReferenceNumber { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; } // الرصيد المتحرك
    }

    public class GeneralLedgerViewModel
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public string AccountCode { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public decimal OpeningBalance { get; set; } // الرصيد السابق
        public List<GeneralLedgerRow> Transactions { get; set; } = new List<GeneralLedgerRow>();

        // المجاميع
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}