using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Members.ViewModels
{
    public class MemberLoginViewModel
    {
        [Required(ErrorMessage = "الرقم الوطني مطلوب")]
        [Display(Name = "الرقم الوطني (اسم المستخدم)")]
        public string NationalIdNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [Display(Name = "كلمة المرور")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "تذكرني؟")]
        public bool RememberMe { get; set; }
        public string ReturnUrl { get; internal set; }
    }
}
