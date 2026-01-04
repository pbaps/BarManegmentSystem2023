using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public enum CheckStatus
    {
        [Display(Name = "في الحوزة (برسم التحصيل)")]
        UnderCollection = 0,

        [Display(Name = "تم التحصيل (مودع بالبنك)")]
        Collected = 1,

        [Display(Name = "مرتجع من البنك")]
        Bounced = 2,

        [Display(Name = "معاد للمستفيد")]
        ReturnedToClient = 3
    }

    public class CheckPortfolio
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "رقم الشيك")]
        public string CheckNumber { get; set; }

        [Required]
        [Display(Name = "اسم البنك المسحوب عليه")]
        public string BankName { get; set; }

        [Required]
        [Display(Name = "تاريخ الاستحقاق")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        [Display(Name = "المبلغ")]
        public decimal Amount { get; set; }

        [Display(Name = "العملة")]
        public int? CurrencyId { get; set; }
        public virtual Currency Currency { get; set; }

        [Display(Name = "حالة الشيك")]
        public CheckStatus Status { get; set; } = CheckStatus.UnderCollection;

        // الربط مع السند الأصلي
        [Display(Name = "رقم سند القبض")]
        public int? ReceiptId { get; set; }
        public virtual Receipt Receipt { get; set; }

        // اسم صاحب الشيك (المحامي أو المتعهد)
        [Display(Name = "اسم الساحب")]
        public string DrawerName { get; set; }

        [Display(Name = "تاريخ التحصيل/الإجراء")]
        public DateTime? ActionDate { get; set; }

        // القيد الذي أثبت عملية التحصيل أو الارتجاع
        public int? ActionJournalEntryId { get; set; }
    }
}