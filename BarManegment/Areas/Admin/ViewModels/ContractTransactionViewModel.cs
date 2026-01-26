using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ContractTransactionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "تاريخ المعاملة مطلوب")]
        [Display(Name = "تاريخ المعاملة")]
        [DataType(DataType.Date)]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "معرف المحامي مطلوب")]
        [Display(Name = "معرف المحامي (رقم الهوية أو العضوية)")]
        public string LawyerIdentifier { get; set; }

        [Required(ErrorMessage = "نوع العقد مطلوب")]
        [Display(Name = "نوع العقد")]
        public int ContractTypeId { get; set; }

        [Display(Name = "قيمة العقد (للحساب النسبي)")]
        public decimal ContractValue { get; set; }

        [Display(Name = "الرسوم النهائية")]
        public decimal FinalFee { get; set; }

        [Display(Name = "معفى من الرسوم")]
        public bool IsExempt { get; set; }

        [Display(Name = "سبب الإعفاء")]
        public int? ExemptionReasonId { get; set; }

        [Display(Name = "ملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        // الوكالة الخاصة
        [Display(Name = "الموكل يوقع عن نفسه أيضاً")]
        public bool IsActingForSelf { get; set; } = true;

        [Display(Name = "صفة الموكل")]
        public string AgentLegalCapacity { get; set; }

        // القوائم الفرعية
        public List<TransactionPartyViewModel> Parties { get; set; } = new List<TransactionPartyViewModel>();
        public List<PassportMinorViewModel> Minors { get; set; } = new List<PassportMinorViewModel>();
    }

    public class TransactionPartyViewModel
    {
        [Required]
        [Display(Name = "نوع الطرف (1=أول, 2=ثاني)")]
        public int PartyType { get; set; }

        [Required(ErrorMessage = "اسم الطرف مطلوب")]
        [Display(Name = "اسم الطرف")]
        [StringLength(200)]
        public string PartyName { get; set; }

        [Required(ErrorMessage = "رقم الهوية مطلوب")]
        [Display(Name = "رقم الهوية")]
        [StringLength(50)]
        public string PartyIDNumber { get; set; }

        [Required(ErrorMessage = "المحافظة مطلوبة")]
        [Display(Name = "المحافظة")]
        public int ProvinceId { get; set; }

        [Required(ErrorMessage = "صفة الطرف مطلوبة")]
        [Display(Name = "صفة الطرف (بائع، مشتري...)")]
        public int PartyRoleId { get; set; }
    }

    public class PassportMinorViewModel
    {
        [Required(ErrorMessage = "اسم القاصر مطلوب")]
        [Display(Name = "اسم القاصر")]
        [StringLength(200)]
        public string MinorName { get; set; }

        [Required(ErrorMessage = "رقم هوية القاصر مطلوب")]
        [Display(Name = "رقم الهوية")]
        [StringLength(50)]
        public string MinorIDNumber { get; set; }

        [Required(ErrorMessage = "صفة الموكل مطلوبة")]
        [Display(Name = "صفة الموكل (ولي، وصي...)")]
        public int GuardianRoleId { get; set; }

        // 💡💡 === بداية التعديل === 💡💡
        [Required(ErrorMessage = "صفة القاصر مطلوبة")]
        [Display(Name = "صفة القاصر (ابن، ابنة...)")]
        public int MinorRelationshipId { get; set; } // (استبدال GuardianRoleId)
        // 💡💡 === نهاية التعديل === 💡💡
    }
}