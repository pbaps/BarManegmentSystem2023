using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    // 1. للقائمة والإنشاء
    public class OathCeremonyViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "تاريخ الحفل مطلوب")]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الحفل")]
        public DateTime CeremonyDate { get; set; } = DateTime.Now.AddDays(7);

        [Required(ErrorMessage = "المكان مطلوب")]
        [Display(Name = "المكان/القاعة")]
        public string Location { get; set; } = "مقر النقابة الرئيسي - قاعة المؤتمرات";

        [Display(Name = "فعالة (متاحة للتسجيل)")]
        public bool IsActive { get; set; } = true;

        public int AttendeesCount { get; set; }
    }

    // 2. للتفاصيل وإضافة المتدربين
    public class OathCeremonyDetailsViewModel
    {
        public int CeremonyId { get; set; }
        public DateTime CeremonyDate { get; set; }
        public string Location { get; set; }
        public bool IsActive { get; set; }

        // الحضور المسجلين
        public List<BarManegment.Models.GraduateApplication> AssignedAttendees { get; set; }

        // لإضافة متدربين جدد
        [Display(Name = "اختر المتدربين الجاهزین لليمن")]
        public List<int> SelectedTraineeIds { get; set; }

        // القائمة المنسدلة (متدربين دفعوا الرسوم وبانتظار الموعد)
        public IEnumerable<SelectListItem> AvailableTrainees { get; set; }

        // === بداية الإضافة: الخاصية المفقودة ===
        [Display(Name = "تاريخ الاختبار (لهذه الدفعة)")]
        [DataType(DataType.Date)]
        // [Required(ErrorMessage = "الرجاء تحديد تاريخ الاختبار للمتدربين الجدد")] // يمكنك تفعيل هذا إذا كان الحقل إلزامياً في النموذج
        public DateTime ExamDate { get; set; } = DateTime.Now.Date;
        // === نهاية الإضافة ===

        public OathCeremonyDetailsViewModel()
        {
            AssignedAttendees = new List<BarManegment.Models.GraduateApplication>();
            SelectedTraineeIds = new List<int>();
            AvailableTrainees = new List<SelectListItem>();
        }
    }

    // 3. للطباعة (كشف الحضور)
    public class OathAttendeesViewModel
    {
        public int CeremonyId { get; set; }
        public DateTime CeremonyDate { get; set; }
        public string Location { get; set; }
        public List<BarManegment.Models.GraduateApplication> Attendees { get; set; }
        public List<BarManegment.Models.CouncilMember> SigningMembers { get; set; }
    }
}