using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Members.ViewModels
{
    // (مودل فرعي لرفع المرفقات)
    public class AttachmentUploadViewModel
    {
        [Required(ErrorMessage = "الرجاء اختيار نوع المرفق.")]
        [Display(Name = "نوع المرفق")]
        public int AttachmentTypeId { get; set; }

        [Required(ErrorMessage = "الرجاء اختيار ملف.")]
        [Display(Name = "الملف")]
        public HttpPostedFileBase File { get; set; }
    }

    // (مودل فرعي لرفع المؤهلات)
    public class QualificationUploadViewModel
    {
        [Required(ErrorMessage = "الرجاء اختيار نوع المؤهل.")]
        [Display(Name = "نوع المؤهل")]
        public int QualificationTypeId { get; set; }

        [Required(ErrorMessage = "اسم الجامعة مطلوب.")]
        [Display(Name = "اسم الجامعة / المدرسة")]
        public string UniversityName { get; set; }

        [Display(Name = "الكلية")]
        public string Faculty { get; set; }

        [Display(Name = "التخصص")]
        public string Specialization { get; set; }

        [Display(Name = "سنة التخرج")]
        public int GraduationYear { get; set; }

        [Display(Name = "المعدل (%)")]
        public double? GradePercentage { get; set; }
    }


    public class MemberProfileViewModel
    {
        public int Id { get; set; }

        [Display(Name = "الاسم باللغة العربية")]
        public string ArabicName { get; set; }

        [Display(Name = "الاسم باللغة الإنجليزية")]
        [Required(ErrorMessage = "الاسم باللغة الإنجليزية مطلوب")]
        public string EnglishName { get; set; }

        [Display(Name = "الرقم الوطني")]
        public string NationalIdNumber { get; set; }

        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "مكان الميلاد مطلوب")]
        [Display(Name = "مكان الميلاد")]
        public string BirthPlace { get; set; }

        [Required(ErrorMessage = "الجنسية مطلوبة")]
        [Display(Name = "الجنسية")]
        public string Nationality { get; set; }

        [Required]
        public ContactInfo ContactInfo { get; set; }

        public List<Qualification> Qualifications { get; set; }
        public List<Attachment> Attachments { get; set; }

        // --- نماذج الإضافة (للمودالات) ---
        public QualificationUploadViewModel NewQualification { get; set; }
        public AttachmentUploadViewModel NewAttachment { get; set; }

        // --- قوائم منسدلة ---
        public SelectList Nationalities { get; set; }
        public SelectList Governorates { get; set; }
        public SelectList QualificationTypes { get; set; }
        public SelectList AttachmentTypes { get; set; }

        [Display(Name = "معرف دردشة تليجرام")]
        public long? TelegramChatId { get; set; }

        // === 
        // === بداية الإضافة
        // ===
        public string TelegramBotName { get; set; }
        // === نهاية الإضافة ===

        [Display(Name = "الصورة الشخصية")]
        public HttpPostedFileBase PersonalPhotoFile { get; set; }
        public string CurrentPersonalPhotoPath { get; set; }

        [Display(Name = "الحالة الحالية للملف")]
        public string CurrentStatusName { get; set; }

        // --- بيانات المشرف ---
        [Display(Name = "المحامي المشرف")]
        public int? SupervisorId { get; set; }

        [Display(Name = "الرقم الوطني للمشرف")]
        public string SupervisorNationalId { get; set; }

        [Display(Name = "اسم المشرف (بعد التحقق)")]
        public string SupervisorNameDisplay { get; set; }

        // --- بيانات تسجيل الدخول ---
        [Display(Name = "البريد الإلكتروني (بيانات الدخول)")]
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "صيغة بريد إلكتروني غير صحيحة")]
        public string Email { get; set; }

        [Display(Name = "كلمة المرور القديمة")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [Display(Name = "كلمة المرور الجديدة")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "يجب أن تكون كلمة المرور {0} على الأقل {2} أحرف.", MinimumLength = 6)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "كلمة المرور الجديدة وتأكيدها غير متطابقين.")]
        public string ConfirmNewPassword { get; set; }

        // --- ⬇️ ⬇️ بداية الإضافة: بيانات البنك ⬇️ ⬇️ ---
        [Display(Name = "اسم البنك")]
        [StringLength(100)]
        public string BankName { get; set; }

        [Display(Name = "فرع البنك")]
        [StringLength(100)]
        public string BankBranch { get; set; }

        [Display(Name = "رقم الحساب")]
        [StringLength(50)]
        public string AccountNumber { get; set; }

        [Display(Name = "رقم الآيبان (IBAN)")]
        [StringLength(34)]
        public string Iban { get; set; }
        // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---
        public MemberProfileViewModel()
        {
            Qualifications = new List<Qualification>();
            Attachments = new List<Attachment>();
            ContactInfo = new ContactInfo();
            NewQualification = new QualificationUploadViewModel();
            NewAttachment = new AttachmentUploadViewModel();
        }
    }
}