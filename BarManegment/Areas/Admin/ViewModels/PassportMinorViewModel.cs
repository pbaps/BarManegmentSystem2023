using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    // نموذج لتمثيل بيانات القاصر في شاشة إنشاء المعاملة
    public class PassportMinorViewModel
    {
        [Required(ErrorMessage = "اسم القاصر مطلوب")]
        [Display(Name = "اسم القاصر")]
        [StringLength(200)]
        public string MinorName { get; set; }

        [Required(ErrorMessage = "رقم هوية القاصر مطلوب")]
        [Display(Name = "رقم الهوية")]
        [StringLength(50)]
        public string MinorIDNumber { get; set; }

        [Required(ErrorMessage = "صفة الموكل مطلوبة")]
        [Display(Name = "صفة الموكل (ولي، وصي...)")]
        public int GuardianRoleId { get; set; }

        // 💡💡 === بداية التعديل === 💡💡
        [Required(ErrorMessage = "صفة القاصر مطلوبة")]
        [Display(Name = "صفة القاصر (ابن، ابنة...)")]
        public int MinorRelationshipId { get; set; } // (استبدال GuardianRoleId)
        // 💡💡 === نهاية التعديل === 💡💡
    }
}