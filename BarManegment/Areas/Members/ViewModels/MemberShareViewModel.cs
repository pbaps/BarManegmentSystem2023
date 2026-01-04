using System;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Members.ViewModels
{
    public class MemberShareViewModel
    {
        [Display(Name = "رقم المعاملة")]
        public int TransactionId { get; set; }

        [Display(Name = "نوع العقد")]
        public string ContractTypeName { get; set; }

        [Display(Name = "تاريخ الدفع")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; }

        [Display(Name = "قيمة الحصة")]
        public decimal LawyerShareAmount { get; set; }

        [Display(Name = "الحالة")]
        public string Status { get; set; } // (جاهزة للدفع، محجوزة، مرسلة للبنك)
    }
}