using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ReportViewModel
    {
        // 1. إعدادات التقرير
        public string ReportType { get; set; } // (Graduates, Contracts, Financial...)

        // 2. الفلاتر المشتركة (Common Filters)
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }

        public string SearchKeyword { get; set; } // بحث عام (اسم، رقم هوية)

        // فلاتر القوائم (Dropdowns IDs)
        public int? StatusId { get; set; }
        public int? TypeId { get; set; } // (نوع عقد، نوع مستخدم، نوع امتحان...)
        public int? GenderId { get; set; }
        public int? BranchId { get; set; } // محافظة أو فرع

        // 3. تحديد الأعمدة (Column Selection)
        public List<string> SelectedColumns { get; set; } // أسماء الخصائص المختارة للعرض
        public Dictionary<string, string> AvailableColumns { get; set; } // <الاسم البرمجي, الاسم العربي>
                                                                         // === 💡 الإضافات الجديدة ===
        public string SelectedGovernorate { get; set; } // لأن المحافظة نص في ContactInfo
        public string SelectedCity { get; set; } // المدينة
        public int? QualificationTypeId { get; set; } // نوع الشهادة
        public int? RegistrationYear { get; set; } // سنة التسجيل
        // 4. النتائج (Data)
        // نستخدم dynamic للسماح بمرونة عالية، أو List<object>
        public List<dynamic> Results { get; set; }

        // === 💡 فلاتر السجل العائلي والصحي ===
        public string SelectedMaritalStatus { get; set; } // الحالة الاجتماعية
        public string SelectedHealthStatus { get; set; } // الحالة الصحية
        public string SelectedDisplacement { get; set; } // محافظة النزوح
        public bool? IsDetainedFilter { get; set; } // هل تعرض للاعتقال؟
        public bool? HasInsuranceFilter { get; set; } // هل لديه تأمين؟

        public ReportViewModel()
        {
            SelectedColumns = new List<string>();
            AvailableColumns = new Dictionary<string, string>();
            Results = new List<dynamic>();
        }
    }
}