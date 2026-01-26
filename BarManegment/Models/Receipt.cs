using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class Receipt
    {
        [Key, ForeignKey("PaymentVoucher")]
        public int Id { get; set; }

        // === 1. حقول الرقم التسلسلي ===
        [Required]
        [Display(Name = "سنة الإيصال")]
        [Index("IX_Receipt_Year_Sequence", 1, IsUnique = true)]
        public int Year { get; set; }

        [Required]
        [Display(Name = "الرقم المتسلسل")]
        [Index("IX_Receipt_Year_Sequence", 2, IsUnique = true)]
        public int SequenceNumber { get; set; }

        [NotMapped]
        [Display(Name = "رقم الإيصال")]
        public string ReceiptNumber => $"{SequenceNumber}/{Year}";

        // === 2. تفاصيل الدفع البنكي ===
        [Required]
        [Display(Name = "تاريخ السداد في البنك")]
        [DataType(DataType.Date)]
        public DateTime BankPaymentDate { get; set; }

        [Required]
        [Display(Name = "رقم وصل البنك")]
        [StringLength(100)]
        public string BankReceiptNumber { get; set; }

        // === 3. بيانات النظام ===
        [Display(Name = "تاريخ التسجيل")]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        [Display(Name = "ملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        // === 4. بيانات الموظف المصدر ===
        [Required]
        public int IssuedByUserId { get; set; }

        [Required]
        [StringLength(150)]
        public string IssuedByUserName { get; set; }

        // === 5. العلاقات ===
        public virtual PaymentVoucher PaymentVoucher { get; set; }

        // ✅✅✅ الإضافات الجديدة لحل الأخطاء (هذه الحقول غير موجودة في الجدول، بل نعتمد على PaymentVoucher) ✅✅✅
        // ولكن للتسهيل في الـ ViewModel والتعامل، سنضيف خصائص "غير مخصصة" (NotMapped) 
        // أو إذا كنت تريد تخزينها فعلياً، أزل [NotMapped] وقم بـ Migration.
        // الأفضل هنا هو الاعتماد على PaymentVoucher للقيم المالية، ولكن سأضيفها كـ NotMapped للحل السريع للأخطاء البرمجية.

        [NotMapped]
        public DateTime ReceiptDate { get { return BankPaymentDate; } set { BankPaymentDate = value; } }

        [NotMapped]
        public decimal Amount { get; set; } // سنملؤها يدوياً من PaymentVoucher عند العرض

        [NotMapped]
        public string PayerName { get; set; } // اسم المحامي

        [NotMapped]
        public string Description { get; set; }

        [NotMapped]
        public string PaymentMethod { get; set; } = "Bank"; // بما أن هناك رقم وصل بنك

        [NotMapped]
        public DateTime CreatedAt { get { return CreationDate; } set { CreationDate = value; } }

        [NotMapped]
        public int? PaymentVoucherId { get { return Id; } set { Id = value ?? 0; } } // لأن العلاقة 1:1
    }
}