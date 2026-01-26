using BarManegment.Models;
using System;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class ContractPrintViewModel
    {
        public int TransactionId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string LawyerName { get; set; }
        public string LawyerMembershipId { get; set; }
        public string ContractTypeName { get; set; }
        public string EmployeeName { get; set; }
        public int VoucherId { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrencySymbol { get; set; }
        public string TotalAmountInWords { get; set; }
        public string ReceiptFullNumber { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string BankReceiptNumber { get; set; }
        public string PaymentMethod { get; set; }
        public string IssuedByUserName { get; set; }

        public bool IsActingForSelf { get; set; }
        public string AgentLegalCapacity { get; set; }

        public List<TransactionParty> Parties { get; set; } = new List<TransactionParty>();
        public List<PassportMinor> Minors { get; set; } = new List<PassportMinor>();
        public List<VoucherDetail> Details { get; set; } = new List<VoucherDetail>();
    }
}