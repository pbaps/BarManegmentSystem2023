using BarManegment.Models; // مطلوب لـ ContractTransaction
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ContractTransactionViewModel
    {
        [Display(Name = "تاريخ المعاملة")]
        [DataType(DataType.Date)]
        public System.DateTime TransactionDate { get; set; } = System.DateTime.Now;

        [Required(ErrorMessage = "معرف المحامي مطلوب (الرقم الوطني/العضوية/اسم المستخدم)")]
        [Display(Name = "معرف المحامي (الرقم الوطني/العضوية)")]
        public string LawyerIdentifier { get; set; }

        // (حقل مخفي لـ ID المحامي بعد التحقق منه)
        public int LawyerId { get; set; }

        [Required(ErrorMessage = "نوع العقد مطلوب")]
        [Display(Name = "نوع العقد")]
        public int ContractTypeId { get; set; }

        [Required]
        [Display(Name = "الرسوم النهائية")]
        public decimal FinalFee { get; set; }

        [Display(Name = "إعفاء من الرسوم")]
        public bool IsExempt { get; set; } = false;

        [Display(Name = "سبب الإعفاء")]
        public int? ExemptionReasonId { get; set; }

        [Display(Name = "ملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        // القائمة الديناميكية للأطراف
        public List<TransactionPartyViewModel> Parties { get; set; }

        // (اختياري: خاص بوكالة جواز السفر)
        // public List<PassportMinor> Minors { get; set; }
        // 💡💡 === بداية الإضافة === 💡💡
        [Display(Name = "بيانات القُصّر (لوكالة جواز السفر)")]
        public List<PassportMinorViewModel> Minors { get; set; }
        // 💡💡 === نهاية الإضافة === 💡💡

 

        // 💡💡 === بداية الإضافة === 💡💡
        [Display(Name = "الموكل (الطرف الأول) يوقع عن نفسه أيضاً")]
        public bool IsActingForSelf { get; set; } = true;

        [Display(Name = "صفة الموكل (في حال التوقيع عن الغير)")]
        [StringLength(250)]
        public string AgentLegalCapacity { get; set; }
        // 💡💡 === نهاية الإضافة === 💡💡
        public ContractTransactionViewModel()
        {
            Parties = new List<TransactionPartyViewModel>();
            // Minors = new List<PassportMinor>();
            Minors = new List<PassportMinorViewModel>(); // 💡 (تهيئة القائمة الجديدة)
        }
    }
}