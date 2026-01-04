using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Members.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "الرقم الوطني مطلوب")]
        [Display(Name = "الرقم الوطني")]
        public string NationalIdNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [Display(Name = "كلمة المرور")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "كلمة المرور وتأكيدها غير متطابقين.")]
        public string ConfirmPassword { get; set; }
    }
}
