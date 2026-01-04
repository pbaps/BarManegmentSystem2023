using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("LoanTypes")]
    public class LoanType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "اسم نوع القرض")]
        public string Name { get; set; } // (مثل: قرض حسن، قرض طوارئ)

        [Required]
        [Display(Name = "حساب البنك (لتسديد الأقساط)")]
        public int BankAccountForRepaymentId { get; set; }
        [ForeignKey("BankAccountForRepaymentId")]
        public virtual BankAccount BankAccount { get; set; }

        [Display(Name = "الحد الأعلى لمبلغ القرض")]
        public decimal MaxAmount { get; set; }

        [Display(Name = "الحد الأعلى لعدد الأقساط")]
        public int MaxInstallments { get; set; }
    }
}