using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class JournalEntryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "التاريخ مطلوب")]
        [Display(Name = "تاريخ القيد")]
        public DateTime EntryDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "البيان مطلوب")]
        [Display(Name = "شرح القيد")]
        public string Description { get; set; }

        [Display(Name = "رقم المستند المرفق")]
        public string ReferenceNumber { get; set; }

        // قائمة الأسطر
        public List<JournalEntryLineViewModel> Lines { get; set; } = new List<JournalEntryLineViewModel>();
    }

    public class JournalEntryLineViewModel
    {
        public int AccountId { get; set; }
        public int? CostCenterId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string LineDescription { get; set; }
    }
}