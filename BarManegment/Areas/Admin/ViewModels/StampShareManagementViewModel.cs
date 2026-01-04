using BarManegment.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class StampShareManagementViewModel
    {
        public int LawyerId { get; set; }
        public string LawyerName { get; set; }
        public string LawyerStatus { get; set; }

        // قائمة بالحصص المحجوزة حالياً
        public List<StampSale> HeldShares { get; set; }

        // قائمة بالحصص الجاهزة للدفع (التي تم فك حجزها)
        public List<StampSale> ReleasedShares { get; set; }

        // --- لحقل الحجز اليدوي ---
        [Display(Name = "سبب الحجز (مطلوب)")]
        [Required(ErrorMessage = "الرجاء تحديد سبب الحجز")]
        public string HoldReason { get; set; }

        public StampShareManagementViewModel()
        {
            HeldShares = new List<StampSale>();
            ReleasedShares = new List<StampSale>();
        }
    }
}