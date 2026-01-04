using System; // <-- إضافة مهمة للتعامل مع التاريخ
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    /// <summary>
    /// المرفقات الخاصة بالطلب
    /// </summary>
    public class Attachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "مسار الملف")]
        public string FilePath { get; set; }
        // === بداية الإضافة: إضافة الحقول الناقصة ===
        [StringLength(255)]
        [Display(Name = "اسم الملف الأصلي")]
        public string OriginalFileName { get; set; }

        [Display(Name = "تاريخ الرفع")]
        public DateTime UploadDate { get; set; }
        // === نهاية الإضافة ===

        // Foreign Keys
        public int GraduateApplicationId { get; set; }
        public int AttachmentTypeId { get; set; }

        // Navigation Properties
        public virtual GraduateApplication GraduateApplication { get; set; }
        public virtual AttachmentType AttachmentType { get; set; }
    }
}
