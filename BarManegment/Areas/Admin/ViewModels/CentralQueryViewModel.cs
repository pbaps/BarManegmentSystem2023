using BarManegment.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class CentralQueryViewModel
    {
        public IEnumerable<GraduateApplication> Applications { get; set; }
        public CentralQueryStats Stats { get; set; }
        public SelectList Statuses { get; set; }
        public SelectList Governorates { get; set; }

        public CentralQueryViewModel()
        {
            Applications = new List<GraduateApplication>();
            Stats = new CentralQueryStats();
        }
    }

    public class CentralQueryStats
    {
        public int TotalRecords { get; set; }
        public int PracticingCount { get; set; }
        public int TraineeCount { get; set; }
        public int DebtCount { get; set; }
        public int ResearchAcceptedCount { get; set; }
    }
}