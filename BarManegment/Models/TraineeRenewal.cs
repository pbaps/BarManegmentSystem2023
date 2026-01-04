using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class TraineeRenewal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المتدرب")]
        public int TraineeId { get; set; }
        [ForeignKey("TraineeId")]
        public virtual GraduateApplication Trainee { get; set; }

        [Required]
        [Display(Name = "سنة التجديد")]
        public int RenewalYear { get; set; }

       
 
        [Display(Name = "رقم إيصال السداد")]
        public int? ReceiptId { get; set; } // (تم التعديل إلى int? ليقبل null)
        [ForeignKey("ReceiptId")]
        public virtual Receipt Receipt { get; set; }

        [Display(Name = "تاريخ التجديد")]
        public DateTime RenewalDate { get; set; }
    }
}