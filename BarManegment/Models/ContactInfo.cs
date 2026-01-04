using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    /// <summary>
    /// يمثل بيانات الاتصال الخاصة بطلب الخريج
    /// </summary>
    public class ContactInfo
    {
        // --- بداية التعديل ---
        // الآن، المفتاح الأساسي هو نفسه المفتاح الأجنبي
        [Key, ForeignKey("GraduateApplication")]
        public int Id { get; set; }
        // --- نهاية التعديل ---

        [Display(Name = "المحافظة")]
        [StringLength(100)]
        public string Governorate { get; set; }

        [Display(Name = "المدينة/الحي")]
        [StringLength(100)]
        public string City { get; set; }

        [Display(Name = "الشارع")]
        [StringLength(200)]
        public string Street { get; set; }

        [Display(Name = "رقم البناية")]
        [StringLength(20)]
        public string BuildingNumber { get; set; }

        [Display(Name = "رقم الجوال")]
        [StringLength(20)]
        public string MobileNumber { get; set; }

        [Display(Name = "رقم الوطنية (موبايل)")]
        [StringLength(20)]
        public string NationalMobileNumber { get; set; }

        [Display(Name = "رقم الهاتف (أرضي)")]
        [StringLength(20)]
        public string HomePhoneNumber { get; set; }

        [Display(Name = "رقم الواتساب")]
        [StringLength(20)]
        public string WhatsAppNumber { get; set; }

        [Display(Name = "البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "الرجاء إدخال بريد إلكتروني صحيح.")]
        [StringLength(100)]
        public string Email { get; set; }

        [Display(Name = "اسم شخص للطوارئ")]
        [StringLength(100)]
        public string EmergencyContactPerson { get; set; }

        [Display(Name = "رقم اتصال للطوارئ")]
        [StringLength(20)]
        public string EmergencyContactNumber { get; set; }

        // --- بداية التعديل ---
        // تم حذف الخاصية GraduateApplicationId لأنها غير ضرورية الآن
        public virtual GraduateApplication GraduateApplication { get; set; }
        // --- نهاية التعديل ---
    }
}

