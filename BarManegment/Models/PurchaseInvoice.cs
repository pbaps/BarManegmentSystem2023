using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class PurchaseInvoice
    {
        public int Id { get; set; }

        [Display(Name = "رقم الفاتورة (للمورد)")]
        public string SupplierInvoiceNumber { get; set; }

        [Required]
        [Display(Name = "تاريخ الفاتورة")]
        public DateTime InvoiceDate { get; set; }

        [Display(Name = "المورد")]
        public int SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        [Display(Name = "طريقة الدفع")]
        public string PaymentMethod { get; set; } // "آجل" (ذمم) أو "نقدي"

        [Display(Name = "الإجمالي")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; }

        // حالة الترحيل للمالية
        public bool IsPosted { get; set; }
        public int? JournalEntryId { get; set; } // لربطها بالقيد

        public virtual ICollection<PurchaseInvoiceItem> Items { get; set; }

        // المستخدم الذي قام بالإدخال
        public int CreatedByUserId { get; set; }
    }
}