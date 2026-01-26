using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class StampSalesReportViewModel
    {
        [Display(Name = "من تاريخ")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "إلى تاريخ")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [Display(Name = "المتعهد")]
        public int? ContractorId { get; set; }

        // النتائج
        public List<StampSaleItemDto> Sales { get; set; } = new List<StampSaleItemDto>();

        // المجاميع (Summary)
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public decimal TotalLawyerShare { get; set; }
        public decimal TotalBarShare { get; set; }
    }

    public class StampSaleItemDto
    {
        public int Id { get; set; }
        public DateTime SaleDate { get; set; }
        public string ContractorName { get; set; }
        public string LawyerName { get; set; }
        public string LawyerMembershipId { get; set; }
        public long SerialNumber { get; set; }
        public decimal Value { get; set; }
        public decimal LawyerShare { get; set; }
        public decimal BarShare { get; set; }
        public string RecordedBy { get; set; }
    }
}