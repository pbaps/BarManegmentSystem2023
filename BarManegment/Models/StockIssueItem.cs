using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class StockIssueItem
    {
        public int Id { get; set; }

        public int StockIssueId { get; set; }
        [ForeignKey("StockIssueId")]
        public virtual StockIssue StockIssue { get; set; }

        public int ItemId { get; set; }
        [ForeignKey("ItemId")]
        public virtual Item Item { get; set; }

        [Display(Name = "الكمية المصروفة")]
        public int Quantity { get; set; }

        // نخزن التكلفة لحظة الصرف لغايات الحسابات
        [Display(Name = "التكلفة (عند الصرف)")]
        public decimal UnitCostSnapshot { get; set; }
    }
}