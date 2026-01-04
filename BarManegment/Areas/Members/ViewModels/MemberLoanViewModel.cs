using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Members.ViewModels
{
    public class MemberLoanViewModel
    {
        [Display(Name = "رقم القرض")]
        public int LoanId { get; set; }

        [Display(Name = "نوع القرض")]
        public string LoanTypeName { get; set; }

        [Display(Name = "مبلغ القرض")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal Amount { get; set; }

        [Display(Name = "عدد الأقساط")]
        public int InstallmentCount { get; set; }

        [Display(Name = "تاريخ الطلب")]
        [DataType(DataType.Date)]
        public DateTime ApplicationDate { get; set; }

        [Display(Name = "الحالة")]
        public string Status { get; set; } // (تحت المراجعة، مفعل، مكتمل)

        [Display(Name = "تم الصرف؟")]
        public bool IsDisbursed { get; set; }

        // (قائمة الأقساط التفصيلية)
        public List<MemberInstallmentViewModel> Installments { get; set; }

        public MemberLoanViewModel()
        {
            Installments = new List<MemberInstallmentViewModel>();
        }
    }
}