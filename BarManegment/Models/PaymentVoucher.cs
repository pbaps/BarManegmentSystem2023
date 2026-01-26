using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class PaymentVoucher
    {
        [Key]
        public int Id { get; set; }

        // === 💡 التعديل: إضافة علامة الاستفهام (?) ليقبل Null ===
        [Display(Name = "المستفيد (الخريج/المحامي)")]
        public int? GraduateApplicationId { get; set; }

        [ForeignKey("GraduateApplicationId")]
        public virtual GraduateApplication GraduateApplication { get; set; }
        // ========================================================

        // --- 💡 طريقة الدفع ---
        [Display(Name = "طريقة الدفع")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } // سيخزن "نقدي" أو "بنكي" أو "شيك"

        // ⬇️⬇️ الإضافة المطلوبة لحل الخطأ ⬇️⬇️
        [Display(Name = "رقم الشيك / المرجع")]
        [StringLength(50)]
        public string CheckNumber { get; set; }

        [Display(Name = "رقم مرجعي")]
        [StringLength(50)]
        public string ReferenceNumber { get; set; }
        // ⬆️⬆️ ---------------------------- ⬆️⬆️

        [Display(Name = "الإجمالي")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Display(Name = "تاريخ الإصدار")]
        public DateTime IssueDate { get; set; }

        [Required]
        [Display(Name = "تاريخ الانتهاء")]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [Display(Name = "الحالة")]
        public string Status { get; set; } // صادر، مسدد، ملغى

        // === حقول الموظف المصدر للقسيمة ===
        [Display(Name = "أصدر بواسطة")]
        public int IssuedByUserId { get; set; }

        [Required]
        [StringLength(150)]
        public string IssuedByUserName { get; set; }

        public virtual Receipt Receipt { get; set; }

        public virtual ICollection<VoucherDetail> VoucherDetails { get; set; }

 

        // ✅ أضف هذا إذا لم يكن موجوداً
        public bool IsPaid { get; set; }

        public PaymentVoucher()
        {
            VoucherDetails = new HashSet<VoucherDetail>();
        }
    }
}