using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class Receipt
    {
        [Key, ForeignKey("PaymentVoucher")]
        public int Id { get; set; }

        // === 1. حقول الرقم التسلسلي (السنة + الرقم) ===
        [Required]
        [Display(Name = "سنة الإيصال")]
        // إنشاء فهرس لضمان عدم تكرار الرقم التسلسلي في نفس السنة
        [Index("IX_Receipt_Year_Sequence", 1, IsUnique = true)]
        public int Year { get; set; }

        [Required]
        [Display(Name = "الرقم المتسلسل")]
        [Index("IX_Receipt_Year_Sequence", 2, IsUnique = true)]
        public int SequenceNumber { get; set; }

        // خاصية للقراءة فقط لدمج السنة والرقم (مثال: 2025/001)
        [NotMapped]
        [Display(Name = "رقم الإيصال")]
        public string ReceiptNumber => $"{SequenceNumber}/{Year}";

        // === 2. تفاصيل الدفع البنكي ===
        [Required(ErrorMessage = "تاريخ السداد مطلوب")]
        [Display(Name = "تاريخ السداد في البنك")]
        [DataType(DataType.Date)]
        public DateTime BankPaymentDate { get; set; }

        [Required(ErrorMessage = "رقم وصل البنك مطلوب")]
        [Display(Name = "رقم وصل البنك")]
        [StringLength(100)]
        public string BankReceiptNumber { get; set; }

        // === 3. بيانات النظام ===
        [Display(Name = "تاريخ التسجيل")]
        public DateTime CreationDate { get; set; } = DateTime.Now;

        // 💡 الحقل الجديد الذي كان يسبب الخطأ
        [Display(Name = "ملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        // === 4. بيانات الموظف المصدر ===
        [Required]
        [Display(Name = "الموظف المصدر")]
        public int IssuedByUserId { get; set; }

        [Required]
        [StringLength(150)]
        public string IssuedByUserName { get; set; }

        // === 5. العلاقات ===
        public virtual PaymentVoucher PaymentVoucher { get; set; }
    }
}