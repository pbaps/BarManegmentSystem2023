using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        public int LeaveTypeId { get; set; }
        [ForeignKey("LeaveTypeId")]
        public virtual LeaveType LeaveType { get; set; }

        [Display(Name = "من تاريخ")]
        public DateTime StartDate { get; set; }

        [Display(Name = "إلى تاريخ")]
        public DateTime EndDate { get; set; }

        [Display(Name = "عدد الأيام")]
        public int DaysCount { get; set; }

        [Display(Name = "السبب")]
        public string Reason { get; set; }

        [Display(Name = "الحالة")]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [Display(Name = "ملاحظات المدير")]
        public string ManagerComment { get; set; }
    }
}