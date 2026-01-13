using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    // =========================================================
    // 1. إنشاء قسيمة (Create Voucher) - محامي/متدرب
    // =========================================================
    public class CreateVoucherViewModel
    {
        [Required]
        public int GraduateApplicationId { get; set; }

        [Display(Name = "اسم المستفيد")]
        public string TraineeName { get; set; }

        [Display(Name = "الحالة الحالية")]
        public string CurrentTraineeStatus { get; set; }

        [Required(ErrorMessage = "تاريخ الانتهاء مطلوب")]
        [Display(Name = "تاريخ انتهاء الصلاحية")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(7);

        [Required(ErrorMessage = "الرجاء تحديد طريقة الدفع")]
        [Display(Name = "طريقة الدفع")]
        public string PaymentMethod { get; set; } = "نقدي";

        public string CurrencySymbol { get; set; }

        public decimal TotalAmount { get; set; }

        public List<FeeSelection> Fees { get; set; } = new List<FeeSelection>();
    }

    // =========================================================
    // 2. إنشاء إيصال سداد (Create Receipt)
    // =========================================================
    public class CreateReceiptViewModel
    {
        [Required]
        public int PaymentVoucherId { get; set; }

        public string TraineeName { get; set; }
        public string CurrentTraineeStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrencySymbol { get; set; }

        [Required]
        [Display(Name = "تاريخ السداد البنكي")]
        [DataType(DataType.Date)]
        public DateTime BankPaymentDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "رقم الإيصال البنكي")]
        public string BankReceiptNumber { get; set; }

        public string Notes { get; set; }

        public bool ActivateTrainee { get; set; }

        public List<ReceiptVoucherDetail> Details { get; set; } = new List<ReceiptVoucherDetail>();
    }

    public class ReceiptVoucherDetail
    {
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; }
        public decimal Amount { get; set; }
        public string CurrencySymbol { get; set; }
        public int BankAccountId { get; set; }
        public string BankName { get; set; }
        public string BankReceiptNumber { get; set; }
    }

    // =========================================================
    // 3. الكلاس المساعد لاختيار الرسوم (مشترك)
    // =========================================================
    public class FeeSelection
    {
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; }
        public decimal Amount { get; set; }
        public string CurrencySymbol { get; set; }
        public bool IsSelected { get; set; }
        public int BankAccountId { get; set; }
        public string BankName { get; set; }
        public string BankAccountName { get; set; }
        public string BankAccountNumber { get; set; }
        public string Iban { get; set; }
    }

    // =========================================================
    // 4. قسائم المتعهدين (Contractors)
    // =========================================================
    public class CreateContractorVoucherViewModel
    {
        [Required(ErrorMessage = "الرجاء اختيار المتعهد")]
        [Display(Name = "المتعهد")]
        public int SelectedContractorId { get; set; }

        [Required(ErrorMessage = "الرجاء اختيار دفتر واحد على الأقل")]
        [Display(Name = "الدفاتر المطلوب حجزها")]
        public List<int> SelectedBookIds { get; set; } = new List<int>();

        public SelectList ContractorsList { get; set; }
        public List<StampBook> AvailableBooksList { get; set; } = new List<StampBook>();
    }

    // =========================================================
    // 5. القسائم العامة (General / External)
    // =========================================================
    public class CreateGeneralVoucherViewModel
    {
        [Required(ErrorMessage = "يرجى إدخال اسم الجهة / المستفيد")]
        [Display(Name = "اسم المستفيد / الجهة")]
        public string PayerName { get; set; }

        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "طريقة الدفع")]
        public string PaymentMethod { get; set; } = "نقدي";

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; }

        public List<FeeSelection> Fees { get; set; } = new List<FeeSelection>();
    }

    // =========================================================
    // 6. نموذج العرض الرئيسي (Index)
    // =========================================================
    public class VoucherIndexViewModel
    {
        public List<PaymentVoucher> UnpaidTraineeVouchers { get; set; }
        public List<ContractorVoucherDisplay> UnpaidContractorVouchers { get; set; }
        public List<PaymentVoucher> UnpaidGeneralVouchers { get; set; }
        public List<PaymentVoucher> PaidVouchers { get; set; }
    }

    public class ContractorVoucherDisplay
    {
        public PaymentVoucher Voucher { get; set; }
        public string ContractorName { get; set; }
    }

    // =========================================================
    // 7. نموذج الطباعة (Print)
    // =========================================================
    public class PrintVoucherViewModel
    {
        public int VoucherId { get; set; }
        public string TraineeName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string IssuedByUserName { get; set; }
        public List<VoucherPrintDetail> Details { get; set; }
    }

    public class VoucherPrintDetail
    {
        public string FeeTypeName { get; set; }
        public decimal Amount { get; set; }
        public string CurrencySymbol { get; set; }
        public string BankName { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string Iban { get; set; }
    }

    // =========================================================
    // 8. نموذج طباعة إيصال الطوابع (Stamp Issuance)
    // =========================================================
    public class StampIssuanceReceiptViewModel
    {
        public int ReceiptId { get; set; }
        public string ReceiptFullNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ContractorName { get; set; }
        public string IssuedByUserName { get; set; }
        public decimal TotalAmount { get; set; }
        public string TotalAmountInWords { get; set; }
        public string CurrencySymbol { get; set; }
        public string BankReceiptNumber { get; set; }
        public List<StampIssuanceReceiptDetail> Details { get; set; }

        public StampIssuanceReceiptViewModel()
        {
            Details = new List<StampIssuanceReceiptDetail>();
        }
    }

    public class StampIssuanceReceiptDetail
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}