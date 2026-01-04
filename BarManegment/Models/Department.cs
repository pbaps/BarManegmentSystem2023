using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace BarManegment.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required, Display(Name = "اسم القسم")]
        public string Name { get; set; }

        [Display(Name = "مدير القسم")]
        public int? ManagerId { get; set; } // يمكن ربطه بالموظف لاحقاً

        // 👇 الإضافات الجديدة 👇
        [Display(Name = "نسبة الزيادة السنوية (%)")]
        public decimal AnnualIncrementPercent { get; set; }

        [Display(Name = "نسبة استقطاع الموظف (%)")]
        public decimal EmployeePensionPercent { get; set; }

        [Display(Name = "نسبة مساهمة النقابة (%)")]
        public decimal EmployerPensionPercent { get; set; }




    }
}