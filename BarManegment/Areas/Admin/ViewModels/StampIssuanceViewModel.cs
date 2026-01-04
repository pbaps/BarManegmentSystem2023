using BarManegment.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class StampIssuanceViewModel
    {
        // للعرض
        public IEnumerable<StampBook> AvailableBooksList { get; set; }
        public SelectList ContractorsList { get; set; }

        // للاستلام
        public int SelectedContractorId { get; set; }
        public List<int> SelectedBookIds { get; set; }
    }
}