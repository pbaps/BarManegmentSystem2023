using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class ExchangeRate
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "العملة")]
        public int CurrencyId { get; set; }
        public virtual Currency Currency { get; set; }

        [Required]
        [Display(Name = "سعر التحويل (مقابل الشيكل)")]
        public decimal Rate { get; set; }

        [Required]
        [Display(Name = "تاريخ السعر")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Now;

        [Display(Name = "تم الإدخال بواسطة")]
        public string CreatedBy { get; set; }
    }
}