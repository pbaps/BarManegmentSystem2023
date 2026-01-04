using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    /// <summary>
    /// يمثل سجل تجديد المزاولة السنوي للمحامي المزاول
    /// </summary>
    public class PracticingLawyerRenewal
    {
        [Key]
        public int Id { get; set; }

        // === 1. الربط بالمحامي ===
        [Required]
        [Display(Name = "المحامي")]
        public int GraduateApplicationId { get; set; }

        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication GraduateApplication { get; set; } // (تم توحيد الاسم ليتوافق مع باقي النظام)

        // === 2. بيانات السنة ===
        [Required]
        [Display(Name = "سنة التجديد")]
        public int RenewalYear { get; set; }

        [Display(Name = "تاريخ التسجيل")]
        public DateTime RenewalDate { get; set; } = DateTime.Now; // تاريخ إنشاء السجل

        // === 3. الحالة والقسيمة (مرحلة ما قبل الدفع) ===
        [Display(Name = "حالة التجديد")]
        public bool IsActive { get; set; } = false; // False = قسيمة صادرة، True = تم الدفع

        [Display(Name = "رقم القسيمة")]
        public int? PaymentVoucherId { get; set; }

        [ForeignKey("PaymentVoucherId")]
        public virtual PaymentVoucher PaymentVoucher { get; set; }

        // === 4. الإيصال والدفع (مرحلة ما بعد الدفع) ===
        [Display(Name = "تاريخ الدفع الفعلي")]
        public DateTime? PaymentDate { get; set; } // (Nullable لأن الدفع لم يتم بعد عند الإنشاء)

        [Display(Name = "رقم إيصال القبض")]
        public int? ReceiptId { get; set; }

        [ForeignKey("ReceiptId")]
        public virtual Receipt Receipt { get; set; }

        [Display(Name = "ملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }
    }
}