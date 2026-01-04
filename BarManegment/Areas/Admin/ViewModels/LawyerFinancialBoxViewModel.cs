using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    // 1. النموذج الرئيسي للصفحة
    public class LawyerFinancialBoxViewModel
    {
        // بيانات المحامي
        public int LawyerId { get; set; }
        public string LawyerName { get; set; }
        public string NationalId { get; set; }
        public string Iban { get; set; }
        public string BankName { get; set; }

        // الملخص المالي
        [Display(Name = "مجموع المستحقات (قيد الانتظار)")]
        public decimal PendingBalance { get; set; } // رصيد جاهز للصرف

        [Display(Name = "مجموع المستحقات (المحولة للبنك)")]
        public decimal TransferredBalance { get; set; } // ما تم صرفه سابقاً

        [Display(Name = "مجموع المبالغ المحجوزة")]
        public decimal HeldBalance { get; set; } // محجوز إدارياً أو لقروض

        [Display(Name = "إجمالي المديونية (القروض)")]
        public decimal TotalLoanDebt { get; set; } // قروض لم تسدد

        [Display(Name = "صافي الرصيد المتاح")]
        public decimal NetAvailableBalance => PendingBalance - TotalLoanDebt; // معادلة بسيطة

        // سجل الحركات الموحد
        public List<FinancialTransactionItem> Transactions { get; set; }

        public LawyerFinancialBoxViewModel()
        {
            Transactions = new List<FinancialTransactionItem>();
        }
    }

    // 2. النموذج الموحد للحركة المالية (سطر في الجدول)
    public class FinancialTransactionItem
    {
        public int OriginalId { get; set; } // ID من الجدول الأصلي
        public string SourceType { get; set; } // "تصديق", "طوابع", "قسط قرض"
        public string SourceTable { get; set; } // "FeeDistribution", "StampSale", "Loan"

        [Display(Name = "التاريخ")]
        public DateTime Date { get; set; }

        [Display(Name = "البيان / التفاصيل")]
        public string Description { get; set; }

        [Display(Name = "دائن (مستحقات)")]
        public decimal CreditAmount { get; set; } // + للمحامي

        [Display(Name = "مدين (عليه)")]
        public decimal DebitAmount { get; set; } // - على المحامي

        [Display(Name = "الحالة")]
        public string Status { get; set; } // "بانتظار الدفع", "تم التحويل", "محجوز", "مستحق"
        public string StatusColor { get; set; } // لتلوين الحالة (success, warning, danger)

        public bool IsHoldable { get; set; } // هل يمكن حجز هذا البند؟
        public bool IsOnHold { get; set; } // هل هو محجوز فعلاً؟
    }
}