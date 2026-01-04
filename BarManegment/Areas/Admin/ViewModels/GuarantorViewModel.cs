using System.ComponentModel.DataAnnotations;
using System.Web;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class GuarantorViewModel
    {
        [Required(ErrorMessage = "الرجاء تحديد نوع الكفيل")]
        [Display(Name = "نوع الكفيل")]
        public string GuarantorType { get; set; } // "Lawyer" or "External"

        // --- (في حالة الكفيل محامي) ---
        [Display(Name = "معرف المحامي (الرقم الوطني/العضوية)")]
        public string LawyerIdentifier { get; set; }

        [Display(Name = "تجاوز التحقق (قرار إداري)")]
        public bool IsOverride { get; set; } = false;

        // --- (في حالة الكفيل موظف خارجي) ---
        [Display(Name = "اسم الكفيل الخارجي")]
        public string ExternalName { get; set; }

        [Display(Name = "رقم الهوية")]
        public string ExternalIdNumber { get; set; }

        [Display(Name = "الوظيفة")]
        public string JobTitle { get; set; }

        [Display(Name = "مكان العمل")]
        public string Workplace { get; set; }

        [Display(Name = "الرقم الوظيفي")]
        public string WorkplaceEmployeeId { get; set; }

        [Display(Name = "الراتب الصافي")]
        public decimal? NetSalary { get; set; }

        [Display(Name = "البنك")]
        public string BankName { get; set; }

        [Display(Name = "رقم الحساب (IBAN)")]
        public string BankAccountNumber { get; set; }

        // --- (ملف الكفالة) ---
        [Display(Name = "نموذج الكفالة الموقع")]
        public HttpPostedFileBase GuarantorFormFile { get; set; }
    }
}