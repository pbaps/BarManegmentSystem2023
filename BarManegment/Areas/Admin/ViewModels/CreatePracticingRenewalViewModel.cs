using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class CreatePracticingRenewalViewModel
    {
        // بيانات المحامي (للعرض)
        public int LawyerId { get; set; }
        public string LawyerName { get; set; }
        public string LawyerMembershipId { get; set; }

        // بيانات مطلوبة للإنشاء
        [Required(ErrorMessage = "الرجاء إدخال سنة التجديد")]
        [Display(Name = "سنة التجديد")]
        public int RenewalYear { get; set; }

        // === 
        // === بداية التعديل: استخدام قائمة الرسوم
        // ===
        // === 
        // === بداية الإضافة: إضافة تاريخ الصلاحية
        // ===
        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "تاريخ انتهاء صلاحية القسيمة")]
        public DateTime ExpiryDate { get; set; }
        // === نهاية الإضافة ===

        // (هذه القائمة ستعرض للموظف ليختار منها)
        public List<FeeSelectionViewModel> AvailableFees { get; set; }

        // (لإضافة ملاحظات اختيارية للقسيمة)
        [Display(Name = "ملاحظات (اختياري)")]
        public string VoucherNotes { get; set; }
        // === نهاية التعديل ===

        public CreatePracticingRenewalViewModel()
        {
            AvailableFees = new List<FeeSelectionViewModel>();
        }
    }
}