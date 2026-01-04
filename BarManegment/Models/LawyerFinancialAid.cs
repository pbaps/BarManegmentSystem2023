using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("LawyerFinancialAids")]
    public class LawyerFinancialAid
    {
        public int Id { get; set; }

        // === 💡 الإضافة الجديدة ===
        public string BatchReference { get; set; } // رقم الكشف الجماعي

        // المحامي المستفيد
        public int LawyerId { get; set; }
        [ForeignKey("LawyerId")]
        public virtual GraduateApplication Lawyer { get; set; }

        // نوع المساعدة
        public int AidTypeId { get; set; }
        [ForeignKey("AidTypeId")]
        public virtual SystemLookup AidType { get; set; } // ✅ التغيير ليربط بجدول SystemLookups




        [Display(Name = "تاريخ القرار")]
        public DateTime DecisionDate { get; set; }

        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        // العملة (شيكل، دولار..)
        public int CurrencyId { get; set; }
        [ForeignKey("CurrencyId")]
        public virtual Currency Currency { get; set; }

        // طريقة الدفع: BankTransfer أو Wallet
        [Display(Name = "طريقة الصرف")]
        public string DisbursementMethod { get; set; }

        // === تفاصيل الدفع (حسب الطريقة) ===
        [Display(Name = "اسم البنك المستفيد")]
        public string TargetBankName { get; set; }

        [Display(Name = "فرع البنك")]
        public string TargetBankBranch { get; set; }

        [Display(Name = "رقم الآيبان IBAN")]
        public string TargetIban { get; set; }

        [Display(Name = "رقم المحفظة الإلكترونية")]
        public string TargetWalletNumber { get; set; }

        // الحالة
        [Display(Name = "هل تم الصرف؟")]
        public bool IsPaid { get; set; }
        public DateTime? PaymentDate { get; set; }

        // ربط مع جدول المصروفات (عند الصرف)
        public int? ExpenseId { get; set; }
        [ForeignKey("ExpenseId")]
        public virtual BarExpense Expense { get; set; }

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; }
    }
}