using System;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.ViewModels
{
    public class ExamApplicationViewModel
    {
        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "الرقم الوطني مطلوب")]
        [Display(Name = "الرقم الوطني")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "الرجاء إدخال رقم هوية صحيح مكون من 9 أرقام.")]
        public string NationalIdNumber { get; set; }

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "الجنس مطلوب")]
        [Display(Name = "الجنس")]
        public int GenderId { get; set; }
        public SelectList Genders { get; set; }

        [Required(ErrorMessage = "رقم الجوال مطلوب")]
        [Display(Name = "رقم الجوال")]
        public string MobileNumber { get; set; }

        [Display(Name = "رقم الواتساب (اختياري)")]
        public string WhatsAppNumber { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [Display(Name = "البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "الرجاء إدخال بريد إلكتروني صحيح.")]
        public string Email { get; set; }
        // === بداية الإضافة ===
        [Display(Name = "معرف دردشة تليجرام (Telegram Chat ID)")]
        public long? TelegramChatId { get; set; }
        // === نهاية الإضافة ===

        // --- المؤهلات ---
        [Required(ErrorMessage = "سنة التخرج (ثانوية) مطلوبة")]
        [Display(Name = "سنة التخرج")]
        public int HighSchoolYear { get; set; }

        [Required(ErrorMessage = "معدل الثانوية مطلوب")]
        [Display(Name = "المعدل (%)")]
        public double HighSchoolPercentage { get; set; }

        [Required(ErrorMessage = "اسم الجامعة مطلوب")]
        [Display(Name = "اسم الجامعة")]
        public string UniversityName { get; set; }

        [Required(ErrorMessage = "سنة التخرج (جامعة) مطلوبة")]
        [Display(Name = "سنة التخرج")]
        public int BachelorYear { get; set; }

        [Required(ErrorMessage = "معدل الجامعة مطلوب")]
        [Display(Name = "المعدل (%)")]
        public double BachelorPercentage { get; set; }





        // المرفقات
        [Required(ErrorMessage = "الرجاء إرفاق شهادة الثانوية العامة.")]
        [Display(Name = "شهادة الثانوية العامة")]
        public HttpPostedFileBase HighSchoolCertificateFile { get; set; }

        [Required(ErrorMessage = "الرجاء إرفاق الشهادة الجامعية.")]
        [Display(Name = "الشهادة الجامعية")]
        public HttpPostedFileBase BachelorCertificateFile { get; set; }

        [Required(ErrorMessage = "الرجاء إرفاق صورة الهوية.")]
        [Display(Name = "صورة الهوية الشخصية")]
        public HttpPostedFileBase PersonalIdFile { get; set; }



    }
}
