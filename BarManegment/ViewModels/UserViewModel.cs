// File Path: BarManegment/ViewModels/UserViewModel.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب.")]
        [Display(Name = "الاسم الكامل")]
        public string FullNameArabic { get; set; }

        [Required(ErrorMessage = "اسم المستخدم مطلوب.")]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; }

        [Required(ErrorMessage = "رقم الهوية مطلوب.")]
        [Display(Name = "رقم الهوية")]
        public string IdentificationNumber { get; set; }
        [Display(Name = "البريد الإلكتروني")]
        [EmailAddress(ErrorMessage = "الرجاء إدخال بريد إلكتروني صحيح.")]
        public string Email { get; set; }

        [Display(Name = "مسار الصورة الشخصية")]
        public string ProfilePicturePath { get; set; }

        [Display(Name = "اختر صورة شخصية")]
        public HttpPostedFileBase ProfilePictureFile { get; set; }

        [StringLength(100, ErrorMessage = "يجب أن تكون كلمة المرور 6 أحرف على الأقل.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; }

        [NotMapped]
        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين.")]
        public string ConfirmPassword { get; set; }


        [Required(ErrorMessage = "الرجاء تحديد نوع المستخدم")]
        [Display(Name = "نوع المستخدم (الدور)")]
        public int UserTypeId { get; set; }

        [Display(Name = "حالة الحساب")]
        public bool IsActive { get; set; } = true;

        public SelectList UserTypes { get; set; }
    }
}

