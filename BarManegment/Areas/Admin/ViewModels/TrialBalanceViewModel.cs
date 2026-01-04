using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class TrialBalanceRow
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; }

        // الرصيد الافتتاحي (قبل الفترة)
        public decimal OpeningDebit { get; set; }
        public decimal OpeningCredit { get; set; }
        public decimal OpeningBalance => OpeningDebit - OpeningCredit;

        // حركة الفترة (خلال الفترة)
        public decimal PeriodDebit { get; set; }
        public decimal PeriodCredit { get; set; }

        // الرصيد الختامي (التراكمي)
        public decimal EndingDebit => OpeningDebit + PeriodDebit;
        public decimal EndingCredit => OpeningCredit + PeriodCredit;
        public decimal NetBalance => EndingDebit - EndingCredit;
    }

    public class TrialBalanceViewModel
    {
        public System.DateTime? FromDate { get; set; }
        public System.DateTime? ToDate { get; set; }
        public System.Collections.Generic.List<TrialBalanceRow> Rows { get; set; }
    }
}