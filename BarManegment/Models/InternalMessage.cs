using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    public class InternalMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "الموضوع")]
        [StringLength(255)]
        public string Subject { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "محتوى الرسالة")]
        public string Body { get; set; }

        [Display(Name = "التاريخ")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // --- تسلسل الرسائل والردود (Thread Tracking) ---

        [Display(Name = "رسالة أصلية")]
        public int? ParentMessageId { get; set; } // للإشارة إلى الرسالة الأصلية في حالة الرد
        [ForeignKey("ParentMessageId")]
        public virtual InternalMessage ParentMessage { get; set; }

        // --- المرسل والمستقبل (الربط بجدول المستخدمين) ---

        [Required]
        public int SenderId { get; set; }
        [ForeignKey("SenderId")]
        public virtual UserModel Sender { get; set; }

        [Required]
        public int RecipientId { get; set; }
        [ForeignKey("RecipientId")]
        public virtual UserModel Recipient { get; set; }

        // --- الحالة والمرفقات ---

        [Display(Name = "مقروءة")]
        public bool IsRead { get; set; } = false;

        [Display(Name = "تحتوي مرفقات")]
        public bool HasAttachment { get; set; } = false;

        // Navigation property for replies and attachments
        public virtual ICollection<MessageAttachment> Attachments { get; set; }
        public virtual ICollection<InternalMessage> Replies { get; set; }
    }

    public class MessageAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InternalMessageId { get; set; }
        [ForeignKey("InternalMessageId")]
        public virtual InternalMessage Message { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "اسم الملف الأصلي")]
        public string OriginalFileName { get; set; }

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; }
    }
}