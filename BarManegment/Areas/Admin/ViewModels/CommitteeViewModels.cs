using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class CommitteeViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم اللجنة مطلوب")]
        [Display(Name = "اسم اللجنة")]
        public string CommitteeName { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ التشكيل")]
        public DateTime FormationDate { get; set; } = DateTime.Now;

        [Display(Name = "لجنة فعالة")]
        public bool IsActive { get; set; } = true;

        public int MemberCount { get; set; }
        public int AssignedResearchesCount { get; set; }

        public List<CommitteeMemberSelection> Members { get; set; }
        public SelectList AvailableMembers { get; set; }
        public List<string> AvailableRoles { get; set; }

        public CommitteeViewModel()
        {
            Members = new List<CommitteeMemberSelection>();
            AvailableRoles = new List<string> { "رئيس اللجنة", "عضو مناقش", "مشرف البحث", "عضو إضافي" };
        }
    }

    public class CommitteeMemberSelection
    {
        [Required(ErrorMessage = "يجب اختيار عضو")]
        public int MemberLawyerId { get; set; }

        [Required(ErrorMessage = "يجب تحديد الدور")]
        public string Role { get; set; }
    }

    // 💡 الإضافة الجديدة: نموذج تفاصيل اللجنة لإضافة الأبحاث
    public class CommitteeDetailsViewModel
    {
        public int CommitteeId { get; set; }
        public string CommitteeName { get; set; }
        public DateTime FormationDate { get; set; }
        public bool IsActive { get; set; }

        // الأعضاء الحاليين
        public List<BarManegment.Models.CommitteeMember> Members { get; set; }

        // الأبحاث المعينة لهذه اللجنة
        public List<BarManegment.Models.LegalResearch> AssignedResearches { get; set; }

        // لتعيين أبحاث جديدة (نفس آلية الاختبار الشفوي)
        [Display(Name = "اختر الأبحاث للمناقشة")]
        public List<int> SelectedResearchIds { get; set; }

        // قائمة الأبحاث المتاحة (التي حالتها "مُقدم" ولم تُعين للجنة بعد)
        public List<SelectListItem> AvailableResearches { get; set; }
    }
}