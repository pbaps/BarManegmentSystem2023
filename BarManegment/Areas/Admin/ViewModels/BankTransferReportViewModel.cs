using BarManegment.Models;
using System;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class BankTransferReportViewModel
    {
        public DateTime Date { get; set; }
        public string BatchReference { get; set; }
        public string SourceBankName { get; set; }
        public string SourceAccountNumber { get; set; }
        public string SourceIBAN { get; set; }
        public decimal TotalAmount { get; set; }
        public string CurrencySymbol { get; set; }
        public List<LawyerFinancialAid> Beneficiaries { get; set; }
    }
}