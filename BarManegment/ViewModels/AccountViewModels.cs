// File Path: BarManegment/ViewModels/AccountViewModels.cs
using System.ComponentModel.DataAnnotations;

namespace BarManegment.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "حقل البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "الرجاء إدخال بريد إلكتروني صحيح.")]
        [Display(Name = "البريد الإلكتروني المسجل في النظام")]
        public string Email { get; set; }
    }

    public class ResetPasswordViewModel
    {
      //  [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "حقل كلمة المرور الجديدة مطلوب.")]
        [StringLength(100, ErrorMessage = "يجب أن تكون كلمة المرور على الأقل {2} أحرف.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الجديدة")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        [Compare("Password", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين.")]
        public string ConfirmPassword { get; set; }
    }
}