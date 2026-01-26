using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class LoanApplicationViewModel
    { 
        public int Id { get; set; }
        [Required(ErrorMessage = "معرف المحامي مطلوب")]
        [Display(Name = "معرف المحامي (الرقم الوطني/العضوية)")]
        public string LawyerIdentifier { get; set; }

        [Required(ErrorMessage = "نوع القرض مطلوب")]
        [Display(Name = "نوع القرض")]
        public int LoanTypeId { get; set; }

        [Required]
        [Display(Name = "مبلغ القرض")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "عدد الأقساط (أشهر)")]
        public int InstallmentCount { get; set; }

        [Required]
        [Display(Name = "تاريخ بدء أول قسط")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(1);

        [Required]
        [Display(Name = "تاريخ تقديم الطلب")]
        [DataType(DataType.Date)]
        public DateTime ApplicationDate { get; set; } = DateTime.Now;

        [Display(Name = "الحالة")]
        public string Status { get; set; }

        // 💡💡 === بداية الإضافة: حقل الملاحظات/سبب القرض === 💡💡
        [Display(Name = "سبب القرض / ملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
        // 💡💡 === نهاية الإضافة === 💡💡

        // --- المرفقات (تم إزالة [Required] لدعم الطباعة أولاً ثم الرفع) ---

        [Display(Name = "1. طلب الحصول على قرض")]
        public HttpPostedFileBase ApplicationFormFile { get; set; }

        [Display(Name = "2. قرار مجلس بالموافقة (إن وجد)")]
        public HttpPostedFileBase CouncilApprovalFile { get; set; }

        [Display(Name = "3. كمبيالة بالمبلغ الكلي")]
        public HttpPostedFileBase MainPromissoryNoteFile { get; set; }

        [Display(Name = "4. سند مديونية")]
        public HttpPostedFileBase DebtBondFile { get; set; }

        // --- الكفلاء (قائمة ديناميكية) ---
        public List<GuarantorViewModel> Guarantors { get; set; }

        // --- قوائم منسدلة ---
        public SelectList LoanTypesList { get; set; }

        public LoanApplicationViewModel()
        {
            Guarantors = new List<GuarantorViewModel>();
        }
    }
}