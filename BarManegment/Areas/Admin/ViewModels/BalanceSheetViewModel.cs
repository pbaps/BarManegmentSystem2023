using System;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class BalanceSheetItem
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public decimal Amount { get; set; }
    }

    public class BalanceSheetViewModel
    {
        public DateTime AsOfDate { get; set; }

        // الجانب الأيمن: الأصول
        public List<BalanceSheetItem> Assets { get; set; } = new List<BalanceSheetItem>();
        public decimal TotalAssets { get; set; }

        // الجانب الأيسر: الخصوم
        public List<BalanceSheetItem> Liabilities { get; set; } = new List<BalanceSheetItem>();
        public decimal TotalLiabilities { get; set; }

        // الجانب الأيسر: حقوق الملكية
        public List<BalanceSheetItem> Equity { get; set; } = new List<BalanceSheetItem>();
        public decimal TotalEquity { get; set; }

        // المتمم الحسابي: صافي الدخل (الفائض/العجز المتراكم)
        public decimal NetIncome { get; set; }

        // الإجمالي النهائي للجانب الأيسر
        public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity + NetIncome;

        // التحقق من التوازن
        public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 0.1m;
    }
}