using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class StockIssue
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "تاريخ الصرف")]
        public DateTime IssueDate { get; set; }

        [Display(Name = "صرف لـ (الموظف)")]
        public int? EmployeeId { get; set; } // يمكن ربطه بجدول Users

        [Display(Name = "القسم / المستفيد")]
        public string DepartmentName { get; set; } // وصف نصي للقسم أو اللجنة

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; }

        // حالة الترحيل (لتحويلها لمصروف)
        public bool IsPosted { get; set; }
        public int? JournalEntryId { get; set; }

        public virtual ICollection<StockIssueItem> Items { get; set; }

        public int IssuedByUserId { get; set; }
    }
}