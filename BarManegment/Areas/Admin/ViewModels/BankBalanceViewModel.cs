using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class BankBalanceViewModel
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string CurrencySymbol { get; set; }
        public decimal ForeignBalance { get; set; } // الرصيد الفعلي (دولار/دينار)
        public decimal LocalBalance { get; set; }   // المقابل بالشيكل (للميزانية)
    }
}