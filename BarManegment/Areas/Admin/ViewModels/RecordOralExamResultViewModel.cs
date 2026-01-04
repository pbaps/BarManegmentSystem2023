using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class OralExamCommitteeViewModel
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
        public int AssignedTraineesCount { get; set; }

        // لإدارة الأعضاء
        public List<CommitteeMemberSelection> Members { get; set; }
        public SelectList AvailableMembers { get; set; } // قائمة المحامين المزاولين

        public List<string> AvailableRoles { get; set; }
        public OralExamCommitteeViewModel()
        {
            Members = new List<CommitteeMemberSelection>
            {
                new CommitteeMemberSelection { Role = "رئيس اللجنة" },
                new CommitteeMemberSelection { Role = "عضو ممتحن" },
                new CommitteeMemberSelection { Role = "عضو ممتحن" }
            };
        }
    }

    public class OralExamDetailsViewModel
    {
        public int CommitteeId { get; set; }
        public string CommitteeName { get; set; }
        public DateTime FormationDate { get; set; }
        public bool IsActive { get; set; }

        public List<BarManegment.Models.OralExamCommitteeMember> Members { get; set; }
        public List<BarManegment.Models.OralExamEnrollment> EnrolledTrainees { get; set; }

        // لتعيين متدربين جدد
        [Display(Name = "اختر المتدربين المؤهلين")]
        public List<int> SelectedTraineeIds { get; set; }
        public List<SelectListItem> AvailableTrainees { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ الامتحان")]
        public DateTime ExamDate { get; set; } = DateTime.Now;
    }

    public class AssignTraineeToOralCommitteeViewModel
    {
        public int TraineeId { get; set; }
        public string TraineeName { get; set; }

        [Required]
        [Display(Name = "لجنة الاختبار الشفوي")]
        public int SelectedCommitteeId { get; set; }
        public SelectList AvailableCommittees { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ExamDate { get; set; } = DateTime.Now;
    }

    public class RecordOralExamResultViewModel
    {
        public int EnrollmentId { get; set; }
        public int CommitteeId { get; set; } // للعودة للتفاصيل
        public string TraineeName { get; set; }
        public string CommitteeName { get; set; }
        public DateTime ExamDate { get; set; }

        [Required(ErrorMessage = "النتيجة مطلوبة")]
        public string Result { get; set; } // ناجح / راسب

        [Display(Name = "الدرجة (اختياري)")]
        public double? Score { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "ملاحظات اللجنة")]
        public string Notes { get; set; }
    }
}