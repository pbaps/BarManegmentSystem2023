using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure; // ضروري للـ DbQuery
using System.Data.Entity.ModelConfiguration.Conventions;

namespace BarManegment.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection")
        {
            // تعطيل الوكلاء لتجنب مشاكل الـ Serialization
            this.Configuration.ProxyCreationEnabled = false;
            // تعطيل مهيئ قاعدة البيانات والاعتماد على Migrations
            Database.SetInitializer<ApplicationDbContext>(null);
        }

        /// <summary>
        /// //////مساعدات مالية
        /// </summary>
        // ==================================================================
        // =========================== الجداول (DbSets) =====================
        // ==================================================================
        public virtual DbSet<FinancialAidType> FinancialAidTypes { get; set; }
        public virtual DbSet<BarExpense> BarExpenses { get; set; }
        public virtual DbSet<LawyerFinancialAid> LawyerFinancialAids { get; set; }
        // 1. المستخدمين والصلاحيات
        public DbSet<UserModel> Users { get; set; }
        public DbSet<UserTypeModel> UserTypes { get; set; }
        public DbSet<ModuleModel> Modules { get; set; }
        public DbSet<PermissionModel> Permissions { get; set; }
        public DbSet<AuditLogModel> AuditLogs { get; set; }

        // 2. الموارد البشرية (HR & Payroll)
        public DbSet<Department> Departments { get; set; }
        public DbSet<JobTitle> JobTitles { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeFinancialHistory> EmployeeFinancialHistories { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        // جداول الرواتب
        public DbSet<MonthlyPayroll> MonthlyPayrolls { get; set; }
        public DbSet<PayrollSlip> PayrollSlips { get; set; }
        public DbSet<EmployeePayrollSlip> EmployeePayrollSlips { get; set; }

        // 3. النظام المالي والمحاسبي
        public DbSet<FiscalYear> FiscalYears { get; set; }
        public DbSet<CostCenter> CostCenters { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }

        // ✅ الجدول المعتمد حالياً (الاسم الجديد)
        public DbSet<JournalEntryDetail> JournalEntryDetails { get; set; }

        // ⚠️ حل سحري للتوافق مع الكود القديم:
        // هذه الخاصية تجعل الكود القديم الذي يطلب "JournalEntryLines" يقرأ من "JournalEntryDetails"
        public DbQuery<JournalEntryDetail> JournalEntryLines
        {
            get { return Set<JournalEntryDetail>(); }
        }

        public DbSet<CheckPortfolio> ChecksPortfolio { get; set; }
        public DbSet<FeeType> FeeTypes { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<PaymentVoucher> PaymentVouchers { get; set; }
        public DbSet<VoucherDetail> VoucherDetails { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<DeferredFee> DeferredFees { get; set; }
        public DbSet<FeeDistribution> FeeDistributions { get; set; }
        public DbSet<GeneralExpense> GeneralExpenses { get; set; }
        public DbSet<ExchangeRate> ExchangeRates { get; set; }

        // 4. المخازن والمشتريات
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<ItemCategory> ItemCategories { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        public DbSet<StockIssue> StockIssues { get; set; }
        public DbSet<StockIssueItem> StockIssueItems { get; set; }

        // 5. شؤون المحامين والمتدربين
        public DbSet<GraduateApplication> GraduateApplications { get; set; }
        public DbSet<ContactInfo> ContactInfos { get; set; }
        public DbSet<Qualification> Qualifications { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<ApplicationStatus> ApplicationStatuses { get; set; }
        public DbSet<QualificationType> QualificationTypes { get; set; }
        public DbSet<AttachmentType> AttachmentTypes { get; set; }
        public DbSet<Gender> Genders { get; set; }
        public DbSet<NationalIdType> NationalIdTypes { get; set; }

        // البيانات الشخصية الموسعة
        public DbSet<LawyerPersonalData> LawyerPersonalDatas { get; set; }
        public DbSet<LawyerSpouse> LawyerSpouses { get; set; }
        public DbSet<LawyerChild> LawyerChildren { get; set; }
        public DbSet<LawyerOffice> LawyerOffices { get; set; }
        public DbSet<OfficePartner> OfficePartners { get; set; }
        public DbSet<SecurityHealthRecord> SecurityHealthRecords { get; set; }
        public DbSet<InjuryRecord> InjuryRecords { get; set; }

        // 6. التدريب والإشراف
        public DbSet<TrainingCourse> TrainingCourses { get; set; }
        public DbSet<TrainingSession> TrainingSessions { get; set; }
        public DbSet<TraineeAttendance> TraineeAttendances { get; set; }
        public DbSet<TrainingLog> TrainingLogs { get; set; }
        public DbSet<SupervisorHistory> SupervisorHistories { get; set; }
        public DbSet<SupervisorChangeRequest> SupervisorChangeRequests { get; set; }
        public DbSet<TraineeRenewal> TraineeRenewals { get; set; }
        public DbSet<PracticingLawyerRenewal> PracticingLawyerRenewals { get; set; }
        public DbSet<TraineeSuspension> TraineeSuspensions { get; set; }

        // 7. اليمين والمزاولة
        public DbSet<OathRequest> OathRequests { get; set; }
        public DbSet<OathCeremony> OathCeremonies { get; set; }

        // 8. الامتحانات (التحريري والشفوي)
        public DbSet<ExamApplication> ExamApplications { get; set; }
        public DbSet<ExamQualification> ExamQualifications { get; set; }
        public DbSet<ExamType> ExamTypes { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionType> QuestionTypes { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<ExamEnrollment> ExamEnrollments { get; set; }
        public DbSet<TraineeAnswer> TraineeAnswers { get; set; }
        public DbSet<ManualGrade> ManualGrades { get; set; }

        // اللجان الشفوية
        public DbSet<OralExamCommittee> OralExamCommittees { get; set; }
        public DbSet<OralExamCommitteeMember> OralExamCommitteeMembers { get; set; }
        public DbSet<OralExamEnrollment> OralExamEnrollments { get; set; }

        // 9. الأبحاث واللجان
        public DbSet<LegalResearch> LegalResearches { get; set; }
        public DbSet<DiscussionCommittee> DiscussionCommittees { get; set; }
        public DbSet<Committee> Committees { get; set; }
        public DbSet<CommitteeMember> CommitteeMembers { get; set; }
        public DbSet<CommitteeMeeting> CommitteeMeetings { get; set; }
        public DbSet<CommitteeCase> CommitteeCases { get; set; }
        public DbSet<CommitteePanelMember> CommitteePanelMembers { get; set; }
        public DbSet<CommitteeDecision> CommitteeDecisions { get; set; }

        // 10. التصديقات والعقود
        public DbSet<ContractType> ContractTypes { get; set; }
        public DbSet<ContractTransaction> ContractTransactions { get; set; }
        public DbSet<TransactionParty> TransactionParties { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<PartyRole> PartyRoles { get; set; }
        public DbSet<ContractExemptionReason> ContractExemptionReasons { get; set; }
        public DbSet<PassportMinor> PassportMinors { get; set; }
        public DbSet<MinorRelationship> MinorRelationships { get; set; }

        // 11. نظام الطوابع
        public DbSet<StampContractor> StampContractors { get; set; }
        public DbSet<StampBook> StampBooks { get; set; }
        public DbSet<StampBookIssuance> StampBookIssuances { get; set; }
        public DbSet<Stamp> Stamps { get; set; }
        public DbSet<StampSale> StampSales { get; set; }

        // 12. نظام القروض
        public DbSet<LoanType> LoanTypes { get; set; }
        public DbSet<LoanApplication> LoanApplications { get; set; }
        public DbSet<Guarantor> Guarantors { get; set; }
        public DbSet<LoanInstallment> LoanInstallments { get; set; }

        // 13. أخرى
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<CouncilMember> CouncilMembers { get; set; }
        public DbSet<CaseSession> CaseSessions { get; set; }
        public DbSet<CaseDocument> CaseDocuments { get; set; }
        public DbSet<CouncilSession> CouncilSessions { get; set; }
        public DbSet<SessionAttendance> SessionAttendances { get; set; }
        public DbSet<AgendaItem> AgendaItems { get; set; }
        public DbSet<AgendaAttachment> AgendaAttachments { get; set; }
        public DbSet<InternalMessage> InternalMessages { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<SystemLookup> SystemLookups { get; set; }


        // ==================================================================
        // ======================== إعدادات العلاقات ========================
        // ==================================================================
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();

            // --- ضبط دقة الأرقام المالية ---
            modelBuilder.Entity<JournalEntry>().Property(j => j.TotalDebit).HasPrecision(18, 2);
            modelBuilder.Entity<JournalEntry>().Property(j => j.TotalCredit).HasPrecision(18, 2);

            // استخدام JournalEntryDetail
            modelBuilder.Entity<JournalEntryDetail>().Property(l => l.Debit).HasPrecision(18, 2);
            modelBuilder.Entity<JournalEntryDetail>().Property(l => l.Credit).HasPrecision(18, 2);

            modelBuilder.Entity<Account>().Property(a => a.OpeningBalance).HasPrecision(18, 2);
            modelBuilder.Entity<ExchangeRate>().Property(x => x.Rate).HasPrecision(18, 4);
            modelBuilder.Entity<CheckPortfolio>().Property(x => x.Amount).HasPrecision(18, 2);

            // --- علاقات النظام المالي ---
            modelBuilder.Entity<JournalEntry>()
                .HasMany(j => j.JournalEntryDetails)
                .WithRequired(d => d.JournalEntry)
                .WillCascadeOnDelete(true);

            // --- علاقات البيانات الشخصية ---
            modelBuilder.Entity<LawyerPersonalData>()
                .HasRequired(p => p.Lawyer)
                .WithOptional(g => g.LawyerPersonalData)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<LawyerOffice>()
                .HasRequired(o => o.LawyerData)
                .WithOptional(p => p.Office)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<SecurityHealthRecord>()
                .HasRequired(r => r.LawyerData)
                .WithOptional(p => p.HealthRecord)
                .WillCascadeOnDelete(true);

            // --- علاقات القروض ---
            modelBuilder.Entity<LoanApplication>()
                .HasRequired(l => l.Lawyer)
                .WithMany(g => g.LoanApplications)
                .HasForeignKey(l => l.LawyerId)
                .WillCascadeOnDelete(false);

            // --- علاقات المراسلات ---
            modelBuilder.Entity<InternalMessage>()
                .HasRequired(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<InternalMessage>()
                .HasRequired(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<MessageAttachment>()
                .HasRequired(a => a.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(a => a.InternalMessageId)
                .WillCascadeOnDelete(true);

            // --- علاقات المستخدم ---
            modelBuilder.Entity<GraduateApplication>()
                 .HasOptional(g => g.User)
                 .WithMany(u => u.GraduateApplications)
                 .HasForeignKey(g => g.UserId)
                 .WillCascadeOnDelete(false);

            // --- علاقات التدريب والإشراف واليمين ---
            modelBuilder.Entity<TraineeSuspension>()
                .HasRequired(s => s.Trainee).WithMany().HasForeignKey(s => s.GraduateApplicationId).WillCascadeOnDelete(false);

            modelBuilder.Entity<TraineeSuspension>()
                .HasOptional(s => s.CreatedByUser).WithMany().HasForeignKey(s => s.CreatedByUserId).WillCascadeOnDelete(false);

            modelBuilder.Entity<OathRequest>()
                .HasRequired(o => o.Trainee).WithMany(g => g.OathRequests).HasForeignKey(o => o.GraduateApplicationId).WillCascadeOnDelete(false);

            modelBuilder.Entity<OathRequest>()
                .HasOptional(o => o.PaymentVoucher).WithMany().HasForeignKey(o => o.PaymentVoucherId).WillCascadeOnDelete(false);

            modelBuilder.Entity<GraduateApplication>()
                .HasOptional(g => g.OathCeremony).WithMany(c => c.Attendees).HasForeignKey(g => g.OathCeremonyId).WillCascadeOnDelete(false);

            // --- علاقات اللجان والأبحاث ---
            modelBuilder.Entity<OralExamCommitteeMember>()
                .HasRequired(m => m.OralExamCommittee).WithMany(c => c.Members).HasForeignKey(m => m.OralExamCommitteeId).WillCascadeOnDelete(true);

            modelBuilder.Entity<OralExamCommitteeMember>()
                .HasRequired(m => m.MemberLawyer).WithMany().HasForeignKey(m => m.MemberLawyerId).WillCascadeOnDelete(false);

            modelBuilder.Entity<OralExamEnrollment>()
                .HasRequired(e => e.OralExamCommittee).WithMany(c => c.Enrollments).HasForeignKey(e => e.OralExamCommitteeId).WillCascadeOnDelete(false);

            modelBuilder.Entity<OralExamEnrollment>()
                .HasRequired(e => e.Trainee).WithMany().HasForeignKey(e => e.GraduateApplicationId).WillCascadeOnDelete(false);

            modelBuilder.Entity<LegalResearch>()
                .HasOptional(lr => lr.Committee).WithMany(dc => dc.Researches).HasForeignKey(lr => lr.DiscussionCommitteeId).WillCascadeOnDelete(false);

            modelBuilder.Entity<CommitteeMember>()
                .HasRequired(cm => cm.DiscussionCommittee).WithMany(dc => dc.Members).HasForeignKey(cm => cm.DiscussionCommitteeId).WillCascadeOnDelete(true);

            modelBuilder.Entity<CommitteeMember>()
                .HasRequired(cm => cm.MemberLawyer).WithMany().HasForeignKey(cm => cm.MemberLawyerId).WillCascadeOnDelete(false);

            modelBuilder.Entity<CommitteeDecision>()
                .HasRequired(cd => cd.LegalResearch).WithMany(lr => lr.Decisions).HasForeignKey(cd => cd.LegalResearchId).WillCascadeOnDelete(false);

            // --- علاقات الامتحانات ---
            modelBuilder.Entity<GraduateApplication>().HasOptional(g => g.ExamApplication).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<Question>().HasRequired(q => q.QuestionType).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<TraineeAnswer>().HasRequired(ta => ta.ExamEnrollment).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<TraineeAnswer>().HasRequired(ta => ta.Question).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<TraineeAnswer>().HasOptional(ta => ta.SelectedAnswer).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<ManualGrade>().HasRequired(g => g.TraineeAnswer).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<ManualGrade>().HasRequired(g => g.Grader).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<Exam>().HasRequired(e => e.ExamType).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<ExamEnrollment>().HasRequired(e => e.Exam).WithMany(ex => ex.Enrollments).WillCascadeOnDelete(false);
            modelBuilder.Entity<ExamEnrollment>().HasOptional(e => e.ExamApplication).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<ExamEnrollment>().HasOptional(e => e.GraduateApplication).WithMany().WillCascadeOnDelete(false);

            // --- علاقات التدريب ---
            modelBuilder.Entity<TraineeRenewal>().HasRequired(r => r.Trainee).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<TraineeRenewal>().HasRequired(r => r.Receipt).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<TraineeAttendance>().HasRequired(t => t.Trainee).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<TraineeAttendance>().HasRequired(t => t.Session).WithMany(s => s.Attendances).WillCascadeOnDelete(false);
            modelBuilder.Entity<TrainingSession>().HasRequired(s => s.TrainingCourse).WithMany(c => c.Sessions).WillCascadeOnDelete(false);

            modelBuilder.Entity<SupervisorHistory>().HasRequired(h => h.GraduateApplication).WithMany(a => a.SupervisorChanges).WillCascadeOnDelete(false);
            modelBuilder.Entity<SupervisorHistory>().HasOptional(h => h.OldSupervisor).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<SupervisorHistory>().HasRequired(h => h.NewSupervisor).WithMany().WillCascadeOnDelete(false);

            modelBuilder.Entity<SupervisorChangeRequest>().HasRequired(r => r.Trainee).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<SupervisorChangeRequest>().HasOptional(r => r.OldSupervisor).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<SupervisorChangeRequest>().HasOptional(r => r.NewSupervisor).WithMany().WillCascadeOnDelete(false);

            // --- علاقات المالية والرسوم ---
            modelBuilder.Entity<VoucherDetail>()
                .HasRequired(vd => vd.FeeType)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PaymentVoucher>()
                .HasOptional(v => v.GraduateApplication)
                .WithMany()
                .HasForeignKey(v => v.GraduateApplicationId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FeeType>()
                .HasRequired(ft => ft.Currency)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FeeType>()
               .HasRequired(ft => ft.BankAccount)
               .WithMany()
               .WillCascadeOnDelete(false);

            modelBuilder.Entity<BankAccount>()
               .HasRequired(b => b.Currency)
               .WithMany()
               .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }

    // =========================================================
    // تعريفات الموديلات الأساسية للمستخدمين
    // =========================================================

    [Table("Users")]
    public class UserModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "حقل الاسم الكامل مطلوب.")]
        [Display(Name = "الاسم الكامل")]
        public string FullNameArabic { get; set; }

        [Required(ErrorMessage = "حقل اسم المستخدم مطلوب.")]
        [Index(IsUnique = true)]
        [StringLength(50)]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; }

        [Required(ErrorMessage = "حقل البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "الرجاء إدخال بريد إلكتروني صحيح.")]
        [Index(IsUnique = true)]
        [StringLength(100)]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; }

        [Required(ErrorMessage = "حقل رقم الهوية مطلوب.")]
        [Display(Name = "رقم الهوية")]
        public string IdentificationNumber { get; set; }

        [Display(Name = "الصورة الشخصية")]
        public string ProfilePicturePath { get; set; }

        [Required]
        public string HashedPassword { get; set; }

        [NotMapped]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Display(Name = "نوع المستخدم")]
        public int UserTypeId { get; set; }
        public virtual UserTypeModel UserType { get; set; }

        [Display(Name = "الحالة")]
        public bool IsActive { get; set; }

        public virtual ICollection<GraduateApplication> GraduateApplications { get; set; }
        public string ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordTokenExpiration { get; set; }
        public virtual ICollection<AuditLogModel> AuditLogs { get; set; }

        public UserModel()
        {
            AuditLogs = new HashSet<AuditLogModel>();
            GraduateApplications = new HashSet<GraduateApplication>();
        }
    }

    [Table("UserTypes")]
    public class UserTypeModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string NameArabic { get; set; }
        public string NameEnglish { get; set; }
        public virtual ICollection<UserModel> Users { get; set; }
        public virtual ICollection<PermissionModel> Permissions { get; set; }
    }

    [Table("Modules")]
    public class ModuleModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string NameArabic { get; set; }
        [Required]
        public string ControllerName { get; set; }
        public virtual ICollection<PermissionModel> Permissions { get; set; }
    }

    [Table("Permissions")]
    public class PermissionModel
    {
        [Key, Column(Order = 0)]
        public int UserTypeId { get; set; }

        [Key, Column(Order = 1)]
        public int ModuleId { get; set; }

        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanExport { get; set; }
        public bool CanImport { get; set; }

        public virtual UserTypeModel UserType { get; set; }
        public virtual ModuleModel Module { get; set; }
    }

    [Table("AuditLogs")]
    public class AuditLogModel
    {
        [Key]
        public int Id { get; set; }
        public int? UserId { get; set; }
        public virtual UserModel User { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
    }
}
