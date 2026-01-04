// File Path: BarManegment/ViewModels/ProfileViewModel.cs
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace BarManegment.ViewModels
{
    public class ProfileViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "حقل الاسم الكامل مطلوب.")]
        [Display(Name = "الاسم الكامل")]
        public string FullNameArabic { get; set; }

        [Required(ErrorMessage = "حقل رقم الهوية مطلوب.")]
        [Display(Name = "رقم الهوية")]
        public string IdentificationNumber { get; set; }
        [Required(ErrorMessage = "حقل البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "الرجاء إدخال بريد إلكتروني صحيح.")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; }

        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; } // للقراءة فقط

        public string ProfilePicturePath { get; set; }

        [Display(Name = "تغيير الصورة الشخصية")]
        public HttpPostedFileBase ProfilePictureFile { get; set; }

        // --- قسم تغيير كلمة المرور ---

        [Display(Name = "كلمة المرور الحالية")]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        [Display(Name = "كلمة المرور الجديدة")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "يجب أن تكون كلمة المرور الجديدة على الأقل {2} أحرف.", MinimumLength = 6)]
        public string NewPassword { get; set; }

        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "كلمة المرور الجديدة وتأكيدها غير متطابقين.")]
        public string ConfirmNewPassword { get; set; }
    }
}