using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class PurchaseInvoiceItemViewModel
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class PurchaseInvoiceViewModel
    {
        [Display(Name = "المورد")]
        public int SupplierId { get; set; }

        [Required]
        [Display(Name = "رقم فاتورة المورد")]
        public string SupplierInvoiceNumber { get; set; }

        [Display(Name = "تاريخ الفاتورة")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Display(Name = "طريقة الدفع")]
        public string PaymentMethod { get; set; } = "آجل"; // آجل، نقدي

        public string Notes { get; set; }

        // قائمة الأصناف
        public List<PurchaseInvoiceItemViewModel> Items { get; set; } = new List<PurchaseInvoiceItemViewModel>();
    }
}