using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("BarExpenses")]
    public class BarExpense
    {
        public int Id { get; set; }

        [Display(Name = "تاريخ الصرف")]
        public DateTime ExpenseDate { get; set; }

        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [Display(Name = "البيان / الوصف")]
        public string Description { get; set; }

        // الحساب الذي تم الصرف منه (حساب النقابة)
        public int BankAccountId { get; set; }
        [ForeignKey("BankAccountId")]
        public virtual BankAccount BankAccount { get; set; }

        // للتصنيف: هل هو مساعدة، رواتب، تشغيلي...
        public string ExpenseCategory { get; set; }
    }
}