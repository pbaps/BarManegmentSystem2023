using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.ViewModels
{
    public class CreateVoucherViewModel
    {
        public int? GraduateApplicationId { get; set; }
        public string TraineeName { get; set; }

        [Required(ErrorMessage = "تاريخ الانتهاء مطلوب")]
        [Display(Name = "تاريخ انتهاء صلاحية القسيمة")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(7); // قيمة افتراضية مثلاً
        // --- 💡 الإضافة الجديدة ---
        [Required(ErrorMessage = "الرجاء تحديد طريقة الدفع")]
        public string PaymentMethod { get; set; }
        // --- نهاية الإضافة ---
        public List<FeeSelection> Fees { get; set; }

        // *** تمت إضافة هذه الخاصية ***
        public string SpecificFeeName { get; set; } // لتمرير اسم الرسم المحدد (إن وجد)

        public CreateVoucherViewModel()
        {
            Fees = new List<FeeSelection>();
        }
    }

    // في ملف BarManegment/Areas/Admin/ViewModels/CreateVoucherViewModel.cs

    public class FeeSelection
    {
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; }
        public decimal Amount { get; set; }
        public string CurrencySymbol { get; set; }
        public bool IsSelected { get; set; }

        // ====> قم بإضافة الحقل التالي <====
        public int BankAccountId { get; set; } // مهم جداً

        public string BankAccountName { get; set; }
        public string BankAccountNumber { get; set; }
        public string Iban { get; set; }
        public string BankName { get; set; } // <-- ✅ أضف هذا السطر هنا
    }
}
