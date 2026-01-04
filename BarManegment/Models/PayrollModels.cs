using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    // 1. الجدول الرئيسي: مسير الرواتب الشهري
    public class MonthlyPayroll
    {
        public int Id { get; set; }

        [Required, Display(Name = "الشهر")]
        public int Month { get; set; }

        [Required, Display(Name = "السنة")]
        public int Year { get; set; }

        [Display(Name = "تاريخ الإصدار")]
        public DateTime IssueDate { get; set; } = DateTime.Now;

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; }

        [Display(Name = "إجمالي الرواتب (Gross)")]
        public decimal TotalGrossAmount { get; set; }

        [Display(Name = "إجمالي الصافي (Net)")]
        public decimal TotalNetAmount { get; set; }

        [Display(Name = "تم الترحيل للمالية؟")]
        public bool IsPostedToJournal { get; set; } = false;

        [Display(Name = "رقم القيد اليومي")]
        public int? JournalEntryId { get; set; }

        // 👇👇 أضف هذا السطر المفقود 👇👇
        [ForeignKey("JournalEntryId")]
        public virtual JournalEntry JournalEntry { get; set; }

        [Display(Name = "قام بالإصدار")]
        public string CreatedBy { get; set; }

        // العلاقة مع القسائم التفصيلية
        public virtual ICollection<PayrollSlip> PayrollSlips { get; set; }
    }

    // 2. الجدول التفصيلي: قسيمة راتب الموظف
    public class PayrollSlip
    {
        public int Id { get; set; }

        public int MonthlyPayrollId { get; set; }
        [ForeignKey("MonthlyPayrollId")]
        public virtual MonthlyPayroll MonthlyPayroll { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        // --- تفاصيل الاستحقاقات (تُنسخ من الموظف لحظة الإصدار) ---
        public decimal BasicSalary { get; set; } // الأساسي
        public decimal AllowancesTotal { get; set; } // مجموع العلاوات (مدير، مؤهل، تخصص..)
        public decimal AnnualIncrementAmount { get; set; } // قيمة الزيادة السنوية في هذا الشهر
        public decimal TransportAllowance { get; set; } // المواصلات

        // --- تفاصيل الاستقطاعات ---
        public decimal EmployeePensionDeduction { get; set; } // حصة الموظف 7%
        public decimal OtherDeductions { get; set; } // خصومات أخرى (غياب/سلف)

        // --- النتائج ---
        public decimal GrossSalary { get; set; } // الإجمالي
        public decimal NetSalary { get; set; } // الصافي للدفع

        // --- بيانات البنك (للتصدير للبنك) ---
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
    }
}