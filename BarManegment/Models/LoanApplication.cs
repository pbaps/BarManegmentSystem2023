using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("LoanApplications")]
    public class LoanApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المحامي (صاحب الطلب)")]
        public int LawyerId { get; set; }
        [ForeignKey("LawyerId")]
        public virtual GraduateApplication Lawyer { get; set; }

        [Required]
        [Display(Name = "نوع القرض")]
        public int LoanTypeId { get; set; }
        [ForeignKey("LoanTypeId")]
        public virtual LoanType LoanType { get; set; }

        [Required]
        [Display(Name = "المبلغ الإجمالي للقرض")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "عدد الأقساط")]
        public int InstallmentCount { get; set; }

        [Required]
        [Display(Name = "قيمة القسط الشهري")]
        public decimal InstallmentAmount { get; set; }

        [Required]
        [Display(Name = "تاريخ بدء أول قسط")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "تاريخ تقديم الطلب")]
        [DataType(DataType.Date)]
        public DateTime ApplicationDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        [Display(Name = "حالة الطلب")]
        public string Status { get; set; } // (تحت المراجعة، موافق، مفعل، مكتمل...)

        [Display(Name = "هل تم صرف المبلغ؟")]
        public bool IsDisbursed { get; set; } = false;

        [Display(Name = "تاريخ الصرف")]
        public DateTime? DisbursementDate { get; set; }

        // --- المرفقات المطلوبة ---

        [Display(Name = "مسار نموذج طلب القرض")]
        [StringLength(500)]
        public string ApplicationFormPath { get; set; }

        [Display(Name = "مسار قرار المجلس (الموافقة)")]
        [StringLength(500)]
        public string CouncilApprovalScannedPath { get; set; }

        [Display(Name = "مسار الكمبيالة الكلية")]
        [StringLength(500)]
        public string MainPromissoryNoteScannedPath { get; set; }

        [Display(Name = "مسار سند المديونية")]
        [StringLength(500)]
        public string DebtBondScannedPath { get; set; }

        // 💡💡 === بداية الإضافة === 💡💡
        [Display(Name = "سبب القرض")]
        [StringLength(1000)]
        public string Notes { get; set; } // (هذا هو الحقل المفقود)
                                          // 💡💡 === نهاية الإضافة === 💡💡
                                          // ✅✅✅ الإضافات المطلوبة لحل الخطأ ✅✅✅

 

        [Display(Name = "مسار قرار المجلس")]
        public string CouncilApprovalPath { get; set; }

        [Display(Name = "مسار الكمبيالة")]
        public string MainPromissoryNotePath { get; set; }

        [Display(Name = "مسار سند الدين")]
        public string DebtBondPath { get; set; }

        // --- Navigation Properties ---
        public virtual ICollection<Guarantor> Guarantors { get; set; }
        public virtual ICollection<LoanInstallment> Installments { get; set; }


        // ✅✅✅ أضف هذا السطر لحل الخطأ ✅✅✅
        public bool IsPaid { get; set; } = false;

 




        public LoanApplication()
        {
            Guarantors = new HashSet<Guarantor>();
            Installments = new HashSet<LoanInstallment>();
        }
    }
}