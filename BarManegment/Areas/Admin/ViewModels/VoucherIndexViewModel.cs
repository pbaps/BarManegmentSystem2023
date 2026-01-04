using BarManegment.Models;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.ViewModels
{
    // (هذا الكلاس سيبقى في نفس الملف)
    // كلاس مساعد لعرض قسائم المتعهدين مع أسمائهم
    public class ContractorVoucherDisplay
    {
        public PaymentVoucher Voucher { get; set; }
        public string ContractorName { get; set; }
    }

    public class VoucherIndexViewModel
    {
        public List<PaymentVoucher> UnpaidTraineeVouchers { get; set; }

        // --- ⬇️  بداية التعديل ⬇️ ---
        // (سنستخدم الكلاس المساعد الجديد بدلاً من القائمة القديمة)
        public List<ContractorVoucherDisplay> UnpaidContractorVouchers { get; set; }
        // --- ⬆️ نهاية التعديل ⬆️ ---
        public List<PaymentVoucher> UnpaidGeneralVouchers { get; set; }
        public List<PaymentVoucher> PaidVouchers { get; set; }

        public VoucherIndexViewModel()
        {
            UnpaidTraineeVouchers = new List<PaymentVoucher>();
            UnpaidContractorVouchers = new List<ContractorVoucherDisplay>(); // <-- تعديل
            PaidVouchers = new List<PaymentVoucher>();
        }
    }
}