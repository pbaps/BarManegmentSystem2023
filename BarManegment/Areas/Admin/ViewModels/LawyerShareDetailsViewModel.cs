using BarManegment.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    /// <summary>
    /// نموذج لعرض تفاصيل حصص المحامي الواحد (المحجوزة وغير المحجوزة)
    /// </summary>
    public class LawyerShareDetailsViewModel
    {
        public GraduateApplication Lawyer { get; set; }
        public List<FeeDistribution> Shares { get; set; }

        // يستخدم لإضافة سبب الحجز
        [StringLength(500)]
        [Required(ErrorMessage = "سبب الحجز مطلوب")]
        public string HoldReason { get; set; }
    }
}