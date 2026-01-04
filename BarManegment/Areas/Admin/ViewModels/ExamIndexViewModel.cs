using BarManegment.Models;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ExamIndexViewModel
    {
        public List<Exam> ActiveExams { get; set; }
        public List<Exam> FinishedExams { get; set; }
        public string SearchString { get; set; }

        public ExamIndexViewModel()
        {
            ActiveExams = new List<Exam>();
            FinishedExams = new List<Exam>();
        }
    }
}
