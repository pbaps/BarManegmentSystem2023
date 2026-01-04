using System;

namespace BarManegment.Areas.Admin.ViewModels
{
    // هذا الكلاس مخصص فقط لعرض بيانات العضو في الواجهة
    public class CommitteeMemberDisplayViewModel
    {
        // نحتاج هذا الـ ID لحذف العضو من اللجنة
        public int PanelMemberId { get; set; }

        public string DisplayName { get; set; }
        public string Role { get; set; }
        public DateTime JoinDate { get; set; }
        public bool IsActive { get; set; }

        // لعرض أيقونة (محامي أو موظف)
        public string TypeIcon { get; set; }
    }
}