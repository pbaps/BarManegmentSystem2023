using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    /// <summary>
    /// يمثل عضو مجلس النقابة المخول بالتوقيع
    /// </summary>
    public class CouncilMember
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم العضو مطلوب")]
        [Display(Name = "اسم العضو")]
        [StringLength(150)]
        public string Name { get; set; }

        [Required(ErrorMessage = "صفة العضو مطلوبة")]
        [Display(Name = "الصفة / المنصب")]
        [StringLength(100)]
        public string Title { get; set; } // مثال: نائب نقيب المحامين، أمين السر , امينن الصندوق، عضو مجلس

        [Display(Name = "الحالة")]
        public bool IsActive { get; set; } = true;
    }
}
