using System;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class IncomeStatementItem
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public decimal Amount { get; set; } // القيمة المطلقة
    }

    public class IncomeStatementViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // قائمة الإيرادات
        public List<IncomeStatementItem> Revenues { get; set; } = new List<IncomeStatementItem>();
        public decimal TotalRevenues { get; set; }

        // قائمة المصروفات
        public List<IncomeStatementItem> Expenses { get; set; } = new List<IncomeStatementItem>();
        public decimal TotalExpenses { get; set; }

        // النتيجة النهائية
        public decimal NetIncome => TotalRevenues - TotalExpenses;
    }
}