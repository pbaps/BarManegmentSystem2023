using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    // 1. النموذج الفرعي (لتفاصيل الجدول)
    public class ReceiptVoucherDetail
    {
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; }
        public decimal Amount { get; set; }
        public string CurrencySymbol { get; set; }
        public int BankAccountId { get; set; }
        public string BankName { get; set; }

        [Display(Name = "رقم إيصال البنك")]
        public string BankReceiptNumber { get; set; }
    }

    // 2. النموذج الرئيسي (لصفحة الإنشاء)
    public class CreateReceiptViewModel
    {
        // 💡 التعديل 1: تغيير الاسم ليتطابق مع الكنترولر
        [Required]
        public int PaymentVoucherId { get; set; }

        public string TraineeName { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrencySymbol { get; set; }

        [Required(ErrorMessage = "تاريخ السداد مطلوب")]
        [DataType(DataType.Date)]
        [Display(Name = "تاريخ السداد البنكي")]
        public DateTime BankPaymentDate { get; set; } = DateTime.Now;

        // 💡 التعديل 2: إضافة رقم الوصل الرئيسي وملاحظات
        [Required(ErrorMessage = "رقم وصل البنك مطلوب")]
        [Display(Name = "رقم وصل البنك (الرئيسي)")]
        public string BankReceiptNumber { get; set; }

        [Display(Name = "ملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "الحالة الحالية للمستخدم")]
        public string CurrentTraineeStatus { get; set; }

        [Display(Name = "تفعيل التسجيل (مقابل الرسوم المدفوعة)")]
        public bool ActivateTrainee { get; set; }

        // 💡 التعديل 3: قائمة لعرض الحسابات البنكية المطلوبة
        public List<string> TargetBankAccounts { get; set; }

        [Required]
        public List<ReceiptVoucherDetail> Details { get; set; }

        public CreateReceiptViewModel()
        {
            TargetBankAccounts = new List<string>();
            Details = new List<ReceiptVoucherDetail>();
        }
    }
}