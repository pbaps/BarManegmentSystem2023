using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace BarManegment.Models
{
    public class FiscalYear
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "اسم السنة")]
        public string Name { get; set; } // مثال: 2025

        [Display(Name = "تاريخ البدء")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "تاريخ الانتهاء")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "مغلقة؟")]
        public bool IsClosed { get; set; } // لا يمكن إضافة قيود عليها

        [Display(Name = "الحالية؟")]
        public bool IsCurrent { get; set; } // السنة الافتراضية للنظام
    }
}