using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("EmployeeFinancialHistory")]
    public class EmployeeFinancialHistory
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [Display(Name = "تاريخ التغيير")]
        public DateTime ChangeDate { get; set; } = DateTime.Now;

        [Display(Name = "قام بالتعديل")]
        public string ChangedBy { get; set; }

        [Display(Name = "سبب التعديل")]
        public string ChangeReason { get; set; } // مثال: علاوة سنوية، ترقية، تصحيح

        // --- نسخة من البيانات المالية (Snapshot) ---
        public decimal BasicSalary { get; set; }
        public decimal ManagerAllowance { get; set; }
        public decimal HeadOfDeptAllowance { get; set; }
        public decimal MasterDegreeAllowance { get; set; }
        public decimal PhdDegreeAllowance { get; set; }
        public decimal SpecializationAllowance { get; set; }
        public decimal TransportAllowance { get; set; }
        public decimal EmployeePensionPercent { get; set; }
        public decimal EmployerPensionPercent { get; set; }
        public decimal OtherMonthlyDeduction { get; set; }

        // خاصية لحساب الإجمالي لهذه النسخة التاريخية
        public decimal TotalSalarySnapshot => BasicSalary + ManagerAllowance + HeadOfDeptAllowance + MasterDegreeAllowance + PhdDegreeAllowance + SpecializationAllowance + TransportAllowance;
    }
}