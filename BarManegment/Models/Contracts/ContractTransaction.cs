using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("ContractTransactions")]
    public class ContractTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "تاريخ المعاملة")]
        [DataType(DataType.Date)]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "المحامي")]
        public int LawyerId { get; set; }
        [ForeignKey("LawyerId")]
        public virtual GraduateApplication Lawyer { get; set; }

        [Required]
        [Display(Name = "نوع العقد")]
        public int ContractTypeId { get; set; }
        [ForeignKey("ContractTypeId")]
        public virtual ContractType ContractType { get; set; }

        [Required]
        [Display(Name = "الرسوم النهائية")]
        public decimal FinalFee { get; set; }

        // للقيمة المصرح بها (للعقود النسبية)
        [Display(Name = "قيمة العقد المصرح بها")]
        public decimal DeclaredValue { get; set; }

        [Required]
        [Display(Name = "معفى من الرسوم")]
        [DefaultValue(false)]
        public bool IsExempt { get; set; } = false;

        [Display(Name = "سبب الإعفاء")]
        public int? ExemptionReasonId { get; set; }
        [ForeignKey("ExemptionReasonId")]
        public virtual ContractExemptionReason ExemptionReason { get; set; }

        [Display(Name = "ملاحظات")]
        [DataType(DataType.MultilineText)]
        public string Notes { get; set; }

        [Display(Name = "تاريخ التصديق الفعلي")]
        [DataType(DataType.Date)]
        public DateTime? CertificationDate { get; set; }

        [Required]
        [Display(Name = "الموظف المدخل")]
        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual UserModel Employee { get; set; }

        [Display(Name = "مسار العقد المصدق")]
        [StringLength(500)]
        public string ScannedContractPath { get; set; }

        [Required]
        [Display(Name = "حالة المعاملة")]
        [StringLength(100)]
        public string Status { get; set; }

        [Display(Name = "قسيمة الدفع")]
        public int? PaymentVoucherId { get; set; }
        [ForeignKey("PaymentVoucherId")]
        public virtual PaymentVoucher PaymentVoucher { get; set; }

        // إضافات الوكالة الخاصة
        [Display(Name = "الموكل يوقع عن نفسه أيضاً")]
        [DefaultValue(true)]
        public bool IsActingForSelf { get; set; } = true;

        [Display(Name = "صفة الموكل (في حال التوقيع عن الغير)")]
        [StringLength(250)]
        public string AgentLegalCapacity { get; set; }

        // Navigation Properties
        public virtual ICollection<TransactionParty> Parties { get; set; }
        public virtual ICollection<PassportMinor> Minors { get; set; }

        public ContractTransaction()
        {
            Parties = new HashSet<TransactionParty>();
            Minors = new HashSet<PassportMinor>();
        }
    }
}