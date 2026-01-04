using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class PurchaseInvoiceItem
    {
        public int Id { get; set; }

        public int PurchaseInvoiceId { get; set; }
        [ForeignKey("PurchaseInvoiceId")]
        public virtual PurchaseInvoice PurchaseInvoice { get; set; }

        public int ItemId { get; set; }
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }

        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        [Display(Name = "سعر الوحدة")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "الإجمالي")]
        public decimal TotalLineAmount => Quantity * UnitPrice;
    }
}