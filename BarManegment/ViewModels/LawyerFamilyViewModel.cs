using BarManegment.Models;
using System.Collections.Generic;
using System.Web;

namespace BarManegment.ViewModels
{
    public class LawyerFamilyViewModel
    {
        public int LawyerId { get; set; }
        public string LawyerName { get; set; }

        // استخدام الموديلات المعتمدة
        public LawyerPersonalData PersonalData { get; set; }
        public SecurityHealthRecord HealthRecord { get; set; }

        public List<LawyerSpouse> SpousesList { get; set; }
        public List<LawyerChild> ChildrenList { get; set; }

        // الأدوية كنص (لأن جدول الأدوية حذفناه)
        public string MedicationsText { get; set; }

        public HttpPostedFileBase MedicalReportFile { get; set; }
        public HttpPostedFileBase DetentionProofFile { get; set; }

        public LawyerFamilyViewModel()
        {
            PersonalData = new LawyerPersonalData();
            HealthRecord = new SecurityHealthRecord();
            SpousesList = new List<LawyerSpouse>();
            ChildrenList = new List<LawyerChild>();
        }
    }
}