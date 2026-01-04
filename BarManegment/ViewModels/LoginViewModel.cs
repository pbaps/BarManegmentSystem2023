using System.ComponentModel.DataAnnotations;

namespace BarManegment.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "اسم المستخدم مطلوب.")]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; }

        [Display(Name = "تذكرني؟")]
        public bool RememberMe { get; set; }
        public string ReturnUrl { get; internal set; }
    }


   
}
