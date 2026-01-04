namespace BarManegment.Migrations
{
    using BarManegment.Helpers;
    using BarManegment.Models;
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<BarManegment.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(BarManegment.Models.ApplicationDbContext context)
        {
            // ============================================================
            // 1. Add User Types (Roles)
            // ============================================================
            context.UserTypes.AddOrUpdate(ut => ut.NameEnglish,
                new UserTypeModel { NameArabic = "مسؤول عام", NameEnglish = "Administrator" },
                new UserTypeModel { NameArabic = "موظف", NameEnglish = "Employee" },
                new UserTypeModel { NameArabic = "مصحح امتحان", NameEnglish = "Grader" },
                new UserTypeModel { NameArabic = "خريج", NameEnglish = "Graduate" },
                new UserTypeModel { NameArabic = "عضو لجنة مناقشة", NameEnglish = "CommitteeMember" },
                new UserTypeModel { NameArabic = "محامي", NameEnglish = "Advocate" }
            );
            context.SaveChanges();

            // ============================================================
            // 2. Add Currencies
            // ============================================================
            context.Currencies.AddOrUpdate(c => c.Symbol,
                new Currency { Name = "شيكل إسرائيلي جديد", Symbol = "₪" },
                new Currency { Name = "دينار أردني", Symbol = "JD" },
                new Currency { Name = "دولار أمريكي", Symbol = "$" },
                new Currency { Name = "يورو", Symbol = "€" }
            );
            context.SaveChanges();

            var jodiCurrency = context.Currencies.FirstOrDefault(c => c.Symbol == "JD");
            var jodiId = jodiCurrency != null ? jodiCurrency.Id : 2;

            // ============================================================
            // 3. Add Contract Types
            // ============================================================
            context.ContractTypes.AddOrUpdate(c => c.Name,
                new ContractType { Name = "وكالة عامة", DefaultFee = 20, CurrencyId = jodiId },
                new ContractType { Name = "وكالة خاصة", DefaultFee = 10, CurrencyId = jodiId },
                new ContractType { Name = "وكالة دورية", DefaultFee = 30, CurrencyId = jodiId },
                new ContractType { Name = "سند تعهد", DefaultFee = 10, CurrencyId = jodiId },
                new ContractType { Name = "كفالة عدلية", DefaultFee = 15, CurrencyId = jodiId },
                new ContractType { Name = "وكالة جواز سفر", DefaultFee = 10, CurrencyId = jodiId }
            );
            context.SaveChanges();

            context.SystemLookups.AddOrUpdate(l => l.Name,
                new SystemLookup { Category = "PaymentMethod", Name = "نقدي", IsActive = true },
                new SystemLookup { Category = "PaymentMethod", Name = "شيك", IsActive = true },
                new SystemLookup { Category = "PaymentMethod", Name = "حوالة بنكية", IsActive = true },
                new SystemLookup { Category = "PaymentMethod", Name = "إيداع مباشر", IsActive = true }
            );
            context.SaveChanges();

            // ============================================================
            // 4. Register Modules
            // ============================================================
            context.Modules.AddOrUpdate(m => m.ControllerName,
              // --- HR & Payroll (جديد ومحدث) ---
              new ModuleModel { NameArabic = "سجل الموظفين", ControllerName = "Employees" },
              new ModuleModel { NameArabic = "إدارة الأقسام", ControllerName = "Departments" },
              new ModuleModel { NameArabic = "المسميات الوظيفية", ControllerName = "JobTitles" },
              new ModuleModel { NameArabic = "إدارة الرواتب", ControllerName = "Payroll" }, // 👈 جديد

              // --- Admissions ---
              new ModuleModel { NameArabic = "طلبات امتحان القبول", ControllerName = "ExamApplications" },
              new ModuleModel { NameArabic = "إعدادات القبول", ControllerName = "SystemSettings" },
              new ModuleModel { NameArabic = "طلبات الخريجين (المراجعة)", ControllerName = "GraduateApplications" },
              new ModuleModel { NameArabic = "لجنة التدريب", ControllerName = "TrainingCommittee" },

              // --- Trainee Affairs ---
              new ModuleModel { NameArabic = "سجل المتدربين المقيدين", ControllerName = "RegisteredTrainees" },
              new ModuleModel { NameArabic = "ملف المتدرب (للمحامي)", ControllerName = "TraineeProfile" },
              new ModuleModel { NameArabic = "طلبات نقل/وقف التدريب", ControllerName = "SupervisorChangeRequests" },
              new ModuleModel { NameArabic = "تجديدات التدريب", ControllerName = "TraineeRenewals" },
              new ModuleModel { NameArabic = "إدارة الإيقاف الإداري", ControllerName = "TraineeSuspensions" },
              new ModuleModel { NameArabic = "استعلامات وتقارير المتدربين", ControllerName = "TraineeQuery" },

              // --- Research & Committees ---
              new ModuleModel { NameArabic = "إدارة الأبحاث القانونية", ControllerName = "LegalResearch" },
              new ModuleModel { NameArabic = "إدارة لجان المناقشة", ControllerName = "CommitteeManagement" },
              new ModuleModel { NameArabic = "إدارة لجان الاختبار الشفوي", ControllerName = "OralExamCommittee" },
              new ModuleModel { NameArabic = "بوابة لجان المناقشة والاختبارات", ControllerName = "CommitteePortal" },

              // --- Oath & Practice ---
              new ModuleModel { NameArabic = "إدارة طلبات اليمين", ControllerName = "OathRequests" },
              new ModuleModel { NameArabic = "إدارة مواعيد اليمين", ControllerName = "OathCeremony" },
              new ModuleModel { NameArabic = "تجديدات مزاولة المحامين", ControllerName = "PracticingLawyerRenewals" },

              // --- Exams & Courses ---
              new ModuleModel { NameArabic = "إدارة أنواع الامتحانات", ControllerName = "ExamTypes" },
              new ModuleModel { NameArabic = "إدارة الامتحانات", ControllerName = "Exams" },
              new ModuleModel { NameArabic = "بنك الأسئلة", ControllerName = "Questions" },
              new ModuleModel { NameArabic = "التصحيح اليدوي", ControllerName = "ManualGrading" },
              new ModuleModel { NameArabic = "تسجيل المتقدمين للامتحان", ControllerName = "ExamEnrollments" },
              new ModuleModel { NameArabic = "إدارة الدورات التدريبية", ControllerName = "TrainingCourses" },
              new ModuleModel { NameArabic = "إدارة الجلسات والحضور", ControllerName = "TrainingSessions" },
              new ModuleModel { NameArabic = "تقارير حضور الدورات", ControllerName = "TrainingReports" },

              // --- Financials ---
              new ModuleModel { NameArabic = "إدارة القسائم المالية", ControllerName = "PaymentVouchers" },
              new ModuleModel { NameArabic = "سندات القبض", ControllerName = "Receipts" },
              new ModuleModel { NameArabic = "قيود اليومية", ControllerName = "JournalEntries" },
              new ModuleModel { NameArabic = "سندات الصرف", ControllerName = "GeneralExpenses" },
              new ModuleModel { NameArabic = "كشف الحساب التفصيلي", ControllerName = "GeneralLedger" },
              new ModuleModel { NameArabic = "حافظة الشيكات", ControllerName = "CheckPortfolio" },
              new ModuleModel { NameArabic = "الإعدادات المالية (الدليل والمراكز)", ControllerName = "FinancialSetup" },
              new ModuleModel { NameArabic = "تقرير ميزان المراجعة", ControllerName = "AccountingReports" },
              new ModuleModel { NameArabic = "اسعار الصرف", ControllerName = "ExchangeRates" },
              new ModuleModel { NameArabic = "القيد الافتتاحي", ControllerName = "OpeningBalances" },
              new ModuleModel { NameArabic = "إدارة أنواع الرسوم", ControllerName = "FeeTypes" },
              new ModuleModel { NameArabic = "إدارة حسابات البنوك", ControllerName = "BankAccounts" },
              new ModuleModel { NameArabic = "إدارة العملات", ControllerName = "Currencies" },
              new ModuleModel { NameArabic = "الصندوق المالي للمحامي", ControllerName = "LawyerFinancialBox" },
              new ModuleModel { NameArabic = "إدارة البيانات المالية للمحامين", ControllerName = "LawyerFinancialData" },

              // --- Inventory & Procurement ---
              new ModuleModel { NameArabic = "إدارة الموردين", ControllerName = "Suppliers" },
              new ModuleModel { NameArabic = "إدارة الأصناف والمخزون", ControllerName = "Items" },
              new ModuleModel { NameArabic = "فواتير المشتريات (التوريد)", ControllerName = "PurchaseInvoices" },
              new ModuleModel { NameArabic = "أذونات الصرف المخزني", ControllerName = "StockIssues" },

              // --- Loans ---
              new ModuleModel { NameArabic = "إدارة أنواع القروض", ControllerName = "LoanTypes" },
              new ModuleModel { NameArabic = "إدارة طلبات القروض", ControllerName = "LoanApplications" },
              new ModuleModel { NameArabic = "إدارة سداد القروض", ControllerName = "LoanPayments" },

              // --- Contracts ---
              new ModuleModel { NameArabic = "إدارة معاملات التصديق", ControllerName = "ContractTransactions" },
              new ModuleModel { NameArabic = "إدارة أنواع العقود", ControllerName = "ContractTypes" },
              new ModuleModel { NameArabic = "التقارير المالية (التصديقات)", ControllerName = "FinancialReports" },
              new ModuleModel { NameArabic = "إدارة حجوزات الحصص", ControllerName = "ShareManagement" },
              new ModuleModel { NameArabic = "إدارة الإعفاءات", ControllerName = "ContractExemptionReasons" },
              new ModuleModel { NameArabic = "إدارة صفات الأطراف", ControllerName = "PartyRoles" },
              new ModuleModel { NameArabic = "إدارة صفات القُصّر", ControllerName = "MinorRelationships" },

              // --- Stamps ---
              new ModuleModel { NameArabic = "إدارة مخزون الطوابع", ControllerName = "StampInventory" },
              new ModuleModel { NameArabic = "صرف دفاتر الطوابع", ControllerName = "StampIssuance" },
              new ModuleModel { NameArabic = "تسجيل بيع الطوابع", ControllerName = "StampSales" },
              new ModuleModel { NameArabic = "التقارير المالية (طوابع)", ControllerName = "StampFinancials" },
              new ModuleModel { NameArabic = "إدارة حجوزات (الطوابع)", ControllerName = "StampShareManagement" },
              new ModuleModel { NameArabic = "إدارة المساعدات المالية", ControllerName = "FinancialAid" },

              // --- Admin Services ---
              new ModuleModel { NameArabic = "المراسلات الداخلية", ControllerName = "Messaging" },
              new ModuleModel { NameArabic = "إدارة جلسات المجلس", ControllerName = "CouncilSessions" },
              new ModuleModel { NameArabic = "إدارة اللجان الفرعية", ControllerName = "Committees" },
              new ModuleModel { NameArabic = "متابعة تنفيذ القرارات", ControllerName = "DecisionFollowUp" },
              new ModuleModel { NameArabic = "مهامي (تنفيذ القرارات)", ControllerName = "MyExecutionTasks" },
              new ModuleModel { NameArabic = "تنسيق جدول الأعمال", ControllerName = "CoordinatorInbox" },

              // --- General System ---
              new ModuleModel { NameArabic = "لوحة التحكم الرئيسية", ControllerName = "Home" },
              new ModuleModel { NameArabic = "إدارة المستخدمين", ControllerName = "Users" },
              new ModuleModel { NameArabic = "إدارة الصلاحيات", ControllerName = "Permissions" },
              new ModuleModel { NameArabic = "أعضاء المجلس", ControllerName = "CouncilMembers" },
              new ModuleModel { NameArabic = "الجداول المساعدة", ControllerName = "LookupManagement" },
              new ModuleModel { NameArabic = "سجلات التدقيق", ControllerName = "AuditLogs" },
              new ModuleModel { NameArabic = "المحافظات", ControllerName = "Provinces" },
              new ModuleModel { NameArabic = "ملف المحامي", ControllerName = "LawyerProfile" },
              new ModuleModel { NameArabic = "الإفادات والوثائق", ControllerName = "OfficialReports" },
              new ModuleModel { NameArabic = "الاستعلام المركزي", ControllerName = "CentralQuery" },
              new ModuleModel { NameArabic = "أرشيف المحامي", ControllerName = "LawyerArchive" },
              new ModuleModel { NameArabic = "استيراد وتصدير البيانات", ControllerName = "DataExchange" },
              new ModuleModel { NameArabic = "التقارير الشاملة", ControllerName = "Reports" }
          );
            context.SaveChanges();


            // ============================================================
            // 5. Lookups
            // ============================================================
            context.Genders.AddOrUpdate(g => g.Name, new Gender { Name = "ذكر" }, new Gender { Name = "أنثى" });

            context.NationalIdTypes.AddOrUpdate(n => n.Name,
                new NationalIdType { Name = "رقم الهوية" },
                new NationalIdType { Name = "بطاقة تعريف" },
                new NationalIdType { Name = "رقم جواز سفر" }
            );

            context.QuestionTypes.AddOrUpdate(qt => qt.Name,
                new QuestionType { Name = "اختيار من متعدد" },
                new QuestionType { Name = "صح / خطأ" },
                new QuestionType { Name = "مقالي" }
            );

            context.ExamTypes.AddOrUpdate(et => et.Name,
                new ExamType { Name = "امتحان قبول" },
                new ExamType { Name = "امتحان إنهاء تدريب" },
                new ExamType { Name = "اختبار وظيفي" }
            );

            context.ApplicationStatuses.AddOrUpdate(s => s.Name,
                 new ApplicationStatus { Name = "طلب جديد" },
                 new ApplicationStatus { Name = "بانتظار استكمال النواقص" },
                 new ApplicationStatus { Name = "بانتظار الموافقة النهائية" },
                 new ApplicationStatus { Name = "مقبول (بانتظار الدفع)" },
                 new ApplicationStatus { Name = "بانتظار دفع الرسوم" },
                 new ApplicationStatus { Name = "قيد المراجعة" },
                 new ApplicationStatus { Name = "متدرب مقيد" },
                 new ApplicationStatus { Name = "متدرب موقوف" },
                 new ApplicationStatus { Name = "محامي مزاول" },
                 new ApplicationStatus { Name = "بانتظار تجديد المزاولة" },
                 new ApplicationStatus { Name = "محامي غير مزاول" },
                 new ApplicationStatus { Name = "محامي متقاعد" },
                 new ApplicationStatus { Name = "محامي موظف" },
                 new ApplicationStatus { Name = "محامي موقوف" },
                 new ApplicationStatus { Name = "محامي متوفي" },
                 new ApplicationStatus { Name = "محامي مشطوب" },
                 new ApplicationStatus { Name = "مقبول" },
                 new ApplicationStatus { Name = "مرفوض" },
                 new ApplicationStatus { Name = "متدرب غير مقيد" },
                 new ApplicationStatus { Name = "بانتظار موافقة لجنة اليمين" },
                 new ApplicationStatus { Name = "بانتظار دفع رسوم اليمين" },
                 new ApplicationStatus { Name = "بانتظار تحديد موعد اليمين" }
            );

            context.QualificationTypes.AddOrUpdate(q => q.Name,
                new QualificationType { Name = "شهادة ثانوية عامة", MinimumAcceptancePercentage = 50.0 },
                new QualificationType { Name = "بكالوريوس", MinimumAcceptancePercentage = 60.0 },
                new QualificationType { Name = "ليسانس", MinimumAcceptancePercentage = 60.0 },
                new QualificationType { Name = "ماجستير", MinimumAcceptancePercentage = null },
                new QualificationType { Name = "دكتوراه", MinimumAcceptancePercentage = null }
            );

            context.AttachmentTypes.AddOrUpdate(a => a.Name,
                new AttachmentType { Name = "صورة شخصية" },
                new AttachmentType { Name = "صورة الهوية" },
                new AttachmentType { Name = "موافقة المشرف" },
                new AttachmentType { Name = "شهادة الثانوية العامة" },
                new AttachmentType { Name = "الشهادة الجامعية" },
                new AttachmentType { Name = "شهادة ميلاد" },
                new AttachmentType { Name = "كتاب موافقة مشرف قديم" },
                new AttachmentType { Name = "كتاب موافقة مشرف جديد" },
                new AttachmentType { Name = "مرفق طلب وقف" },
                new AttachmentType { Name = "مرفق طلب استكمال" },
                new AttachmentType { Name = "ملف البحث النهائي" },
                new AttachmentType { Name = "نموذج انتهاء التمرين" },
                new AttachmentType { Name = "شهادة مواظبة" }
            );

            // ============================================================
            // 6. System Settings
            // ============================================================
            context.SystemSettings.AddOrUpdate(s => s.SettingKey,
                new SystemSetting { SettingKey = "ExamRegistrationStartDate", SettingValue = DateTime.Now.ToString("yyyy-MM-dd") },
                new SystemSetting { SettingKey = "ExamRegistrationEndDate", SettingValue = DateTime.Now.AddDays(30).ToString("yyyy-MM-dd") },
                new SystemSetting { SettingKey = "RequiredTrainingHours", SettingValue = "100" },
                new SystemSetting { SettingKey = "StampSaleLawyerPercentage", SettingValue = "0.50" },
                new SystemSetting { SettingKey = "MinHighSchoolScore", SettingValue = "50" },
                new SystemSetting { SettingKey = "MinBachelorScore", SettingValue = "60" },
                new SystemSetting { SettingKey = "RenewalGracePeriodEndDate", SettingValue = $"{DateTime.Now.Year}-03-31" },

                // 👇👇👇 الإضافات الجديدة للرواتب 👇👇👇
                new SystemSetting { SettingKey = "AnnualIncrementPercent", SettingValue = "5" }, // نسبة الزيادة السنوية 5%
                new SystemSetting { SettingKey = "EmployeePensionPercent", SettingValue = "7" }, // نسبة استقطاع الموظف 7%
                new SystemSetting { SettingKey = "EmployerPensionPercent", SettingValue = "9" }  // نسبة مساهمة النقابة 9%



                );
            context.SaveChanges();

            // ============================================================
            // 7. Bank Accounts and Fee Types
            // ============================================================
            var shekelCurrencyId = context.Currencies.FirstOrDefault(c => c.Symbol == "₪")?.Id;
            var jodiCurrencyId = context.Currencies.FirstOrDefault(c => c.Symbol == "JD")?.Id;
            int defaultBankAccountId = 0;

            if (shekelCurrencyId.HasValue)
            {
                context.BankAccounts.AddOrUpdate(b => b.AccountNumber,
                    new BankAccount { BankName = "بنك فلسطين", AccountName = "الحساب الجاري", AccountNumber = "123456", CurrencyId = shekelCurrencyId.Value, IsActive = true, Iban = "PSXXPALX0470XXXXXX00123456001" },
                    new BankAccount { BankName = "البنك الإسلامي", AccountName = "حساب الرسوم", AccountNumber = "654321", CurrencyId = shekelCurrencyId.Value, IsActive = true, Iban = "PSXXPALI0450XXXXXX00654321001" }
                );
                context.SaveChanges();
                defaultBankAccountId = context.BankAccounts.FirstOrDefault()?.Id ?? 0;
            }

            if (defaultBankAccountId > 0 && jodiCurrencyId.HasValue && shekelCurrencyId.HasValue)
            {
                context.FeeTypes.AddOrUpdate(f => f.Name,
                    new FeeType { Name = "رسوم تسجيل متدرب جديد", DefaultAmount = 200, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم تجديد سنوي للمتدربين", DefaultAmount = 150, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم دورة تدريبية", DefaultAmount = 100, CurrencyId = shekelCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم استئناف تدريب", DefaultAmount = 50, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم نقل إشراف", DefaultAmount = 75, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم امتحان القبول", DefaultAmount = 100, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم بطاقة التدريب (الكارنيه)", DefaultAmount = 20, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم صندوق تعاون (متدرب)", DefaultAmount = 12, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم متعلقات التدريب", DefaultAmount = 5, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },

                    new FeeType { Name = "رسوم انتماء مزاولة (أول مرة)", DefaultAmount = 500, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم شهادة إجازة المحاماة", DefaultAmount = 50, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم بطاقة المزاولة (الكارنيه)", DefaultAmount = 20, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "تجديد مزاولة (سنوي)", DefaultAmount = 300, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم صندوق التعاون", DefaultAmount = 10, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم الزمالة", DefaultAmount = 15, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "غرامة تأخير (عام)", DefaultAmount = 5, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },

                    new FeeType { Name = "رسوم تصديق عقد", DefaultAmount = 0, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم طوابع", DefaultAmount = 1, CurrencyId = shekelCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true, LawyerPercentage = 0.50m, BarSharePercentage = 0.50m },

                    new FeeType { Name = "رسوم تقاعد (الفئة الأولى: 30 أو أقل)", DefaultAmount = 50, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم تقاعد (الفئة الثانية: 31-40)", DefaultAmount = 100, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم تقاعد (الفئة الثالثة: 41-50)", DefaultAmount = 150, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true },
                    new FeeType { Name = "رسوم تقاعد (الفئة الرابعة: 51 فأكثر)", DefaultAmount = 200, CurrencyId = jodiCurrencyId.Value, BankAccountId = defaultBankAccountId, IsActive = true }
                );
            }

            // ============================================================
            // 8. Fake Supervisors and Financial Records
            // ============================================================
            var practicingStatusId = context.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول")?.Id;
            var idTypeId = context.NationalIdTypes.FirstOrDefault()?.Id;
            var genderId = context.Genders.FirstOrDefault()?.Id;
            var graduateUserType = context.UserTypes.FirstOrDefault(ut => ut.NameEnglish == "Graduate");

            if (practicingStatusId.HasValue && idTypeId.HasValue && genderId.HasValue && graduateUserType != null)
            {
                // Supervisor 1
                string supervisor1_NationalId = "987654321";
                if (!context.Users.Any(u => u.Username == supervisor1_NationalId))
                {
                    var user1 = new UserModel { FullNameArabic = "المحامي ابراهيم احمد", Username = supervisor1_NationalId, Email = "supervisor1@example.com", IdentificationNumber = supervisor1_NationalId, IsActive = true, UserTypeId = graduateUserType.Id, HashedPassword = PasswordHelper.HashPassword("PBA@12345") };
                    var app1 = new GraduateApplication { ArabicName = "المحامي ابراهيم احمد", NationalIdNumber = supervisor1_NationalId, NationalIdTypeId = idTypeId.Value, BirthDate = new DateTime(1980, 1, 1), GenderId = genderId.Value, ApplicationStatusId = practicingStatusId.Value, SubmissionDate = new DateTime(2010, 5, 15), PracticeStartDate = new DateTime(2010, 5, 15), User = user1 };
                    context.GraduateApplications.Add(app1);
                }

                // Supervisor 2
                string supervisor2_NationalId = "987654322";
                if (!context.Users.Any(u => u.Username == supervisor2_NationalId))
                {
                    var user2 = new UserModel { FullNameArabic = "المحامية فاطمة علي", Username = supervisor2_NationalId, Email = "supervisor2@example.com", IdentificationNumber = supervisor2_NationalId, IsActive = true, UserTypeId = graduateUserType.Id, HashedPassword = PasswordHelper.HashPassword("PBA@12345") };
                    var app2 = new GraduateApplication { ArabicName = "المحامية فاطمة علي", NationalIdNumber = supervisor2_NationalId, NationalIdTypeId = idTypeId.Value, BirthDate = new DateTime(1982, 2, 20), GenderId = genderId.Value, ApplicationStatusId = practicingStatusId.Value, SubmissionDate = new DateTime(2012, 8, 1), PracticeStartDate = new DateTime(2012, 8, 1), User = user2 };
                    context.GraduateApplications.Add(app2);
                }
                context.SaveChanges();

                // Financial records for supervisors
                var renewalFee = context.FeeTypes.FirstOrDefault(f => f.Name.Contains("تجديد مزاولة"));
                var supervisorsToSeed = context.GraduateApplications.Where(x => x.NationalIdNumber == "987654321" || x.NationalIdNumber == "987654322").ToList();

                if (renewalFee != null)
                {
                    foreach (var supervisor in supervisorsToSeed)
                    {
                        bool hasReceipts = context.PaymentVouchers.Any(pv => pv.GraduateApplicationId == supervisor.Id);
                        if (!hasReceipts)
                        {
                            for (int i = 1; i <= 5; i++)
                            {
                                int year = DateTime.Now.Year - i;
                                var voucher = new PaymentVoucher
                                {
                                    GraduateApplicationId = supervisor.Id,
                                    IssueDate = new DateTime(year, 1, 15),
                                    ExpiryDate = new DateTime(year, 12, 31),
                                    TotalAmount = renewalFee.DefaultAmount,
                                    Status = "مسدد",
                                    PaymentMethod = "نقدي",
                                    IssuedByUserId = 1,
                                    IssuedByUserName = "System Seeder",
                                    VoucherDetails = new List<VoucherDetail> { new VoucherDetail { FeeTypeId = renewalFee.Id, Amount = renewalFee.DefaultAmount, BankAccountId = renewalFee.BankAccountId, Description = $"رسوم تجديد مزاولة لعام {year}" } }
                                };
                                context.PaymentVouchers.Add(voucher);
                                context.SaveChanges();

                                context.Receipts.Add(new Receipt
                                {
                                    Id = voucher.Id,
                                    Year = year,
                                    SequenceNumber = supervisor.Id * 100 + i,
                                    BankPaymentDate = new DateTime(year, 1, 16),
                                    BankReceiptNumber = $"REF-{year}-{supervisor.Id}",
                                    CreationDate = new DateTime(year, 1, 17),
                                    IssuedByUserId = 1,
                                    IssuedByUserName = "System Seeder"
                                });
                            }
                        }
                    }
                    context.SaveChanges();
                }
            }

            // ============================================================
            // 9. Create Admin User
            // ============================================================
            var adminRole = context.UserTypes.FirstOrDefault(ut => ut.NameEnglish == "Administrator");
            if (adminRole != null && !context.Users.Any(u => u.Username == "admin"))
            {
                context.Users.Add(new UserModel
                {
                    FullNameArabic = "المدير العام",
                    Username = "admin",
                    Email = "admin@example.com",
                    IdentificationNumber = "000000000",
                    IsActive = true,
                    UserTypeId = adminRole.Id,
                    HashedPassword = PasswordHelper.HashPassword("Admin@123")
                });
                context.SaveChanges();
            }

            // ============================================================
            // 9. Grant Permissions to Admin (تحديث الصلاحيات)
            // ============================================================
            var adminTypeId = context.UserTypes.FirstOrDefault(ut => ut.NameEnglish == "Administrator")?.Id;
            if (adminTypeId.HasValue)
            {
                var allModuleIds = context.Modules.Select(m => m.Id).ToList();
                foreach (var moduleId in allModuleIds)
                {
                    context.Permissions.AddOrUpdate(p => new { p.UserTypeId, p.ModuleId },
                        new PermissionModel { UserTypeId = adminTypeId.Value, ModuleId = moduleId, CanView = true, CanAdd = true, CanEdit = true, CanDelete = true, CanExport = true, CanImport = true }
                    );
                }
                context.SaveChanges();
            }

            // ============================================================
            // 10. Grant Committee Portal Access
            // ============================================================
            var committeePortalModule = context.Modules.FirstOrDefault(m => m.ControllerName == "CommitteePortal");
            if (committeePortalModule != null)
            {
                var rolesToGrant = new[] { "Advocate", "CommitteeMember" };
                foreach (var roleName in rolesToGrant)
                {
                    var role = context.UserTypes.FirstOrDefault(ut => ut.NameEnglish == roleName);
                    if (role != null)
                    {
                        context.Permissions.AddOrUpdate(p => new { p.UserTypeId, p.ModuleId },
                            new PermissionModel
                            {
                                UserTypeId = role.Id,
                                ModuleId = committeePortalModule.Id,
                                CanView = true,
                                CanAdd = true,
                                CanEdit = true,
                                CanDelete = false,
                                CanExport = false,
                                CanImport = false
                            }
                        );
                    }
                }
                context.SaveChanges();
            }

            // ============================================================
            // 11. FINANCIAL SYSTEM SEEDING
            // ============================================================
            SeedFinancialSystem(context);

            // ============================================================
            // 12. HR Initial Data (New)
            // ============================================================
            context.Departments.AddOrUpdate(d => d.Name,
                new Department { Name = "الإدارة العامة" },
                new Department { Name = "تكنولوجيا المعلومات" },
                new Department { Name = "الشؤون القانونية" },
                new Department { Name = "المالية" }
            );

            context.JobTitles.AddOrUpdate(j => j.Name,
                new JobTitle { Name = "مدير" },
                new JobTitle { Name = "محاسب" },
                new JobTitle { Name = "مبرمج" },
                new JobTitle { Name = "سكرتير" }
            );
            context.SaveChanges();
        }

        private void SeedFinancialSystem(ApplicationDbContext context)
        {
            // 1. Years
            if (!context.FiscalYears.Any())
            {
                context.FiscalYears.AddOrUpdate(y => y.Name,
                    new FiscalYear { Name = "2024", StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31), IsClosed = true, IsCurrent = false },
                    new FiscalYear { Name = "2025", StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 12, 31), IsClosed = false, IsCurrent = true }
                );
                context.SaveChanges();
            }

            // 2. Cost Centers
            if (!context.CostCenters.Any())
            {
                context.CostCenters.AddOrUpdate(c => c.Code,
                    new CostCenter { Code = "100", Name = "الإدارة العامة" },
                    new CostCenter { Code = "200", Name = "لجنة التدريب" },
                    new CostCenter { Code = "300", Name = "لجنة الحريات" },
                    new CostCenter { Code = "400", Name = "المقر - غزة" },
                    new CostCenter { Code = "500", Name = "المقر - خانيونس" }
                );
                context.SaveChanges();
            }

            // 3. Chart of Accounts (COA)
            if (!context.Accounts.Any())
            {
                // --- المستوى الأول ---
                var assets = new Account { Code = "1", Name = "الأصول", AccountType = AccountType.Asset, Level = 1, IsTransactional = false };
                var liabilities = new Account { Code = "2", Name = "الخصوم", AccountType = AccountType.Liability, Level = 1, IsTransactional = false };
                var equity = new Account { Code = "3", Name = "حقوق الملكية", AccountType = AccountType.Equity, Level = 1, IsTransactional = false };
                var revenues = new Account { Code = "4", Name = "الإيرادات", AccountType = AccountType.Revenue, Level = 1, IsTransactional = false };
                var expenses = new Account { Code = "5", Name = "المصروفات", AccountType = AccountType.Expense, Level = 1, IsTransactional = false };

                context.Accounts.AddOrUpdate(a => a.Code, assets, liabilities, equity, revenues, expenses);
                context.SaveChanges();

                // --- المستوى الثاني (أصول) ---
                context.Accounts.AddOrUpdate(a => a.Code,
                    new Account { Code = "11", Name = "الأصول المتداولة", ParentId = assets.Id, AccountType = AccountType.Asset, Level = 2, IsTransactional = false },
                    new Account { Code = "12", Name = "الأصول الثابتة", ParentId = assets.Id, AccountType = AccountType.Asset, Level = 2, IsTransactional = false }
                );
                context.SaveChanges();

                var currentAssets = context.Accounts.FirstOrDefault(a => a.Code == "11");
                var fixedAssets = context.Accounts.FirstOrDefault(a => a.Code == "12");

                // --- المستوى الثالث (الأصول المتداولة) - هنا الترتيب الجديد ---
                if (currentAssets != null)
                {
                    context.Accounts.AddOrUpdate(a => a.Code,
                        new Account { Code = "1101", Name = "النقدية بالصندوق", ParentId = currentAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true },
                        new Account { Code = "1102", Name = "النقدية بالبنوك", ParentId = currentAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true },
                        new Account { Code = "1103", Name = "الذمم المدينة (المحامين)", ParentId = currentAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true },
                        // الحساب الجديد للشيكات (هام جداً)
                        new Account { Code = "1104", Name = "شيكات برسم التحصيل", ParentId = currentAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true },
                        // الحساب الجديد للذمم المرتجعة
                        new Account { Code = "1105", Name = "ذمم شيكات مرتجعة", ParentId = currentAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true },

                        // إعادة ترتيب الباقي
                        new Account { Code = "1106", Name = "مخزون الطوابع", ParentId = currentAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true },
                        new Account { Code = "1107", Name = "الذمم المدينة (المتعهدين)", ParentId = currentAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true },
                        new Account { Code = "1108", Name = "سلف وقروض الموظفين", ParentId = currentAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true }
                    );
                }

                // --- المستوى الثالث (الأصول الثابتة) ---
                if (fixedAssets != null)
                {
                    context.Accounts.AddOrUpdate(a => a.Code,
                        new Account { Code = "1201", Name = "الأراضي والمباني", ParentId = fixedAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true },
                        new Account { Code = "1202", Name = "الأثاث والمفروشات", ParentId = fixedAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true },
                        new Account { Code = "1203", Name = "أجهزة كمبيوتر وبرمجيات", ParentId = fixedAssets.Id, AccountType = AccountType.Asset, Level = 3, IsTransactional = true }
                    );
                }

                // --- المستوى الثاني (الخصوم) ---
                context.Accounts.AddOrUpdate(a => a.Code,
                    new Account { Code = "21", Name = "الخصوم المتداولة", ParentId = liabilities.Id, AccountType = AccountType.Liability, Level = 2, IsTransactional = false }
                );
                context.SaveChanges();
                var currentLiabilities = context.Accounts.FirstOrDefault(a => a.Code == "21");

                if (currentLiabilities != null)
                {
                    context.Accounts.AddOrUpdate(a => a.Code,
                        new Account { Code = "2101", Name = "الذمم الدائنة (موردين)", ParentId = currentLiabilities.Id, AccountType = AccountType.Liability, Level = 3, IsTransactional = true },
                        new Account { Code = "2102", Name = "أمانات الطوابع (حصة النقابة)", ParentId = currentLiabilities.Id, AccountType = AccountType.Liability, Level = 3, IsTransactional = true },
                        new Account { Code = "2103", Name = "رسوم محصلة مقدماً", ParentId = currentLiabilities.Id, AccountType = AccountType.Liability, Level = 3, IsTransactional = true }
                    );
                }

                // --- المستوى الثاني (الإيرادات) ---
                context.Accounts.AddOrUpdate(a => a.Code,
                    new Account { Code = "41", Name = "إيرادات العضوية", ParentId = revenues.Id, AccountType = AccountType.Revenue, Level = 2, IsTransactional = false },
                    new Account { Code = "42", Name = "إيرادات الخدمات", ParentId = revenues.Id, AccountType = AccountType.Revenue, Level = 2, IsTransactional = false }
                );
                context.SaveChanges();
                var memberRev = context.Accounts.FirstOrDefault(a => a.Code == "41");
                var serviceRev = context.Accounts.FirstOrDefault(a => a.Code == "42");

                if (memberRev != null)
                {
                    context.Accounts.AddOrUpdate(a => a.Code,
                        new Account { Code = "4101", Name = "رسوم الانتساب", ParentId = memberRev.Id, AccountType = AccountType.Revenue, Level = 3, IsTransactional = true },
                        new Account { Code = "4102", Name = "رسوم الاشتراك السنوي", ParentId = memberRev.Id, AccountType = AccountType.Revenue, Level = 3, IsTransactional = true },
                        new Account { Code = "4103", Name = "رسوم إعادة القيد", ParentId = memberRev.Id, AccountType = AccountType.Revenue, Level = 3, IsTransactional = true }
                    );
                }

                if (serviceRev != null)
                {
                    context.Accounts.AddOrUpdate(a => a.Code,
                        new Account { Code = "4201", Name = "إيرادات بيع الطوابع", ParentId = serviceRev.Id, AccountType = AccountType.Revenue, Level = 3, IsTransactional = true },
                        new Account { Code = "4202", Name = "رسوم تصديق العقود", ParentId = serviceRev.Id, AccountType = AccountType.Revenue, Level = 3, IsTransactional = true },
                        new Account { Code = "4203", Name = "رسوم الدورات التدريبية", ParentId = serviceRev.Id, AccountType = AccountType.Revenue, Level = 3, IsTransactional = true },
                        new Account { Code = "4204", Name = "رسوم الامتحانات", ParentId = serviceRev.Id, AccountType = AccountType.Revenue, Level = 3, IsTransactional = true }
                    );
                }

                // --- المستوى الثاني (المصروفات) ---
                context.Accounts.AddOrUpdate(a => a.Code,
                    new Account { Code = "51", Name = "المصاريف الإدارية والعمومية", ParentId = expenses.Id, AccountType = AccountType.Expense, Level = 2, IsTransactional = false },
                    new Account { Code = "52", Name = "مصاريف الأنشطة واللجان", ParentId = expenses.Id, AccountType = AccountType.Expense, Level = 2, IsTransactional = false }
                );
                context.SaveChanges();
                var adminExp = context.Accounts.FirstOrDefault(a => a.Code == "51");
                var activityExp = context.Accounts.FirstOrDefault(a => a.Code == "52");

                if (adminExp != null)
                {
                    context.Accounts.AddOrUpdate(a => a.Code,
                        new Account { Code = "5101", Name = "الرواتب والأجور", ParentId = adminExp.Id, AccountType = AccountType.Expense, Level = 3, IsTransactional = true },
                        new Account { Code = "5102", Name = "إيجار المقرات", ParentId = adminExp.Id, AccountType = AccountType.Expense, Level = 3, IsTransactional = true },
                        new Account { Code = "5103", Name = "كهرباء ومياه واتصالات", ParentId = adminExp.Id, AccountType = AccountType.Expense, Level = 3, IsTransactional = true },
                        new Account { Code = "5104", Name = "قرطاسية ومطبوعات", ParentId = adminExp.Id, AccountType = AccountType.Expense, Level = 3, IsTransactional = true },
                        new Account { Code = "5105", Name = "ضيافة ونظافة", ParentId = adminExp.Id, AccountType = AccountType.Expense, Level = 3, IsTransactional = true }
                    );
                }

                if (activityExp != null)
                {
                    context.Accounts.AddOrUpdate(a => a.Code,
                        new Account { Code = "5201", Name = "مكافآت لجان المناقشة", ParentId = activityExp.Id, AccountType = AccountType.Expense, Level = 3, IsTransactional = true },
                        new Account { Code = "5202", Name = "مصاريف حفل حلف اليمين", ParentId = activityExp.Id, AccountType = AccountType.Expense, Level = 3, IsTransactional = true },
                        new Account { Code = "5203", Name = "تكلفة طباعة الطوابع", ParentId = activityExp.Id, AccountType = AccountType.Expense, Level = 3, IsTransactional = true },
                        new Account { Code = "5204", Name = "مصاريف الدورات التدريبية", ParentId = activityExp.Id, AccountType = AccountType.Expense, Level = 3, IsTransactional = true }
                    );
                }

                context.SaveChanges();
            }
        }
    }
}