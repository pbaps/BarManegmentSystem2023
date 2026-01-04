using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Web;

namespace BarManegment.Areas.Members.ViewModels
{
    // --- 1. ViewModel لعرض قائمة الرسائل (Inbox/Outbox) ---
    public class MessageListItemViewModel
    {
        public int Id { get; set; }

        [Display(Name = "الموضوع")]
        public string Subject { get; set; }

        [Display(Name = "المرسل")]
        public string SenderName { get; set; }

        [Display(Name = "المستقبل")]
        public string RecipientName { get; set; }

        [Display(Name = "التاريخ")]
        public DateTime Timestamp { get; set; }

        [Display(Name = "مقروءة")]
        public bool IsRead { get; set; }

        [Display(Name = "مرفقات")]
        public bool HasAttachment { get; set; }

        [Display(Name = "عدد الردود")]
        public int ReplyCount { get; set; }
    }

    // --- 2. ViewModel لإنشاء رسالة جديدة (Compose) ---
    public class ComposeMessageViewModel
    {
        public int SenderId { get; set; }

        [Display(Name = "إلى (الرقم الوطني/العضوية)")]
        [Required(ErrorMessage = "تحديد المرسل إليه مطلوب")]
        public string RecipientIdentifier { get; set; }

        // (حقل مخفي لربط الردود)
        public int? ParentMessageId { get; set; }

        [Required(ErrorMessage = "الموضوع مطلوب")]
        [StringLength(255)]
        [Display(Name = "الموضوع")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "محتوى الرسالة مطلوب")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "الرسالة")]
        public string Body { get; set; }

        [Display(Name = "إرفاق ملف (اختياري)")]
        public IEnumerable<HttpPostedFileBase> Files { get; set; }

        [Display(Name = "اسم المستلم")]
        public string RecipientNameDisplay { get; set; }
    }

    // --- 3. ViewModel لعرض سلسلة رسائل واحدة (Details/Reply) ---
    public class MessageThreadViewModel
    {
        public int ThreadId { get; set; }
        public string Subject { get; set; }

        // الرسالة الأصلية أو الرسائل المتسلسلة
        public List<InternalMessage> Messages { get; set; }

        // نموذج الرد السريع
        public ComposeMessageViewModel ReplyModel { get; set; }
    }
}