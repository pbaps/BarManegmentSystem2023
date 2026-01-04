using BarManegment.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class CreateContractorVoucherViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "الرجاء اختيار المتعهد")]
        [System.ComponentModel.DataAnnotations.Display(Name = "المتعهد")]
        public int SelectedContractorId { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "الرجاء اختيار دفتر واحد على الأقل")]
        [System.ComponentModel.DataAnnotations.Display(Name = "الدفاتر المطلوب حجزها")]
        public List<int> SelectedBookIds { get; set; }

        // --- قوائم لملء الفورم ---
        public SelectList ContractorsList { get; set; }
        public List<StampBook> AvailableBooksList { get; set; }

        public CreateContractorVoucherViewModel()
        {
            SelectedBookIds = new List<int>();
            AvailableBooksList = new List<StampBook>();
        }
    }
}