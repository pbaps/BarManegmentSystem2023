using BarManegment.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc; // 💡 إضافة ضرورية لـ SelectList

namespace BarManegment.Areas.Admin.ViewModels
{
    public class StampDashboardViewModel
    {
        // 1. للتبويب الأول (المخزون)
        public IEnumerable<StampContractor> Contractors { get; set; }
        public IEnumerable<StampBook> AvailableBooks { get; set; }
        public IEnumerable<StampBook> IssuedBooks { get; set; }

        // 2. للتبويب الثاني (صرف الدفاتر)
        public SelectList AvailableBooksList { get; set; }
        public SelectList ContractorsList { get; set; }
        public IEnumerable<StampBookIssuance> RecentIssuances { get; set; }


        // --- ⬇️ الإضافة الجديدة هنا ⬇️ ---
        // قائمة بالقسائم التي دفعها المتعهدون في البنك، ولم يتم صرف دفاتر مقابلها
        public IEnumerable<PaymentVoucher> PaidVouchersAwaitingIssuance { get; set; }
        // 3. لنموذج الإدخال
        [Display(Name = "اختر المتعهد")]
        public int SelectedContractorId { get; set; }
        [Display(Name = "اختر الدفتر")]
        public int SelectedBookId { get; set; }

        // (هام) كما طلبت، سنربط بإيصال القبض
        // نحن نفترض أنك ستدخل "رقم" الإيصال
        [Display(Name = "رقم  القسيمة  ")]
        public string VoucherId { get; set; } // تم تغيير الاسم
    }
}