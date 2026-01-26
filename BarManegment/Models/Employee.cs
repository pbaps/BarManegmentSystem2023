using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web;

namespace BarManegment.Models
{
    public class Employee
    {
        public int Id { get; set; }

        // ============================
        // 1. البيانات الشخصية
        // ============================
        [Required, Display(Name = "الاسم الرباعي")]
        public string FullName { get; set; }

        [Display(Name = "رقم الهوية")]
        public string NationalId { get; set; }

        [Display(Name = "رقم الهاتف")]
        public string Phone { get; set; }

        [Display(Name = "الصورة الشخصية")]
        public string ProfilePicturePath { get; set; }

        [NotMapped]
        [Display(Name = "رفع صورة")]
        public HttpPostedFileBase ImageFile { get; set; }

        [Display(Name = "تاريخ التعيين")]
        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; } // جعلته Nullable لتجنب مشاكل البيانات القديمة

        [Display(Name = "القسم")]
        public int DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; }

        [Display(Name = "المسمى الوظيفي")]
        public int JobTitleId { get; set; }
        [ForeignKey("JobTitleId")]
        public virtual JobTitle JobTitle { get; set; }

        // ============================
        // 2. البيانات المالية (الأساسي والعلاوات)
        // ============================
        [Display(Name = "الراتب الأساسي")]
        public decimal BasicSalary { get; set; }

        // --- إعدادات الزيادة السنوية (معدل) ---

        [Display(Name = "قيمة العلاوة السنوية المتراكمة")]
        public decimal? AnnualIncrementAmount { get; set; } = 0; // ✅ الحقل الجديد المخزن في قاعدة البيانات

        [Display(Name = "نسبة الزيادة السنوية (%)")]
        public decimal AnnualIncrementPercent { get; set; } = 5;

        [Display(Name = "الحد الأقصى لسنوات الزيادة")]
        public int MaxIncrementYears { get; set; } = 24;

        // --- العلاوات الإدارية ---
        [Display(Name = "علاوة مدير")]
        public decimal ManagerAllowance { get; set; } = 0;

        [Display(Name = "علاوة رئيس قسم")]
        public decimal HeadOfDeptAllowance { get; set; } = 0;

        // --- علاوات المؤهل والتخصص (الخاضعة للتأمين) ---
        [Display(Name = "علاوة ماجستير")]
        public decimal MasterDegreeAllowance { get; set; } = 0;

        [Display(Name = "علاوة دكتوراه")]
        public decimal PhdDegreeAllowance { get; set; } = 0;

        [Display(Name = "علاوة تخصص/مهنة")]
        public decimal SpecializationAllowance { get; set; } = 0;

        [Display(Name = "بدل مواصلات")]
        public decimal TransportAllowance { get; set; } = 0;

        // ============================
        // 3. التأمين والمعاشات
        // ============================
        [Display(Name = "نسبة استقطاع الموظف (تأمين) %")]
        public decimal EmployeePensionPercent { get; set; } = 7;

        [Display(Name = "نسبة مساهمة النقابة (تأمين) %")]
        public decimal EmployerPensionPercent { get; set; } = 9;

        [Display(Name = "خصومات شهرية ثابتة أخرى")]
        public decimal OtherMonthlyDeduction { get; set; } = 0;

        // ============================
        // 4. الحسابات التلقائية (Logic)
        // ============================

        // ب) حساب "الوعاء التأميني" (الراتب الخاضع للتأمين)
        public decimal PensionBasisSalary
        {
            get
            {
                return BasicSalary +
                       MasterDegreeAllowance +
                       PhdDegreeAllowance +
                       SpecializationAllowance;
            }
        }

        // ج) حساب قيم الاستقطاع
        public decimal PensionAmountEmployee => PensionBasisSalary * (EmployeePensionPercent / 100);
        public decimal PensionAmountEmployer => PensionBasisSalary * (EmployerPensionPercent / 100);
        public decimal TotalToPensionAuthority => PensionAmountEmployee + PensionAmountEmployer;

        // د) إجمالي الراتب (Gross Salary) - يشمل كل شيء قبل الخصم
        public decimal TotalSalary =>
            BasicSalary +
            (AnnualIncrementAmount ?? 0) + // ✅ استخدام القيمة المخزنة
            ManagerAllowance +
            HeadOfDeptAllowance +
            MasterDegreeAllowance +
            PhdDegreeAllowance +
            SpecializationAllowance +
            TransportAllowance;

        // هـ) صافي الراتب (للعرض فقط)
        public decimal NetSalary => TotalSalary - PensionAmountEmployee - OtherMonthlyDeduction;

        // ============================
        // 5. البيانات البنكية والنظام
        // ============================
        [Display(Name = "اسم البنك")]
        public string BankName { get; set; }
        [Display(Name = "فرع البنك")]
        public string BankBranch { get; set; }
        [Display(Name = "رقم الحساب البنكي")]
        public string BankAccountNumber { get; set; }
        [Display(Name = "رقم الآيبان (IBAN)")]
        public string IBAN { get; set; }

        [Display(Name = "مستخدم النظام المرتبط")]
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual UserModel User { get; set; }

        [Display(Name = "الحالة")]
        public bool IsActive { get; set; } = true;
    }
}