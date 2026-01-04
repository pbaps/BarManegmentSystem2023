using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BarManegment.ViewModels;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class CreateGeneralVoucherViewModel
    {
        [Required(ErrorMessage = "يرجى إدخال اسم الجهة / المستفيد")]
        [Display(Name = "اسم المستفيد / الجهة")]
        public string PayerName { get; set; }

        [Required]
        [Display(Name = "تاريخ الانتهاء")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "طريقة الدفع")]
        public string PaymentMethod { get; set; } = "نقدي";

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; }

        public List<FeeSelection> Fees { get; set; } = new List<FeeSelection>();


    }
}