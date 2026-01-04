using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class StockIssueItemViewModel
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class StockIssueViewModel
    {
        [Display(Name = "تاريخ الصرف")]
        [DataType(DataType.Date)]
        public DateTime IssueDate { get; set; } = DateTime.Now;

        [Display(Name = "المستفيد / القسم")]
        public string DepartmentName { get; set; }

        [Display(Name = "صرف للموظف")]
        public int? EmployeeId { get; set; }

        public string Notes { get; set; }

        public List<StockIssueItemViewModel> Items { get; set; } = new List<StockIssueItemViewModel>();
    }
}