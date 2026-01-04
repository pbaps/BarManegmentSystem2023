using BarManegment.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ContractPrintViewModel
    {
        // --- بيانات المحامي والمعاملة ---
        public int TransactionId { get; set; }
        [Display(Name = "تاريخ التصديق (المعاملة)")]
        public DateTime TransactionDate { get; set; }
        public string LawyerName { get; set; }
        public string LawyerMembershipId { get; set; }
        public string ContractTypeName { get; set; }
        public string EmployeeName { get; set; }

        // --- بيانات القسيمة ---
        public int VoucherId { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrencySymbol { get; set; }
        public string TotalAmountInWords { get; set; }

        // --- بيانات الإيصال ---
        public string ReceiptFullNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string BankReceiptNumber { get; set; }

        // --- بيانات الأطراف والتفاصيل ---
        public List<TransactionParty> Parties { get; set; }
        public List<VoucherDetail> Details { get; set; }

        // 💡💡 === بداية الإضافة (بيانات وكالة السفر) === 💡💡
        [Display(Name = "الموكل يوقع عن نفسه أيضاً")]
        public bool IsActingForSelf { get; set; }

        [Display(Name = "القُصّر")]
        public List<PassportMinor> Minors { get; set; }

        [Display(Name = "صفة الموكل")]
        public string AgentLegalCapacity { get; set; }
        // 💡💡 === نهاية الإضافة === 💡💡

        public ContractPrintViewModel()
        {
            Parties = new List<TransactionParty>();
            Details = new List<VoucherDetail>();
            Minors = new List<PassportMinor>();
        }
    }
}