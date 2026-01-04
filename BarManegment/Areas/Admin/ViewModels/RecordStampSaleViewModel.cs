using BarManegment.Models; // <-- (هام) تأكد من إضافة هذا
using System.Collections.Generic; // <-- (هام) تأكد من إضافة هذا
using System.ComponentModel.DataAnnotations;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class RecordStampSaleViewModel
    {
        [Required(ErrorMessage = "الرجاء اختيار المتعهد")]
        public int ContractorId { get; set; }

        [Required(ErrorMessage = "الرجاء إدخال رقم عضوية المحامي أو اسمه")]
        [Display(Name = "رقم العضوية أو اسم المحامي")]
        public string LawyerSearchKey { get; set; }

        public int? LawyerId { get; set; }

        [Display(Name = "اسم المحامي (كما في الكشف)")]
        [Required(ErrorMessage = "اسم المحامي مطلوب")]
        public string LawyerName { get; set; }

        [Display(Name = "رقم الآيبان (IBAN)")]
        public string LawyerIban { get; set; }

        [Display(Name = "اسم البنك")]
        public string LawyerBankName { get; set; }

        // --- ⬇️ ⬇️ (إضافة حقول البنك الجديدة) ⬇️ ⬇️ ---
        [Display(Name = "فرع البنك")]
        public string LawyerBankBranch { get; set; }

        [Display(Name = "رقم الحساب")]
        public string LawyerAccountNumber { get; set; }
        // --- ⬆️ ⬆️ (نهاية الإضافة) ⬆️ ⬆️ ---

        [Required(ErrorMessage = "الرجاء إدخال الرقم التسلسلي للبداية")]
        [Display(Name = "من الرقم التسلسلي")]
        public string StartSerial { get; set; }

        [Display(Name = "إلى الرقم التسلسلي ( اتركه فارغاً لبيع طابع واحد)")]
        public string EndSerial { get; set; }

        // --- ⬇️ ⬇️ (الإضافة الجديدة) ⬇️ ⬇️ ---
        // (قائمة بالطوابع المتاحة للمتعهد الحالي)
        public List<Stamp> AvailableStamps { get; set; }

        public RecordStampSaleViewModel()
        {
            AvailableStamps = new List<Stamp>();
        }
        // --- ⬆️ ⬆️ (نهاية الإضافة) ⬆️ ⬆️ ---
    }
}