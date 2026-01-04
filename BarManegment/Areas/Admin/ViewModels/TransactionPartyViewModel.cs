using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class TransactionPartyViewModel
    {
        [Required]
        [Display(Name = "نوع الطرف (1=أول, 2=ثاني)")]
        public int PartyType { get; set; }

        [Required(ErrorMessage = "اسم الطرف مطلوب")]
        [Display(Name = "اسم الطرف")]
        [StringLength(200)]
        public string PartyName { get; set; }

        [Required(ErrorMessage = "رقم الهوية مطلوب")]
        [Display(Name = "رقم الهوية")]
        [StringLength(50)]
        public string PartyIDNumber { get; set; }

        [Required(ErrorMessage = "المحافظة مطلوبة")]
        [Display(Name = "المحافظة")]
        public int ProvinceId { get; set; }

        [Required(ErrorMessage = "صفة الطرف مطلوبة")]
        [Display(Name = "صفة الطرف (بائع، مشتري...)")]
        public int PartyRoleId { get; set; }
    }
}