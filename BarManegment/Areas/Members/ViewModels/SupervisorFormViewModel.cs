using BarManegment.Models;
using System;

namespace BarManegment.Areas.Members.ViewModels
{
    public class SupervisorFormViewModel
    {
        // بيانات المتدرب (تتم تعبئتها تلقائيًا)
        public string TraineeName { get; set; }
        public string TraineeNationalId { get; set; }
        public string TraineeMobile { get; set; }
        public string TraineeUniversity { get; set; }
        public int? TraineeGradYear { get; set; }
        public string TraineePhotoPath { get; set; }
    }
}
