using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    // 1. دليل الحسابات
    public class Account
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "كود الحساب")]
        public string Code { get; set; }

        [Required]
        [Display(Name = "اسم الحساب")]
        public string Name { get; set; }

        public int Level { get; set; }
        public AccountType AccountType { get; set; }
 


        public int? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public virtual Account ParentAccount { get; set; }

        [Display(Name = "رصيد افتتاحي")]
        public decimal OpeningBalance { get; set; } = 0;

        public bool IsTransactional { get; set; } = true;

        public virtual ICollection<Account> ChildAccounts { get; set; }
    }

    public enum AccountType
    {
        Asset, Liability, Equity, Revenue, Expense
    }

    // 2. رأس القيد المحاسبي (Journal Entry Header)
    public class JournalEntry
    {
        [Key]
        public int Id { get; set; }

        // --- 1. علاقة السنة المالية (تم إصلاحها) ---
        // جعلناها int? لتقبل القيم الفارغة للبيانات القديمة، لكن العلاقة مفعلة
        [Display(Name = "السنة المالية")]
        public int? FiscalYearId { get; set; }

        [ForeignKey("FiscalYearId")]
        public virtual FiscalYear FiscalYear { get; set; } // ✅ هذا هو السطر الذي كان ينقصك

        // --- 2. البيانات الأساسية ---
        [Display(Name = "رقم القيد")]
        public string EntryNumber { get; set; }

        [Display(Name = "تاريخ القيد")]
        [DataType(DataType.Date)]
        public DateTime EntryDate { get; set; }

        [Display(Name = "البيان / الشرح")]
        public string Description { get; set; }

        [Display(Name = "رقم مرجعي")]
        public string ReferenceNumber { get; set; }

        [Display(Name = "المصدر")]
        public string SourceModule { get; set; } // مثال: Manual, Receipts

        public int? SourceId { get; set; } // ID المستند المصدر

        // --- 3. بيانات الترحيل والمستخدمين ---
        public bool IsPosted { get; set; } = false;
        public DateTime? PostedDate { get; set; }
        public int? PostedByUserId { get; set; }

        [Display(Name = "أنشأ بواسطة")]
        public string CreatedBy { get; set; }

        public int? CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public virtual UserModel CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- 4. العملات والأرقام ---
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }

        public int? CurrencyId { get; set; }
        public decimal? ExchangeRate { get; set; } = 1;

        // --- 5. التفاصيل (العلاقة مع الأسطر) ---
        public virtual ICollection<JournalEntryDetail> JournalEntryDetails { get; set; }

        // خاصية مساعدة للتوافق مع الكود القديم (Alias)
        [NotMapped]
        public virtual ICollection<JournalEntryDetail> Lines
        {
            get { return JournalEntryDetails; }
            set { JournalEntryDetails = value; }
        }

        public JournalEntry()
        {
            JournalEntryDetails = new HashSet<JournalEntryDetail>();
        }
    }

    // 3. تفاصيل القيد (الأسطر)
    // قمنا بتغيير الاسم لـ JournalEntryDetail ولكن سنضيف "Alias" للكود القديم
    public class JournalEntryDetail
    {
        public int Id { get; set; }

        public int JournalEntryId { get; set; }
        [ForeignKey("JournalEntryId")]
        public virtual JournalEntry JournalEntry { get; set; }

        public int AccountId { get; set; }
        [ForeignKey("AccountId")]
        public virtual Account Account { get; set; }

        public decimal Debit { get; set; } = 0;
        public decimal Credit { get; set; } = 0;

        public string Description { get; set; }

        // حقول إضافية قد تكون مستخدمة في النظام القديم
        public int? CostCenterId { get; set; }


        // 👇👇 أضف هذه الحقول الناقصة لحل أخطاء التقارير 👇👇
     
        [ForeignKey("CostCenterId")]
        public virtual CostCenter CostCenter { get; set; }

        public int? CurrencyId { get; set; }
        [ForeignKey("CurrencyId")]
        public virtual Currency Currency { get; set; }

        public decimal? ExchangeRate { get; set; } // نحتاجه أحياناً في السطر

        // حقول المبالغ بالعملة الأجنبية (لأن التقارير القديمة قد تطلبها)
        public decimal ForeignDebit { get; set; } = 0;
        public decimal ForeignCredit { get; set; } = 0;
    }
}