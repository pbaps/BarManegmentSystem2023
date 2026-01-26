using BarManegment.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.ViewModels
{
    public class GraduateApplicationViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم باللغة العربية مطلوب")]
        [Display(Name = "الاسم باللغة العربية")]
        public string ArabicName { get; set; }

        [Display(Name = "الاسم باللغة الإنجليزية")]
        public string EnglishName { get; set; }

        [Required(ErrorMessage = "الرقم الوطني مطلوب")]
        [Display(Name = "الرقم الوطني")]
        public string NationalIdNumber { get; set; }

        [Required(ErrorMessage = "نوع الهوية مطلوب")]
        [Display(Name = "نوع الهوية")]
        public int NationalIdTypeId { get; set; }

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        [Display(Name = "تاريخ الميلاد")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public System.DateTime BirthDate { get; set; }

        [Display(Name = "مكان الميلاد")]
        public string BirthPlace { get; set; }

        [Display(Name = "الجنسية")]
        public string Nationality { get; set; }

        [Required(ErrorMessage = "الجنس مطلوب")]
        [Display(Name = "الجنس")]
        public int GenderId { get; set; }

        [Required(ErrorMessage = "حالة الطلب مطلوبة")]
        [Display(Name = "حالة الطلب")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار حالة صحيحة")]
        public int ApplicationStatusId { get; set; }

        public string PersonalPhotoPath { get; set; }

        [Display(Name = "تاريخ تقديم الطلب")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}")]
        public System.DateTime SubmissionDate { get; set; }

        public ContactInfo ContactInfo { get; set; }

        // القوائم المنسدلة
        public SelectList Genders { get; set; }
        public SelectList NationalIdTypes { get; set; }
        public SelectList ApplicationStatuses { get; set; }
        public SelectList Countries { get; set; }
        public SelectList Governorates { get; set; }

        // لإدارة المؤهلات والمرفقات
        public List<Qualification> Qualifications { get; set; }
        public Qualification NewQualification { get; set; }
        public SelectList QualificationTypes { get; set; }
        public List<Attachment> Attachments { get; set; }
        public SelectList AttachmentTypes { get; set; }

        [Display(Name = "نوع المرفق")]
        public int? NewAttachmentTypeId { get; set; }

        [Display(Name = "اختر الملف")]
        public HttpPostedFileBase NewAttachmentFile { get; set; }

        [Display(Name = "الصورة الشخصية")]
        public HttpPostedFileBase PersonalPhotoFile { get; set; }
        // === بداية الإضافة: خصائص المحامي المشرف ===
        [Display(Name = "المحامي المشرف")]
        public int? SupervisorId { get; set; }
        public string SupervisorName { get; set; }

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
        // === نهاية الإضافة ===
        public GraduateApplicationViewModel()
        {
            ContactInfo = new ContactInfo();
            Qualifications = new List<Qualification>();
            NewQualification = new Qualification();
            Attachments = new List<Attachment>();
        }
    }
}
