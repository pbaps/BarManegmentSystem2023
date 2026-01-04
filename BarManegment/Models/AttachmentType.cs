using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    /// <summary>
    /// جدول مساعد لأنواع المرفقات
    /// </summary>
    public class AttachmentType
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; }
        public virtual ICollection<Attachment> Attachments { get; set; }
    }
}
