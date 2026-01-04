using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class EmployeePayrollSlip
    {
        public int Id { get; set; }

        public int MonthlyPayrollId { get; set; }
        [ForeignKey("MonthlyPayrollId")]
        public virtual MonthlyPayroll MonthlyPayroll { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        // --- التفاصيل المالية للشهر ---
        public decimal BasicSalary { get; set; } // الراتب الأساسي
        public decimal Allowances { get; set; } // مجموع البدلات
        public decimal Deductions { get; set; } // الخصومات (غياب، سلف، تأخير)
        public decimal Bonuses { get; set; } // مكافآت إضافية لهذا الشهر

        public decimal NetSalary { get; set; } // الصافي المستحق للدفع

        public string Notes { get; set; } // ملاحظات (سبب الخصم مثلاً)
    }
}