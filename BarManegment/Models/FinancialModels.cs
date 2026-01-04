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

        [Display(Name = "رصيد افتتاحي")]
        public decimal OpeningBalance { get; set; } = 0;

        public bool IsTransactional { get; set; } = true;
    }

    public enum AccountType
    {
        Asset, Liability, Equity, Revenue, Expense
    }

    // 2. رأس القيد المحاسبي (Journal Entry Header)
    public class JournalEntry
    {
        public int Id { get; set; }
        // داخل class JournalEntry
        public int? CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual UserModel CreatedByUser { get; set; }

        // --- الحقول المشتركة (الأساسية) ---
        [Display(Name = "تاريخ القيد")]
        public DateTime EntryDate { get; set; }

        [Display(Name = "البيان / الشرح")]
        public string Description { get; set; }

        [Display(Name = "رقم مرجعي")]
        public string ReferenceNumber { get; set; }

        public bool IsPosted { get; set; } = false;

        // --- حقول الرواتب (الجديدة) ---
        [Display(Name = "أنشأ بواسطة")]
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // --- حقول النظام القديم (لحل الأخطاء في باقي المتحكمات) ---
        [Display(Name = "السنة المالية")]
        public int? FiscalYearId { get; set; } // جعلناه اختياري لعدم كسر الرواتب

        // (ملاحظة: إذا كان FiscalYear كلاس موجود، أضف العلاقة، وإلا اتركها معطلة مؤقتاً)
        // [ForeignKey("FiscalYearId")]
        // public virtual FiscalYear FiscalYear { get; set; }

        [Display(Name = "رقم القيد")]
        public string EntryNumber { get; set; } // كان int في بعض الأنظمة، String أأمن

        [Display(Name = "المصدر")]
        public string SourceModule { get; set; } // مثال: Payroll, Inventory

        public int? SourceId { get; set; } // ID المستند المصدر

        public DateTime? PostedDate { get; set; }
        public int? PostedByUserId { get; set; }

        public decimal? ExchangeRate { get; set; } = 1;
        public int? CurrencyId { get; set; }

        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }

        // --- العلاقات ---
        // سنستخدم اسمين لنفس القائمة لإرضاء جميع المتحكمات!
        // 1. الاسم الجديد (للرواتب)
        public virtual ICollection<JournalEntryDetail> JournalEntryDetails { get; set; }

        // 2. الاسم القديم (لباقي النظام) - يعيد نفس البيانات
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