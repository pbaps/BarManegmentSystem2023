namespace BarManegment.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Account",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false),
                        Name = c.String(nullable: false),
                        Level = c.Int(nullable: false),
                        AccountType = c.Int(nullable: false),
                        ParentId = c.Int(),
                        OpeningBalance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsTransactional = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.AgendaAttachments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AgendaItemId = c.Int(nullable: false),
                        FileName = c.String(),
                        FilePath = c.String(),
                        UploadedBy = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AgendaItems", t => t.AgendaItemId)
                .Index(t => t.AgendaItemId);
            
            CreateTable(
                "dbo.AgendaItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CouncilSessionId = c.Int(),
                        RequestType = c.String(),
                        Source = c.String(),
                        Title = c.String(),
                        Description = c.String(),
                        RequesterLawyerId = c.Int(),
                        CreatedByUserId = c.String(),
                        IsApprovedForAgenda = c.Boolean(nullable: false),
                        AssignedEmployeeId = c.String(),
                        EmployeeStudyNotes = c.String(),
                        CouncilDecisionType = c.String(),
                        DecisionText = c.String(),
                        DecisionFilePath = c.String(),
                        IsVisibleToRequester = c.Boolean(nullable: false),
                        ExecutionStatus = c.String(),
                        EmployeeExecutionNotes = c.String(),
                        AssignedForExecutionUserId = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CouncilSessions", t => t.CouncilSessionId)
                .Index(t => t.CouncilSessionId);
            
            CreateTable(
                "dbo.CouncilSessions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SessionNumber = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        SessionDate = c.DateTime(nullable: false),
                        Location = c.String(),
                        SignedMinutesPath = c.String(),
                        IsFinalized = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SessionAttendances",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CouncilSessionId = c.Int(nullable: false),
                        MemberName = c.String(),
                        IsPresent = c.Boolean(nullable: false),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CouncilSessions", t => t.CouncilSessionId)
                .Index(t => t.CouncilSessionId);
            
            CreateTable(
                "dbo.Answer",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        QuestionId = c.Int(nullable: false),
                        AnswerText = c.String(nullable: false),
                        IsCorrect = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Question", t => t.QuestionId)
                .Index(t => t.QuestionId);
            
            CreateTable(
                "dbo.Question",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ExamId = c.Int(nullable: false),
                        QuestionTypeId = c.Int(nullable: false),
                        QuestionText = c.String(nullable: false),
                        Points = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Exam", t => t.ExamId)
                .ForeignKey("dbo.QuestionType", t => t.QuestionTypeId)
                .Index(t => t.ExamId)
                .Index(t => t.QuestionTypeId);
            
            CreateTable(
                "dbo.Exam",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ExamTypeId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 200),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(nullable: false),
                        DurationInMinutes = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        ShowResultInstantly = c.Boolean(nullable: false),
                        PassingPercentage = c.Double(nullable: false),
                        MinPracticeYears = c.Int(),
                        RequiredApplicationStatusId = c.Int(),
                        RequirementsNote = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ExamType", t => t.ExamTypeId)
                .ForeignKey("dbo.ApplicationStatus", t => t.RequiredApplicationStatusId)
                .Index(t => t.ExamTypeId)
                .Index(t => t.RequiredApplicationStatusId);
            
            CreateTable(
                "dbo.ExamEnrollment",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ExamId = c.Int(nullable: false),
                        ExamApplicationId = c.Int(),
                        GraduateApplicationId = c.Int(),
                        Score = c.Double(),
                        Result = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Exam", t => t.ExamId)
                .ForeignKey("dbo.ExamApplication", t => t.ExamApplicationId)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .Index(t => t.ExamId)
                .Index(t => t.ExamApplicationId)
                .Index(t => t.GraduateApplicationId);
            
            CreateTable(
                "dbo.ExamApplication",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FullName = c.String(nullable: false, maxLength: 200),
                        NationalIdNumber = c.String(nullable: false, maxLength: 50),
                        BirthDate = c.DateTime(nullable: false),
                        GenderId = c.Int(nullable: false),
                        MobileNumber = c.String(nullable: false, maxLength: 20),
                        WhatsAppNumber = c.String(maxLength: 20),
                        Email = c.String(nullable: false, maxLength: 100),
                        HighSchoolCertificatePath = c.String(),
                        BachelorCertificatePath = c.String(),
                        PersonalIdPath = c.String(),
                        ApplicationDate = c.DateTime(nullable: false),
                        Status = c.String(nullable: false, maxLength: 50),
                        TemporaryPassword = c.String(maxLength: 256),
                        RejectionReason = c.String(),
                        ExamScore = c.Double(),
                        ExamResult = c.String(),
                        TelegramChatId = c.Long(),
                        IsAccountCreated = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Gender", t => t.GenderId)
                .Index(t => t.NationalIdNumber, unique: true)
                .Index(t => t.GenderId);
            
            CreateTable(
                "dbo.Gender",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ExamQualification",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ExamApplicationId = c.Int(nullable: false),
                        QualificationType = c.String(nullable: false, maxLength: 100),
                        UniversityName = c.String(maxLength: 200),
                        GraduationYear = c.Int(nullable: false),
                        GradePercentage = c.Double(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ExamApplication", t => t.ExamApplicationId)
                .Index(t => t.ExamApplicationId);
            
            CreateTable(
                "dbo.GraduateApplication",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ArabicName = c.String(nullable: false, maxLength: 200),
                        EnglishName = c.String(maxLength: 200),
                        NationalIdNumber = c.String(nullable: false, maxLength: 50),
                        NationalIdTypeId = c.Int(nullable: false),
                        BirthDate = c.DateTime(nullable: false),
                        BirthPlace = c.String(maxLength: 100),
                        Nationality = c.String(maxLength: 100),
                        GenderId = c.Int(nullable: false),
                        ApplicationStatusId = c.Int(nullable: false),
                        PersonalPhotoPath = c.String(maxLength: 500),
                        SubmissionDate = c.DateTime(nullable: false),
                        SupervisorId = c.Int(),
                        UserId = c.Int(),
                        TrainingStartDate = c.DateTime(),
                        PracticeStartDate = c.DateTime(),
                        MembershipId = c.String(maxLength: 20),
                        OathCeremonyId = c.Int(),
                        Notes = c.String(),
                        TraineeSerialNo = c.String(maxLength: 20),
                        TelegramChatId = c.Long(),
                        BankName = c.String(maxLength: 100),
                        BankBranch = c.String(maxLength: 100),
                        AccountNumber = c.String(maxLength: 50),
                        Iban = c.String(maxLength: 34),
                        ExamApplicationId = c.Int(),
                        WalletNumber = c.String(),
                        WalletProviderId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ApplicationStatus", t => t.ApplicationStatusId)
                .ForeignKey("dbo.ExamApplication", t => t.ExamApplicationId)
                .ForeignKey("dbo.Gender", t => t.GenderId)
                .ForeignKey("dbo.NationalIdType", t => t.NationalIdTypeId)
                .ForeignKey("dbo.OathCeremony", t => t.OathCeremonyId)
                .ForeignKey("dbo.GraduateApplication", t => t.SupervisorId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .ForeignKey("dbo.SystemLookup", t => t.WalletProviderId)
                .Index(t => t.NationalIdTypeId)
                .Index(t => t.GenderId)
                .Index(t => t.ApplicationStatusId)
                .Index(t => t.SupervisorId)
                .Index(t => t.UserId)
                .Index(t => t.MembershipId)
                .Index(t => t.OathCeremonyId)
                .Index(t => t.ExamApplicationId)
                .Index(t => t.WalletProviderId);
            
            CreateTable(
                "dbo.ApplicationStatus",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Attachment",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FilePath = c.String(nullable: false),
                        OriginalFileName = c.String(maxLength: 255),
                        UploadDate = c.DateTime(nullable: false),
                        GraduateApplicationId = c.Int(nullable: false),
                        AttachmentTypeId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AttachmentType", t => t.AttachmentTypeId)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .Index(t => t.GraduateApplicationId)
                .Index(t => t.AttachmentTypeId);
            
            CreateTable(
                "dbo.AttachmentType",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ContactInfo",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        Governorate = c.String(maxLength: 100),
                        City = c.String(maxLength: 100),
                        Street = c.String(maxLength: 200),
                        BuildingNumber = c.String(maxLength: 20),
                        MobileNumber = c.String(maxLength: 20),
                        NationalMobileNumber = c.String(maxLength: 20),
                        HomePhoneNumber = c.String(maxLength: 20),
                        WhatsAppNumber = c.String(maxLength: 20),
                        Email = c.String(maxLength: 100),
                        EmergencyContactPerson = c.String(maxLength: 100),
                        EmergencyContactNumber = c.String(maxLength: 20),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.Id)
                .Index(t => t.Id);
            
            CreateTable(
                "dbo.LawyerPersonalData",
                c => new
                    {
                        LawyerId = c.Int(nullable: false),
                        MaritalStatus = c.String(nullable: false, maxLength: 50),
                        DisplacementGovernorate = c.String(),
                    })
                .PrimaryKey(t => t.LawyerId)
                .ForeignKey("dbo.GraduateApplication", t => t.LawyerId)
                .Index(t => t.LawyerId);
            
            CreateTable(
                "dbo.LawyerChildren",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LawyerId = c.Int(nullable: false),
                        FullName = c.String(nullable: false),
                        NationalId = c.String(),
                        BirthDate = c.DateTime(nullable: false),
                        Gender = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.LawyerPersonalData", t => t.LawyerId)
                .Index(t => t.LawyerId);
            
            CreateTable(
                "dbo.SecurityHealthRecord",
                c => new
                    {
                        LawyerId = c.Int(nullable: false),
                        WasDetained = c.Boolean(nullable: false),
                        DetentionStartDate = c.DateTime(),
                        DetentionEndDate = c.DateTime(),
                        DetentionPlace = c.String(),
                        DetentionAffidavitPath = c.String(),
                        WasInjured = c.Boolean(nullable: false),
                        GeneralHealthStatus = c.String(),
                        HasHealthInsurance = c.Boolean(nullable: false),
                        HealthInsuranceNumber = c.String(),
                        IsTakingMedication = c.Boolean(nullable: false),
                        MedicationsList = c.String(),
                    })
                .PrimaryKey(t => t.LawyerId)
                .ForeignKey("dbo.LawyerPersonalData", t => t.LawyerId, cascadeDelete: true)
                .Index(t => t.LawyerId);
            
            CreateTable(
                "dbo.InjuryRecords",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LawyerId = c.Int(nullable: false),
                        InjuredName = c.String(),
                        Relationship = c.String(),
                        InjuryLocation = c.String(),
                        MedicalReportPath = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SecurityHealthRecord", t => t.LawyerId)
                .Index(t => t.LawyerId);
            
            CreateTable(
                "dbo.LawyerOffice",
                c => new
                    {
                        LawyerId = c.Int(nullable: false),
                        OfficeName = c.String(),
                        Governorate = c.String(),
                        Area = c.String(),
                        Street = c.String(),
                        Building = c.String(),
                        Floor = c.String(),
                        PropertyType = c.String(),
                        OwnershipType = c.String(),
                        CurrentCondition = c.String(),
                        DamageDetails = c.String(),
                    })
                .PrimaryKey(t => t.LawyerId)
                .ForeignKey("dbo.LawyerPersonalData", t => t.LawyerId, cascadeDelete: true)
                .Index(t => t.LawyerId);
            
            CreateTable(
                "dbo.OfficePartners",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LawyerId = c.Int(nullable: false),
                        PartnerName = c.String(),
                        PartnerIdentification = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.LawyerOffice", t => t.LawyerId)
                .Index(t => t.LawyerId);
            
            CreateTable(
                "dbo.LawyerSpouses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LawyerId = c.Int(nullable: false),
                        FullName = c.String(nullable: false),
                        NationalId = c.String(),
                        OccupationType = c.String(),
                        WorkPlace = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.LawyerPersonalData", t => t.LawyerId)
                .Index(t => t.LawyerId);
            
            CreateTable(
                "dbo.LegalResearch",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GraduateApplicationId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 500),
                        SubmissionDate = c.DateTime(nullable: false),
                        Status = c.String(nullable: false, maxLength: 100),
                        FinalDocumentPath = c.String(maxLength: 500),
                        DiscussionCommitteeId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.DiscussionCommittee", t => t.DiscussionCommitteeId)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .Index(t => t.GraduateApplicationId)
                .Index(t => t.DiscussionCommitteeId);
            
            CreateTable(
                "dbo.DiscussionCommittee",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CommitteeName = c.String(nullable: false, maxLength: 200),
                        FormationDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CommitteeMember",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DiscussionCommitteeId = c.Int(nullable: false),
                        MemberLawyerId = c.Int(nullable: false),
                        Role = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.DiscussionCommittee", t => t.DiscussionCommitteeId, cascadeDelete: true)
                .ForeignKey("dbo.GraduateApplication", t => t.MemberLawyerId)
                .Index(t => t.DiscussionCommitteeId)
                .Index(t => t.MemberLawyerId);
            
            CreateTable(
                "dbo.CommitteeDecision",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LegalResearchId = c.Int(nullable: false),
                        Result = c.String(nullable: false, maxLength: 100),
                        DecisionDate = c.DateTime(nullable: false),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.LegalResearch", t => t.LegalResearchId)
                .Index(t => t.LegalResearchId);
            
            CreateTable(
                "dbo.LoanApplications",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LawyerId = c.Int(nullable: false),
                        LoanTypeId = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        InstallmentCount = c.Int(nullable: false),
                        InstallmentAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        StartDate = c.DateTime(nullable: false),
                        ApplicationDate = c.DateTime(nullable: false),
                        Status = c.String(nullable: false, maxLength: 100),
                        IsDisbursed = c.Boolean(nullable: false),
                        DisbursementDate = c.DateTime(),
                        ApplicationFormPath = c.String(maxLength: 500),
                        CouncilApprovalScannedPath = c.String(maxLength: 500),
                        MainPromissoryNoteScannedPath = c.String(maxLength: 500),
                        DebtBondScannedPath = c.String(maxLength: 500),
                        Notes = c.String(maxLength: 1000),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.LawyerId)
                .ForeignKey("dbo.LoanTypes", t => t.LoanTypeId)
                .Index(t => t.LawyerId)
                .Index(t => t.LoanTypeId);
            
            CreateTable(
                "dbo.Guarantors",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LoanApplicationId = c.Int(nullable: false),
                        GuarantorType = c.String(nullable: false, maxLength: 50),
                        LawyerGuarantorId = c.Int(),
                        IsOverride = c.Boolean(nullable: false),
                        ExternalName = c.String(maxLength: 200),
                        ExternalIdNumber = c.String(maxLength: 50),
                        JobTitle = c.String(maxLength: 150),
                        Workplace = c.String(maxLength: 200),
                        WorkplaceEmployeeId = c.String(maxLength: 50),
                        NetSalary = c.Decimal(precision: 18, scale: 2),
                        BankName = c.String(maxLength: 100),
                        BankAccountNumber = c.String(maxLength: 100),
                        GuarantorFormScannedPath = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.LawyerGuarantorId)
                .ForeignKey("dbo.LoanApplications", t => t.LoanApplicationId)
                .Index(t => t.LoanApplicationId)
                .Index(t => t.LawyerGuarantorId);
            
            CreateTable(
                "dbo.LoanInstallments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        LoanApplicationId = c.Int(nullable: false),
                        InstallmentNumber = c.Int(nullable: false),
                        DueDate = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Status = c.String(nullable: false, maxLength: 50),
                        PromissoryNoteScannedPath = c.String(maxLength: 500),
                        PaymentVoucherId = c.Int(),
                        ReceiptId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.LoanApplications", t => t.LoanApplicationId)
                .ForeignKey("dbo.PaymentVoucher", t => t.PaymentVoucherId)
                .ForeignKey("dbo.Receipt", t => t.ReceiptId)
                .Index(t => t.LoanApplicationId)
                .Index(t => t.PaymentVoucherId)
                .Index(t => t.ReceiptId);
            
            CreateTable(
                "dbo.PaymentVoucher",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GraduateApplicationId = c.Int(),
                        PaymentMethod = c.String(maxLength: 50),
                        CheckNumber = c.String(maxLength: 50),
                        ReferenceNumber = c.String(maxLength: 50),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IssueDate = c.DateTime(nullable: false),
                        ExpiryDate = c.DateTime(nullable: false),
                        Status = c.String(nullable: false),
                        IssuedByUserId = c.Int(nullable: false),
                        IssuedByUserName = c.String(nullable: false, maxLength: 150),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .Index(t => t.GraduateApplicationId);
            
            CreateTable(
                "dbo.Receipt",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        SequenceNumber = c.Int(nullable: false),
                        BankPaymentDate = c.DateTime(nullable: false),
                        BankReceiptNumber = c.String(nullable: false, maxLength: 100),
                        CreationDate = c.DateTime(nullable: false),
                        Notes = c.String(),
                        IssuedByUserId = c.Int(nullable: false),
                        IssuedByUserName = c.String(nullable: false, maxLength: 150),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PaymentVoucher", t => t.Id)
                .Index(t => t.Id)
                .Index(t => new { t.Year, t.SequenceNumber }, unique: true, name: "IX_Receipt_Year_Sequence");
            
            CreateTable(
                "dbo.VoucherDetail",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PaymentVoucherId = c.Int(nullable: false),
                        FeeTypeId = c.Int(nullable: false),
                        BankAccountId = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Description = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BankAccount", t => t.BankAccountId)
                .ForeignKey("dbo.FeeType", t => t.FeeTypeId)
                .ForeignKey("dbo.PaymentVoucher", t => t.PaymentVoucherId)
                .Index(t => t.PaymentVoucherId)
                .Index(t => t.FeeTypeId)
                .Index(t => t.BankAccountId);
            
            CreateTable(
                "dbo.BankAccount",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BankName = c.String(nullable: false),
                        AccountName = c.String(nullable: false),
                        AccountNumber = c.String(nullable: false),
                        Iban = c.String(),
                        CurrencyId = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        RelatedAccountId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Currency", t => t.CurrencyId)
                .ForeignKey("dbo.Account", t => t.RelatedAccountId)
                .Index(t => t.CurrencyId)
                .Index(t => t.RelatedAccountId);
            
            CreateTable(
                "dbo.Currency",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        Symbol = c.String(nullable: false, maxLength: 10),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.FeeType",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        DefaultAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CurrencyId = c.Int(nullable: false),
                        BankAccountId = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        LawyerPercentage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BarSharePercentage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RevenueAccountId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BankAccount", t => t.BankAccountId)
                .ForeignKey("dbo.Currency", t => t.CurrencyId)
                .ForeignKey("dbo.Account", t => t.RevenueAccountId)
                .Index(t => t.CurrencyId)
                .Index(t => t.BankAccountId)
                .Index(t => t.RevenueAccountId);
            
            CreateTable(
                "dbo.LoanTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 150),
                        BankAccountForRepaymentId = c.Int(nullable: false),
                        MaxAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        MaxInstallments = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BankAccount", t => t.BankAccountForRepaymentId)
                .Index(t => t.BankAccountForRepaymentId);
            
            CreateTable(
                "dbo.NationalIdType",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.OathCeremony",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CeremonyDate = c.DateTime(nullable: false),
                        Location = c.String(maxLength: 300),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.OathRequest",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GraduateApplicationId = c.Int(nullable: false),
                        RequestDate = c.DateTime(nullable: false),
                        Status = c.String(nullable: false, maxLength: 100),
                        CompletionFormPath = c.String(maxLength: 500),
                        SupervisorCertificatePath = c.String(maxLength: 500),
                        CommitteeNotes = c.String(),
                        PaymentVoucherId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PaymentVoucher", t => t.PaymentVoucherId)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .Index(t => t.GraduateApplicationId)
                .Index(t => t.PaymentVoucherId);
            
            CreateTable(
                "dbo.Qualification",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        QualificationTypeId = c.Int(nullable: false),
                        UniversityName = c.String(nullable: false, maxLength: 200),
                        Faculty = c.String(maxLength: 200),
                        Specialization = c.String(maxLength: 200),
                        GraduationYear = c.Int(nullable: false),
                        GradePercentage = c.Double(),
                        GraduateApplicationId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .ForeignKey("dbo.QualificationType", t => t.QualificationTypeId)
                .Index(t => t.QualificationTypeId)
                .Index(t => t.GraduateApplicationId);
            
            CreateTable(
                "dbo.QualificationType",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        MinimumAcceptancePercentage = c.Double(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SupervisorHistory",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GraduateApplicationId = c.Int(nullable: false),
                        OldSupervisorId = c.Int(),
                        NewSupervisorId = c.Int(nullable: false),
                        ChangeDate = c.DateTime(nullable: false),
                        Reason = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .ForeignKey("dbo.GraduateApplication", t => t.NewSupervisorId)
                .ForeignKey("dbo.GraduateApplication", t => t.OldSupervisorId)
                .Index(t => t.GraduateApplicationId)
                .Index(t => t.OldSupervisorId)
                .Index(t => t.NewSupervisorId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FullNameArabic = c.String(nullable: false),
                        Username = c.String(nullable: false, maxLength: 50),
                        Email = c.String(nullable: false, maxLength: 100),
                        IdentificationNumber = c.String(nullable: false),
                        ProfilePicturePath = c.String(),
                        HashedPassword = c.String(nullable: false),
                        UserTypeId = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        ResetPasswordToken = c.String(),
                        ResetPasswordTokenExpiration = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.UserTypes", t => t.UserTypeId)
                .Index(t => t.Username, unique: true)
                .Index(t => t.Email, unique: true)
                .Index(t => t.UserTypeId);
            
            CreateTable(
                "dbo.AuditLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(),
                        Timestamp = c.DateTime(nullable: false),
                        Action = c.String(),
                        Controller = c.String(),
                        Details = c.String(),
                        IpAddress = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.UserTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        NameArabic = c.String(nullable: false),
                        NameEnglish = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Permissions",
                c => new
                    {
                        UserTypeId = c.Int(nullable: false),
                        ModuleId = c.Int(nullable: false),
                        CanView = c.Boolean(nullable: false),
                        CanAdd = c.Boolean(nullable: false),
                        CanEdit = c.Boolean(nullable: false),
                        CanDelete = c.Boolean(nullable: false),
                        CanExport = c.Boolean(nullable: false),
                        CanImport = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => new { t.UserTypeId, t.ModuleId })
                .ForeignKey("dbo.Modules", t => t.ModuleId)
                .ForeignKey("dbo.UserTypes", t => t.UserTypeId)
                .Index(t => t.UserTypeId)
                .Index(t => t.ModuleId);
            
            CreateTable(
                "dbo.Modules",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        NameArabic = c.String(nullable: false),
                        ControllerName = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SystemLookup",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Category = c.String(nullable: false),
                        Name = c.String(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ExamType",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.QuestionType",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.BarExpenses",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ExpenseDate = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Description = c.String(),
                        BankAccountId = c.Int(nullable: false),
                        ExpenseCategory = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.BankAccount", t => t.BankAccountId)
                .Index(t => t.BankAccountId);
            
            CreateTable(
                "dbo.CaseDocuments",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CommitteeCaseId = c.Int(nullable: false),
                        Description = c.String(),
                        FilePath = c.String(),
                        UploadDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CommitteeCases", t => t.CommitteeCaseId)
                .Index(t => t.CommitteeCaseId);
            
            CreateTable(
                "dbo.CommitteeCases",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CommitteeId = c.Int(nullable: false),
                        CaseNumber = c.String(),
                        Subject = c.String(nullable: false),
                        SourceType = c.String(),
                        ComplainantName = c.String(),
                        TargetLawyerId = c.Int(),
                        Status = c.String(),
                        FinalRecommendation = c.String(),
                        CouncilDecisionNotes = c.String(),
                        CreatedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Committees", t => t.CommitteeId)
                .Index(t => t.CommitteeId);
            
            CreateTable(
                "dbo.Committees",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        Description = c.String(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CommitteeMeetings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CommitteeId = c.Int(nullable: false),
                        MeetingDate = c.DateTime(nullable: false),
                        Location = c.String(),
                        MinutesText = c.String(),
                        MinutesFilePath = c.String(),
                        IsCompleted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Committees", t => t.CommitteeId)
                .Index(t => t.CommitteeId);
            
            CreateTable(
                "dbo.CommitteePanelMembers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CommitteeId = c.Int(nullable: false),
                        LawyerId = c.Int(),
                        EmployeeUserId = c.String(),
                        Role = c.String(nullable: false),
                        JoinDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Committees", t => t.CommitteeId)
                .Index(t => t.CommitteeId);
            
            CreateTable(
                "dbo.CaseSessions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CommitteeCaseId = c.Int(nullable: false),
                        SessionDate = c.DateTime(nullable: false),
                        Title = c.String(),
                        Minutes = c.String(),
                        InterimDecision = c.String(),
                        IsCompleted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CommitteeCases", t => t.CommitteeCaseId)
                .Index(t => t.CommitteeCaseId);
            
            CreateTable(
                "dbo.CheckPortfolio",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CheckNumber = c.String(nullable: false),
                        BankName = c.String(nullable: false),
                        DueDate = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CurrencyId = c.Int(),
                        Status = c.Int(nullable: false),
                        ReceiptId = c.Int(),
                        DrawerName = c.String(),
                        ActionDate = c.DateTime(),
                        ActionJournalEntryId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Currency", t => t.CurrencyId)
                .ForeignKey("dbo.Receipt", t => t.ReceiptId)
                .Index(t => t.CurrencyId)
                .Index(t => t.ReceiptId);
            
            CreateTable(
                "dbo.ContractExemptionReasons",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Reason = c.String(nullable: false, maxLength: 200),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ContractTransactions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TransactionDate = c.DateTime(nullable: false),
                        LawyerId = c.Int(nullable: false),
                        ContractTypeId = c.Int(nullable: false),
                        FinalFee = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsExempt = c.Boolean(nullable: false),
                        ExemptionReasonId = c.Int(),
                        Notes = c.String(),
                        CertificationDate = c.DateTime(),
                        EmployeeId = c.Int(nullable: false),
                        ScannedContractPath = c.String(maxLength: 500),
                        Status = c.String(nullable: false, maxLength: 100),
                        PaymentVoucherId = c.Int(),
                        IsActingForSelf = c.Boolean(nullable: false),
                        AgentLegalCapacity = c.String(maxLength: 250),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ContractTypes", t => t.ContractTypeId)
                .ForeignKey("dbo.Users", t => t.EmployeeId)
                .ForeignKey("dbo.ContractExemptionReasons", t => t.ExemptionReasonId)
                .ForeignKey("dbo.GraduateApplication", t => t.LawyerId)
                .ForeignKey("dbo.PaymentVoucher", t => t.PaymentVoucherId)
                .Index(t => t.LawyerId)
                .Index(t => t.ContractTypeId)
                .Index(t => t.ExemptionReasonId)
                .Index(t => t.EmployeeId)
                .Index(t => t.PaymentVoucherId);
            
            CreateTable(
                "dbo.ContractTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 200),
                        DefaultFee = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CurrencyId = c.Int(nullable: false),
                        LawyerPercentage = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BarSharePercentage = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Currency", t => t.CurrencyId)
                .Index(t => t.CurrencyId);
            
            CreateTable(
                "dbo.PassportMinors",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ContractTransactionId = c.Int(nullable: false),
                        MinorName = c.String(nullable: false, maxLength: 200),
                        MinorIDNumber = c.String(nullable: false, maxLength: 50),
                        MinorRelationshipId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ContractTransactions", t => t.ContractTransactionId)
                .ForeignKey("dbo.MinorRelationships", t => t.MinorRelationshipId)
                .Index(t => t.ContractTransactionId)
                .Index(t => t.MinorRelationshipId);
            
            CreateTable(
                "dbo.MinorRelationships",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TransactionParties",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ContractTransactionId = c.Int(nullable: false),
                        PartyType = c.Int(nullable: false),
                        PartyName = c.String(nullable: false, maxLength: 200),
                        PartyIDNumber = c.String(nullable: false, maxLength: 50),
                        ProvinceId = c.Int(nullable: false),
                        PartyRoleId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ContractTransactions", t => t.ContractTransactionId)
                .ForeignKey("dbo.PartyRoles", t => t.PartyRoleId)
                .ForeignKey("dbo.Provinces", t => t.ProvinceId)
                .Index(t => t.ContractTransactionId)
                .Index(t => t.ProvinceId)
                .Index(t => t.PartyRoleId);
            
            CreateTable(
                "dbo.PartyRoles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Provinces",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CostCenter",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false),
                        Name = c.String(nullable: false),
                        ParentId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CostCenter", t => t.ParentId)
                .Index(t => t.ParentId);
            
            CreateTable(
                "dbo.CouncilMember",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 150),
                        Title = c.String(nullable: false, maxLength: 100),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.DeferredFee",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GraduateApplicationId = c.Int(nullable: false),
                        FeeTypeId = c.Int(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Reason = c.String(nullable: false),
                        DateDeferred = c.DateTime(nullable: false),
                        IsCharged = c.Boolean(nullable: false),
                        OathPaymentVoucherId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.FeeType", t => t.FeeTypeId)
                .ForeignKey("dbo.PaymentVoucher", t => t.OathPaymentVoucherId)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .Index(t => t.GraduateApplicationId)
                .Index(t => t.FeeTypeId)
                .Index(t => t.OathPaymentVoucherId);
            
            CreateTable(
                "dbo.Department",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        ManagerId = c.Int(),
                        AnnualIncrementPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EmployeePensionPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EmployerPensionPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.EmployeeFinancialHistory",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EmployeeId = c.Int(nullable: false),
                        ChangeDate = c.DateTime(nullable: false),
                        ChangedBy = c.String(),
                        ChangeReason = c.String(),
                        BasicSalary = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ManagerAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        HeadOfDeptAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        MasterDegreeAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PhdDegreeAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SpecializationAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TransportAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EmployeePensionPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EmployerPensionPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        OtherMonthlyDeduction = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Employee", t => t.EmployeeId)
                .Index(t => t.EmployeeId);
            
            CreateTable(
                "dbo.Employee",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FullName = c.String(nullable: false),
                        NationalId = c.String(),
                        Phone = c.String(),
                        ProfilePicturePath = c.String(),
                        HireDate = c.DateTime(nullable: false),
                        DepartmentId = c.Int(nullable: false),
                        JobTitleId = c.Int(nullable: false),
                        BasicSalary = c.Decimal(nullable: false, precision: 18, scale: 2),
                        AnnualIncrementPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        MaxIncrementYears = c.Int(nullable: false),
                        ManagerAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        HeadOfDeptAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        MasterDegreeAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        PhdDegreeAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        SpecializationAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TransportAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EmployeePensionPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EmployerPensionPercent = c.Decimal(nullable: false, precision: 18, scale: 2),
                        OtherMonthlyDeduction = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BankName = c.String(),
                        BankBranch = c.String(),
                        BankAccountNumber = c.String(),
                        IBAN = c.String(),
                        UserId = c.Int(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Department", t => t.DepartmentId)
                .ForeignKey("dbo.JobTitle", t => t.JobTitleId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.DepartmentId)
                .Index(t => t.JobTitleId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.JobTitle",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.EmployeePayrollSlip",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MonthlyPayrollId = c.Int(nullable: false),
                        EmployeeId = c.Int(nullable: false),
                        BasicSalary = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Allowances = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Deductions = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Bonuses = c.Decimal(nullable: false, precision: 18, scale: 2),
                        NetSalary = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Employee", t => t.EmployeeId)
                .ForeignKey("dbo.MonthlyPayroll", t => t.MonthlyPayrollId)
                .Index(t => t.MonthlyPayrollId)
                .Index(t => t.EmployeeId);
            
            CreateTable(
                "dbo.MonthlyPayroll",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        IssueDate = c.DateTime(nullable: false),
                        Notes = c.String(),
                        TotalGrossAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TotalNetAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsPostedToJournal = c.Boolean(nullable: false),
                        JournalEntryId = c.Int(),
                        CreatedBy = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.JournalEntry", t => t.JournalEntryId)
                .Index(t => t.JournalEntryId);
            
            CreateTable(
                "dbo.JournalEntry",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreatedByUserId = c.Int(),
                        EntryDate = c.DateTime(nullable: false),
                        Description = c.String(),
                        ReferenceNumber = c.String(),
                        IsPosted = c.Boolean(nullable: false),
                        CreatedBy = c.String(),
                        CreatedAt = c.DateTime(nullable: false),
                        FiscalYearId = c.Int(),
                        EntryNumber = c.String(),
                        SourceModule = c.String(),
                        SourceId = c.Int(),
                        PostedDate = c.DateTime(),
                        PostedByUserId = c.Int(),
                        ExchangeRate = c.Decimal(precision: 18, scale: 2),
                        CurrencyId = c.Int(),
                        TotalDebit = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TotalCredit = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.CreatedByUserId)
                .Index(t => t.CreatedByUserId);
            
            CreateTable(
                "dbo.JournalEntryDetail",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        JournalEntryId = c.Int(nullable: false),
                        AccountId = c.Int(nullable: false),
                        Debit = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Credit = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Description = c.String(),
                        CostCenterId = c.Int(),
                        CurrencyId = c.Int(),
                        ExchangeRate = c.Decimal(precision: 18, scale: 2),
                        ForeignDebit = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ForeignCredit = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Account", t => t.AccountId)
                .ForeignKey("dbo.CostCenter", t => t.CostCenterId)
                .ForeignKey("dbo.Currency", t => t.CurrencyId)
                .ForeignKey("dbo.JournalEntry", t => t.JournalEntryId, cascadeDelete: true)
                .Index(t => t.JournalEntryId)
                .Index(t => t.AccountId)
                .Index(t => t.CostCenterId)
                .Index(t => t.CurrencyId);
            
            CreateTable(
                "dbo.PayrollSlip",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MonthlyPayrollId = c.Int(nullable: false),
                        EmployeeId = c.Int(nullable: false),
                        BasicSalary = c.Decimal(nullable: false, precision: 18, scale: 2),
                        AllowancesTotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                        AnnualIncrementAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TransportAllowance = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EmployeePensionDeduction = c.Decimal(nullable: false, precision: 18, scale: 2),
                        OtherDeductions = c.Decimal(nullable: false, precision: 18, scale: 2),
                        GrossSalary = c.Decimal(nullable: false, precision: 18, scale: 2),
                        NetSalary = c.Decimal(nullable: false, precision: 18, scale: 2),
                        BankName = c.String(),
                        BankAccountNumber = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Employee", t => t.EmployeeId)
                .ForeignKey("dbo.MonthlyPayroll", t => t.MonthlyPayrollId)
                .Index(t => t.MonthlyPayrollId)
                .Index(t => t.EmployeeId);
            
            CreateTable(
                "dbo.ExchangeRate",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CurrencyId = c.Int(nullable: false),
                        Rate = c.Decimal(nullable: false, precision: 18, scale: 4),
                        Date = c.DateTime(nullable: false),
                        CreatedBy = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Currency", t => t.CurrencyId)
                .Index(t => t.CurrencyId);
            
            CreateTable(
                "dbo.FeeDistributions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ReceiptId = c.Int(nullable: false),
                        ContractTransactionId = c.Int(nullable: false),
                        LawyerId = c.Int(),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ShareType = c.String(nullable: false, maxLength: 100),
                        IsSentToBank = c.Boolean(nullable: false),
                        BankSendDate = c.DateTime(),
                        IsOnHold = c.Boolean(nullable: false),
                        HoldReason = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ContractTransactions", t => t.ContractTransactionId)
                .ForeignKey("dbo.GraduateApplication", t => t.LawyerId)
                .ForeignKey("dbo.Receipt", t => t.ReceiptId)
                .Index(t => t.ReceiptId)
                .Index(t => t.ContractTransactionId)
                .Index(t => t.LawyerId);
            
            CreateTable(
                "dbo.FinancialAidTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        MaxAmount = c.Decimal(precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.FiscalYear",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                        IsClosed = c.Boolean(nullable: false),
                        IsCurrent = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.GeneralExpense",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        VoucherNumber = c.String(nullable: false),
                        ExpenseDate = c.DateTime(nullable: false),
                        PayeeName = c.String(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Description = c.String(nullable: false),
                        ExpenseAccountId = c.Int(nullable: false),
                        CostCenterId = c.Int(),
                        PaymentMethod = c.String(nullable: false),
                        TreasuryAccountId = c.Int(nullable: false),
                        ReferenceNumber = c.String(),
                        CreatedByUserId = c.Int(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                        IsPosted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CostCenter", t => t.CostCenterId)
                .ForeignKey("dbo.Account", t => t.ExpenseAccountId)
                .ForeignKey("dbo.Account", t => t.TreasuryAccountId)
                .Index(t => t.ExpenseAccountId)
                .Index(t => t.CostCenterId)
                .Index(t => t.TreasuryAccountId);
            
            CreateTable(
                "dbo.InternalMessage",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Subject = c.String(nullable: false, maxLength: 255),
                        Body = c.String(nullable: false),
                        Timestamp = c.DateTime(nullable: false),
                        ParentMessageId = c.Int(),
                        SenderId = c.Int(nullable: false),
                        RecipientId = c.Int(nullable: false),
                        IsRead = c.Boolean(nullable: false),
                        HasAttachment = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.InternalMessage", t => t.ParentMessageId)
                .ForeignKey("dbo.Users", t => t.RecipientId)
                .ForeignKey("dbo.Users", t => t.SenderId)
                .Index(t => t.ParentMessageId)
                .Index(t => t.SenderId)
                .Index(t => t.RecipientId);
            
            CreateTable(
                "dbo.MessageAttachment",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        InternalMessageId = c.Int(nullable: false),
                        OriginalFileName = c.String(nullable: false, maxLength: 255),
                        FilePath = c.String(nullable: false, maxLength: 500),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.InternalMessage", t => t.InternalMessageId, cascadeDelete: true)
                .Index(t => t.InternalMessageId);
            
            CreateTable(
                "dbo.ItemCategory",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Item",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        Code = c.String(),
                        ItemCategoryId = c.Int(nullable: false),
                        CurrentQuantity = c.Int(nullable: false),
                        AverageCost = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ReorderLevel = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ItemCategory", t => t.ItemCategoryId)
                .Index(t => t.ItemCategoryId);
            
            CreateTable(
                "dbo.LawyerFinancialAids",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BatchReference = c.String(),
                        LawyerId = c.Int(nullable: false),
                        AidTypeId = c.Int(nullable: false),
                        DecisionDate = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CurrencyId = c.Int(nullable: false),
                        DisbursementMethod = c.String(),
                        TargetBankName = c.String(),
                        TargetBankBranch = c.String(),
                        TargetIban = c.String(),
                        TargetWalletNumber = c.String(),
                        IsPaid = c.Boolean(nullable: false),
                        PaymentDate = c.DateTime(),
                        ExpenseId = c.Int(),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.SystemLookup", t => t.AidTypeId)
                .ForeignKey("dbo.Currency", t => t.CurrencyId)
                .ForeignKey("dbo.BarExpenses", t => t.ExpenseId)
                .ForeignKey("dbo.GraduateApplication", t => t.LawyerId)
                .Index(t => t.LawyerId)
                .Index(t => t.AidTypeId)
                .Index(t => t.CurrencyId)
                .Index(t => t.ExpenseId);
            
            CreateTable(
                "dbo.LeaveRequest",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        EmployeeId = c.Int(nullable: false),
                        LeaveTypeId = c.Int(nullable: false),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                        DaysCount = c.Int(nullable: false),
                        Reason = c.String(),
                        Status = c.String(),
                        ManagerComment = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Employee", t => t.EmployeeId)
                .ForeignKey("dbo.LeaveType", t => t.LeaveTypeId)
                .Index(t => t.EmployeeId)
                .Index(t => t.LeaveTypeId);
            
            CreateTable(
                "dbo.LeaveType",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        DefaultDaysPerYear = c.Int(nullable: false),
                        IsPaid = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ManualGrade",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TraineeAnswerId = c.Int(nullable: false),
                        GraderId = c.Int(nullable: false),
                        Status = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.GraderId)
                .ForeignKey("dbo.TraineeAnswer", t => t.TraineeAnswerId)
                .Index(t => t.TraineeAnswerId)
                .Index(t => t.GraderId);
            
            CreateTable(
                "dbo.TraineeAnswer",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ExamEnrollmentId = c.Int(nullable: false),
                        QuestionId = c.Int(nullable: false),
                        SelectedAnswerId = c.Int(),
                        EssayAnswerText = c.String(),
                        Score = c.Double(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ExamEnrollment", t => t.ExamEnrollmentId)
                .ForeignKey("dbo.Question", t => t.QuestionId)
                .ForeignKey("dbo.Answer", t => t.SelectedAnswerId)
                .Index(t => t.ExamEnrollmentId)
                .Index(t => t.QuestionId)
                .Index(t => t.SelectedAnswerId);
            
            CreateTable(
                "dbo.OralExamCommitteeMember",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OralExamCommitteeId = c.Int(nullable: false),
                        MemberLawyerId = c.Int(nullable: false),
                        Role = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.MemberLawyerId)
                .ForeignKey("dbo.OralExamCommittee", t => t.OralExamCommitteeId, cascadeDelete: true)
                .Index(t => t.OralExamCommitteeId)
                .Index(t => t.MemberLawyerId);
            
            CreateTable(
                "dbo.OralExamCommittee",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CommitteeName = c.String(nullable: false, maxLength: 200),
                        FormationDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.OralExamEnrollment",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GraduateApplicationId = c.Int(nullable: false),
                        OralExamCommitteeId = c.Int(nullable: false),
                        ExamDate = c.DateTime(nullable: false),
                        Result = c.String(nullable: false, maxLength: 100),
                        Score = c.Double(),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.OralExamCommittee", t => t.OralExamCommitteeId)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .Index(t => t.GraduateApplicationId)
                .Index(t => t.OralExamCommitteeId);
            
            CreateTable(
                "dbo.PracticingLawyerRenewal",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GraduateApplicationId = c.Int(nullable: false),
                        RenewalYear = c.Int(nullable: false),
                        RenewalDate = c.DateTime(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        PaymentVoucherId = c.Int(),
                        PaymentDate = c.DateTime(),
                        ReceiptId = c.Int(),
                        Notes = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .ForeignKey("dbo.PaymentVoucher", t => t.PaymentVoucherId)
                .ForeignKey("dbo.Receipt", t => t.ReceiptId)
                .Index(t => t.GraduateApplicationId)
                .Index(t => t.PaymentVoucherId)
                .Index(t => t.ReceiptId);
            
            CreateTable(
                "dbo.PurchaseInvoiceItem",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PurchaseInvoiceId = c.Int(nullable: false),
                        ItemId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Item", t => t.ItemId)
                .ForeignKey("dbo.PurchaseInvoice", t => t.PurchaseInvoiceId)
                .Index(t => t.PurchaseInvoiceId)
                .Index(t => t.ItemId);
            
            CreateTable(
                "dbo.PurchaseInvoice",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SupplierInvoiceNumber = c.String(),
                        InvoiceDate = c.DateTime(nullable: false),
                        SupplierId = c.Int(nullable: false),
                        PaymentMethod = c.String(),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Notes = c.String(),
                        IsPosted = c.Boolean(nullable: false),
                        JournalEntryId = c.Int(),
                        CreatedByUserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Supplier", t => t.SupplierId)
                .Index(t => t.SupplierId);
            
            CreateTable(
                "dbo.Supplier",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        Phone = c.String(),
                        Address = c.String(),
                        AccountId = c.Int(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Account", t => t.AccountId)
                .Index(t => t.AccountId);
            
            CreateTable(
                "dbo.StampBookIssuances",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ContractorId = c.Int(nullable: false),
                        StampBookId = c.Int(nullable: false),
                        PaymentVoucherId = c.Int(nullable: false),
                        IssuanceDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.StampContractors", t => t.ContractorId)
                .ForeignKey("dbo.PaymentVoucher", t => t.PaymentVoucherId)
                .ForeignKey("dbo.StampBooks", t => t.StampBookId)
                .Index(t => t.ContractorId)
                .Index(t => t.StampBookId)
                .Index(t => t.PaymentVoucherId);
            
            CreateTable(
                "dbo.StampContractors",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                        Phone = c.String(),
                        NationalId = c.String(),
                        Governorate = c.String(),
                        Location = c.String(),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.StampBooks",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StartSerial = c.Long(nullable: false),
                        EndSerial = c.Long(nullable: false),
                        Quantity = c.Int(nullable: false),
                        ValuePerStamp = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DateAdded = c.DateTime(nullable: false),
                        CouncilDecisionRef = c.String(),
                        Status = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Stamps",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StampBookId = c.Int(nullable: false),
                        SerialNumber = c.Long(nullable: false),
                        Value = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Status = c.String(),
                        ContractorId = c.Int(),
                        SoldToLawyerId = c.Int(),
                        DateSold = c.DateTime(),
                        IsPaidToLawyer = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.SoldToLawyerId)
                .ForeignKey("dbo.StampBooks", t => t.StampBookId)
                .Index(t => t.StampBookId)
                .Index(t => t.SerialNumber, unique: true)
                .Index(t => t.SoldToLawyerId);
            
            CreateTable(
                "dbo.StampSale",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StampId = c.Int(nullable: false),
                        ContractorId = c.Int(nullable: false),
                        SaleDate = c.DateTime(nullable: false),
                        GraduateApplicationId = c.Int(),
                        LawyerMembershipId = c.String(nullable: false),
                        LawyerName = c.String(nullable: false),
                        LawyerBankName = c.String(maxLength: 100),
                        LawyerBankBranch = c.String(maxLength: 100),
                        LawyerAccountNumber = c.String(maxLength: 50),
                        LawyerIban = c.String(maxLength: 34),
                        StampValue = c.Decimal(nullable: false, precision: 18, scale: 2),
                        AmountToLawyer = c.Decimal(nullable: false, precision: 18, scale: 2),
                        AmountToBar = c.Decimal(nullable: false, precision: 18, scale: 2),
                        IsPaidToLawyer = c.Boolean(nullable: false),
                        BankSendDate = c.DateTime(),
                        IsOnHold = c.Boolean(nullable: false),
                        HoldReason = c.String(maxLength: 500),
                        RecordedByUserId = c.Int(nullable: false),
                        RecordedByUserName = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.StampContractors", t => t.ContractorId)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .ForeignKey("dbo.Stamps", t => t.StampId)
                .Index(t => t.StampId, unique: true, name: "IX_StampSale_StampId")
                .Index(t => t.ContractorId)
                .Index(t => t.GraduateApplicationId);
            
            CreateTable(
                "dbo.StockIssueItem",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StockIssueId = c.Int(nullable: false),
                        ItemId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        UnitCostSnapshot = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Item", t => t.ItemId)
                .ForeignKey("dbo.StockIssue", t => t.StockIssueId)
                .Index(t => t.StockIssueId)
                .Index(t => t.ItemId);
            
            CreateTable(
                "dbo.StockIssue",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        IssueDate = c.DateTime(nullable: false),
                        EmployeeId = c.Int(),
                        DepartmentName = c.String(),
                        Notes = c.String(),
                        IsPosted = c.Boolean(nullable: false),
                        JournalEntryId = c.Int(),
                        IssuedByUserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SupervisorChangeRequest",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TraineeId = c.Int(nullable: false),
                        RequestType = c.String(nullable: false),
                        OldSupervisorId = c.Int(),
                        NewSupervisorId = c.Int(),
                        RequestDate = c.DateTime(nullable: false),
                        Status = c.String(nullable: false),
                        CommitteeNotes = c.String(),
                        GracePeriodEndDate = c.DateTime(),
                        OldSupervisorApprovalPath = c.String(),
                        NewSupervisorApprovalPath = c.String(),
                        DecisionDate = c.DateTime(),
                        PaymentVoucherId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.NewSupervisorId)
                .ForeignKey("dbo.GraduateApplication", t => t.OldSupervisorId)
                .ForeignKey("dbo.PaymentVoucher", t => t.PaymentVoucherId)
                .ForeignKey("dbo.GraduateApplication", t => t.TraineeId)
                .Index(t => t.TraineeId)
                .Index(t => t.OldSupervisorId)
                .Index(t => t.NewSupervisorId)
                .Index(t => t.PaymentVoucherId);
            
            CreateTable(
                "dbo.SystemSetting",
                c => new
                    {
                        SettingKey = c.String(nullable: false, maxLength: 100),
                        SettingValue = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.SettingKey);
            
            CreateTable(
                "dbo.TraineeAttendance",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TraineeId = c.Int(nullable: false),
                        SessionId = c.Int(nullable: false),
                        AttendanceTime = c.DateTime(),
                        DurationInMinutes = c.Int(),
                        Status = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TrainingSession", t => t.SessionId)
                .ForeignKey("dbo.GraduateApplication", t => t.TraineeId)
                .Index(t => t.TraineeId)
                .Index(t => t.SessionId);
            
            CreateTable(
                "dbo.TrainingSession",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TrainingCourseId = c.Int(nullable: false),
                        SessionTitle = c.String(nullable: false, maxLength: 200),
                        InstructorName = c.String(maxLength: 150),
                        SessionDate = c.DateTime(nullable: false),
                        CreditHours = c.Double(nullable: false),
                        TeamsMeetingUrl = c.String(),
                        TeamsMeetingId = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.TrainingCourse", t => t.TrainingCourseId)
                .Index(t => t.TrainingCourseId);
            
            CreateTable(
                "dbo.TrainingCourse",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CourseName = c.String(nullable: false, maxLength: 200),
                        Description = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TraineeRenewal",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        TraineeId = c.Int(nullable: false),
                        RenewalYear = c.Int(nullable: false),
                        ReceiptId = c.Int(nullable: false),
                        RenewalDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Receipt", t => t.ReceiptId)
                .ForeignKey("dbo.GraduateApplication", t => t.TraineeId)
                .Index(t => t.TraineeId)
                .Index(t => t.ReceiptId);
            
            CreateTable(
                "dbo.TraineeSuspension",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GraduateApplicationId = c.Int(nullable: false),
                        Reason = c.String(nullable: false),
                        SuspensionStartDate = c.DateTime(nullable: false),
                        SuspensionEndDate = c.DateTime(),
                        DecisionDate = c.DateTime(nullable: false),
                        CreatedByUserId = c.Int(),
                        Status = c.String(nullable: false, maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.CreatedByUserId)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .Index(t => t.GraduateApplicationId)
                .Index(t => t.CreatedByUserId);
            
            CreateTable(
                "dbo.TrainingLog",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        GraduateApplicationId = c.Int(nullable: false),
                        SupervisorId = c.Int(),
                        Year = c.Int(nullable: false),
                        Month = c.Int(nullable: false),
                        WorkSummary = c.String(nullable: false),
                        FilePath = c.String(),
                        Status = c.String(nullable: false, maxLength: 50),
                        SubmissionDate = c.DateTime(nullable: false),
                        SupervisorNotes = c.String(),
                        ReviewDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.GraduateApplication", t => t.SupervisorId)
                .ForeignKey("dbo.GraduateApplication", t => t.GraduateApplicationId)
                .Index(t => t.GraduateApplicationId)
                .Index(t => t.SupervisorId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TrainingLog", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.TrainingLog", "SupervisorId", "dbo.GraduateApplication");
            DropForeignKey("dbo.TraineeSuspension", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.TraineeSuspension", "CreatedByUserId", "dbo.Users");
            DropForeignKey("dbo.TraineeRenewal", "TraineeId", "dbo.GraduateApplication");
            DropForeignKey("dbo.TraineeRenewal", "ReceiptId", "dbo.Receipt");
            DropForeignKey("dbo.TraineeAttendance", "TraineeId", "dbo.GraduateApplication");
            DropForeignKey("dbo.TraineeAttendance", "SessionId", "dbo.TrainingSession");
            DropForeignKey("dbo.TrainingSession", "TrainingCourseId", "dbo.TrainingCourse");
            DropForeignKey("dbo.SupervisorChangeRequest", "TraineeId", "dbo.GraduateApplication");
            DropForeignKey("dbo.SupervisorChangeRequest", "PaymentVoucherId", "dbo.PaymentVoucher");
            DropForeignKey("dbo.SupervisorChangeRequest", "OldSupervisorId", "dbo.GraduateApplication");
            DropForeignKey("dbo.SupervisorChangeRequest", "NewSupervisorId", "dbo.GraduateApplication");
            DropForeignKey("dbo.StockIssueItem", "StockIssueId", "dbo.StockIssue");
            DropForeignKey("dbo.StockIssueItem", "ItemId", "dbo.Item");
            DropForeignKey("dbo.StampSale", "StampId", "dbo.Stamps");
            DropForeignKey("dbo.StampSale", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.StampSale", "ContractorId", "dbo.StampContractors");
            DropForeignKey("dbo.StampBookIssuances", "StampBookId", "dbo.StampBooks");
            DropForeignKey("dbo.Stamps", "StampBookId", "dbo.StampBooks");
            DropForeignKey("dbo.Stamps", "SoldToLawyerId", "dbo.GraduateApplication");
            DropForeignKey("dbo.StampBookIssuances", "PaymentVoucherId", "dbo.PaymentVoucher");
            DropForeignKey("dbo.StampBookIssuances", "ContractorId", "dbo.StampContractors");
            DropForeignKey("dbo.PurchaseInvoice", "SupplierId", "dbo.Supplier");
            DropForeignKey("dbo.Supplier", "AccountId", "dbo.Account");
            DropForeignKey("dbo.PurchaseInvoiceItem", "PurchaseInvoiceId", "dbo.PurchaseInvoice");
            DropForeignKey("dbo.PurchaseInvoiceItem", "ItemId", "dbo.Item");
            DropForeignKey("dbo.PracticingLawyerRenewal", "ReceiptId", "dbo.Receipt");
            DropForeignKey("dbo.PracticingLawyerRenewal", "PaymentVoucherId", "dbo.PaymentVoucher");
            DropForeignKey("dbo.PracticingLawyerRenewal", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.OralExamCommitteeMember", "OralExamCommitteeId", "dbo.OralExamCommittee");
            DropForeignKey("dbo.OralExamEnrollment", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.OralExamEnrollment", "OralExamCommitteeId", "dbo.OralExamCommittee");
            DropForeignKey("dbo.OralExamCommitteeMember", "MemberLawyerId", "dbo.GraduateApplication");
            DropForeignKey("dbo.ManualGrade", "TraineeAnswerId", "dbo.TraineeAnswer");
            DropForeignKey("dbo.TraineeAnswer", "SelectedAnswerId", "dbo.Answer");
            DropForeignKey("dbo.TraineeAnswer", "QuestionId", "dbo.Question");
            DropForeignKey("dbo.TraineeAnswer", "ExamEnrollmentId", "dbo.ExamEnrollment");
            DropForeignKey("dbo.ManualGrade", "GraderId", "dbo.Users");
            DropForeignKey("dbo.LeaveRequest", "LeaveTypeId", "dbo.LeaveType");
            DropForeignKey("dbo.LeaveRequest", "EmployeeId", "dbo.Employee");
            DropForeignKey("dbo.LawyerFinancialAids", "LawyerId", "dbo.GraduateApplication");
            DropForeignKey("dbo.LawyerFinancialAids", "ExpenseId", "dbo.BarExpenses");
            DropForeignKey("dbo.LawyerFinancialAids", "CurrencyId", "dbo.Currency");
            DropForeignKey("dbo.LawyerFinancialAids", "AidTypeId", "dbo.SystemLookup");
            DropForeignKey("dbo.Item", "ItemCategoryId", "dbo.ItemCategory");
            DropForeignKey("dbo.InternalMessage", "SenderId", "dbo.Users");
            DropForeignKey("dbo.InternalMessage", "RecipientId", "dbo.Users");
            DropForeignKey("dbo.InternalMessage", "ParentMessageId", "dbo.InternalMessage");
            DropForeignKey("dbo.MessageAttachment", "InternalMessageId", "dbo.InternalMessage");
            DropForeignKey("dbo.GeneralExpense", "TreasuryAccountId", "dbo.Account");
            DropForeignKey("dbo.GeneralExpense", "ExpenseAccountId", "dbo.Account");
            DropForeignKey("dbo.GeneralExpense", "CostCenterId", "dbo.CostCenter");
            DropForeignKey("dbo.FeeDistributions", "ReceiptId", "dbo.Receipt");
            DropForeignKey("dbo.FeeDistributions", "LawyerId", "dbo.GraduateApplication");
            DropForeignKey("dbo.FeeDistributions", "ContractTransactionId", "dbo.ContractTransactions");
            DropForeignKey("dbo.ExchangeRate", "CurrencyId", "dbo.Currency");
            DropForeignKey("dbo.EmployeePayrollSlip", "MonthlyPayrollId", "dbo.MonthlyPayroll");
            DropForeignKey("dbo.PayrollSlip", "MonthlyPayrollId", "dbo.MonthlyPayroll");
            DropForeignKey("dbo.PayrollSlip", "EmployeeId", "dbo.Employee");
            DropForeignKey("dbo.MonthlyPayroll", "JournalEntryId", "dbo.JournalEntry");
            DropForeignKey("dbo.JournalEntryDetail", "JournalEntryId", "dbo.JournalEntry");
            DropForeignKey("dbo.JournalEntryDetail", "CurrencyId", "dbo.Currency");
            DropForeignKey("dbo.JournalEntryDetail", "CostCenterId", "dbo.CostCenter");
            DropForeignKey("dbo.JournalEntryDetail", "AccountId", "dbo.Account");
            DropForeignKey("dbo.JournalEntry", "CreatedByUserId", "dbo.Users");
            DropForeignKey("dbo.EmployeePayrollSlip", "EmployeeId", "dbo.Employee");
            DropForeignKey("dbo.EmployeeFinancialHistory", "EmployeeId", "dbo.Employee");
            DropForeignKey("dbo.Employee", "UserId", "dbo.Users");
            DropForeignKey("dbo.Employee", "JobTitleId", "dbo.JobTitle");
            DropForeignKey("dbo.Employee", "DepartmentId", "dbo.Department");
            DropForeignKey("dbo.DeferredFee", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.DeferredFee", "OathPaymentVoucherId", "dbo.PaymentVoucher");
            DropForeignKey("dbo.DeferredFee", "FeeTypeId", "dbo.FeeType");
            DropForeignKey("dbo.CostCenter", "ParentId", "dbo.CostCenter");
            DropForeignKey("dbo.ContractTransactions", "PaymentVoucherId", "dbo.PaymentVoucher");
            DropForeignKey("dbo.TransactionParties", "ProvinceId", "dbo.Provinces");
            DropForeignKey("dbo.TransactionParties", "PartyRoleId", "dbo.PartyRoles");
            DropForeignKey("dbo.TransactionParties", "ContractTransactionId", "dbo.ContractTransactions");
            DropForeignKey("dbo.PassportMinors", "MinorRelationshipId", "dbo.MinorRelationships");
            DropForeignKey("dbo.PassportMinors", "ContractTransactionId", "dbo.ContractTransactions");
            DropForeignKey("dbo.ContractTransactions", "LawyerId", "dbo.GraduateApplication");
            DropForeignKey("dbo.ContractTransactions", "ExemptionReasonId", "dbo.ContractExemptionReasons");
            DropForeignKey("dbo.ContractTransactions", "EmployeeId", "dbo.Users");
            DropForeignKey("dbo.ContractTransactions", "ContractTypeId", "dbo.ContractTypes");
            DropForeignKey("dbo.ContractTypes", "CurrencyId", "dbo.Currency");
            DropForeignKey("dbo.CheckPortfolio", "ReceiptId", "dbo.Receipt");
            DropForeignKey("dbo.CheckPortfolio", "CurrencyId", "dbo.Currency");
            DropForeignKey("dbo.CaseDocuments", "CommitteeCaseId", "dbo.CommitteeCases");
            DropForeignKey("dbo.CaseSessions", "CommitteeCaseId", "dbo.CommitteeCases");
            DropForeignKey("dbo.CommitteeCases", "CommitteeId", "dbo.Committees");
            DropForeignKey("dbo.CommitteePanelMembers", "CommitteeId", "dbo.Committees");
            DropForeignKey("dbo.CommitteeMeetings", "CommitteeId", "dbo.Committees");
            DropForeignKey("dbo.BarExpenses", "BankAccountId", "dbo.BankAccount");
            DropForeignKey("dbo.Question", "QuestionTypeId", "dbo.QuestionType");
            DropForeignKey("dbo.Exam", "RequiredApplicationStatusId", "dbo.ApplicationStatus");
            DropForeignKey("dbo.Question", "ExamId", "dbo.Exam");
            DropForeignKey("dbo.Exam", "ExamTypeId", "dbo.ExamType");
            DropForeignKey("dbo.ExamEnrollment", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.GraduateApplication", "WalletProviderId", "dbo.SystemLookup");
            DropForeignKey("dbo.GraduateApplication", "UserId", "dbo.Users");
            DropForeignKey("dbo.Users", "UserTypeId", "dbo.UserTypes");
            DropForeignKey("dbo.Permissions", "UserTypeId", "dbo.UserTypes");
            DropForeignKey("dbo.Permissions", "ModuleId", "dbo.Modules");
            DropForeignKey("dbo.AuditLogs", "UserId", "dbo.Users");
            DropForeignKey("dbo.SupervisorHistory", "OldSupervisorId", "dbo.GraduateApplication");
            DropForeignKey("dbo.SupervisorHistory", "NewSupervisorId", "dbo.GraduateApplication");
            DropForeignKey("dbo.SupervisorHistory", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.GraduateApplication", "SupervisorId", "dbo.GraduateApplication");
            DropForeignKey("dbo.Qualification", "QualificationTypeId", "dbo.QualificationType");
            DropForeignKey("dbo.Qualification", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.OathRequest", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.OathRequest", "PaymentVoucherId", "dbo.PaymentVoucher");
            DropForeignKey("dbo.GraduateApplication", "OathCeremonyId", "dbo.OathCeremony");
            DropForeignKey("dbo.GraduateApplication", "NationalIdTypeId", "dbo.NationalIdType");
            DropForeignKey("dbo.LoanApplications", "LoanTypeId", "dbo.LoanTypes");
            DropForeignKey("dbo.LoanTypes", "BankAccountForRepaymentId", "dbo.BankAccount");
            DropForeignKey("dbo.LoanApplications", "LawyerId", "dbo.GraduateApplication");
            DropForeignKey("dbo.LoanInstallments", "ReceiptId", "dbo.Receipt");
            DropForeignKey("dbo.LoanInstallments", "PaymentVoucherId", "dbo.PaymentVoucher");
            DropForeignKey("dbo.VoucherDetail", "PaymentVoucherId", "dbo.PaymentVoucher");
            DropForeignKey("dbo.VoucherDetail", "FeeTypeId", "dbo.FeeType");
            DropForeignKey("dbo.FeeType", "RevenueAccountId", "dbo.Account");
            DropForeignKey("dbo.FeeType", "CurrencyId", "dbo.Currency");
            DropForeignKey("dbo.FeeType", "BankAccountId", "dbo.BankAccount");
            DropForeignKey("dbo.VoucherDetail", "BankAccountId", "dbo.BankAccount");
            DropForeignKey("dbo.BankAccount", "RelatedAccountId", "dbo.Account");
            DropForeignKey("dbo.BankAccount", "CurrencyId", "dbo.Currency");
            DropForeignKey("dbo.Receipt", "Id", "dbo.PaymentVoucher");
            DropForeignKey("dbo.PaymentVoucher", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.LoanInstallments", "LoanApplicationId", "dbo.LoanApplications");
            DropForeignKey("dbo.Guarantors", "LoanApplicationId", "dbo.LoanApplications");
            DropForeignKey("dbo.Guarantors", "LawyerGuarantorId", "dbo.GraduateApplication");
            DropForeignKey("dbo.LegalResearch", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.CommitteeDecision", "LegalResearchId", "dbo.LegalResearch");
            DropForeignKey("dbo.LegalResearch", "DiscussionCommitteeId", "dbo.DiscussionCommittee");
            DropForeignKey("dbo.CommitteeMember", "MemberLawyerId", "dbo.GraduateApplication");
            DropForeignKey("dbo.CommitteeMember", "DiscussionCommitteeId", "dbo.DiscussionCommittee");
            DropForeignKey("dbo.LawyerSpouses", "LawyerId", "dbo.LawyerPersonalData");
            DropForeignKey("dbo.OfficePartners", "LawyerId", "dbo.LawyerOffice");
            DropForeignKey("dbo.LawyerOffice", "LawyerId", "dbo.LawyerPersonalData");
            DropForeignKey("dbo.LawyerPersonalData", "LawyerId", "dbo.GraduateApplication");
            DropForeignKey("dbo.SecurityHealthRecord", "LawyerId", "dbo.LawyerPersonalData");
            DropForeignKey("dbo.InjuryRecords", "LawyerId", "dbo.SecurityHealthRecord");
            DropForeignKey("dbo.LawyerChildren", "LawyerId", "dbo.LawyerPersonalData");
            DropForeignKey("dbo.GraduateApplication", "GenderId", "dbo.Gender");
            DropForeignKey("dbo.GraduateApplication", "ExamApplicationId", "dbo.ExamApplication");
            DropForeignKey("dbo.ContactInfo", "Id", "dbo.GraduateApplication");
            DropForeignKey("dbo.Attachment", "GraduateApplicationId", "dbo.GraduateApplication");
            DropForeignKey("dbo.Attachment", "AttachmentTypeId", "dbo.AttachmentType");
            DropForeignKey("dbo.GraduateApplication", "ApplicationStatusId", "dbo.ApplicationStatus");
            DropForeignKey("dbo.ExamEnrollment", "ExamApplicationId", "dbo.ExamApplication");
            DropForeignKey("dbo.ExamQualification", "ExamApplicationId", "dbo.ExamApplication");
            DropForeignKey("dbo.ExamApplication", "GenderId", "dbo.Gender");
            DropForeignKey("dbo.ExamEnrollment", "ExamId", "dbo.Exam");
            DropForeignKey("dbo.Answer", "QuestionId", "dbo.Question");
            DropForeignKey("dbo.AgendaAttachments", "AgendaItemId", "dbo.AgendaItems");
            DropForeignKey("dbo.AgendaItems", "CouncilSessionId", "dbo.CouncilSessions");
            DropForeignKey("dbo.SessionAttendances", "CouncilSessionId", "dbo.CouncilSessions");
            DropIndex("dbo.TrainingLog", new[] { "SupervisorId" });
            DropIndex("dbo.TrainingLog", new[] { "GraduateApplicationId" });
            DropIndex("dbo.TraineeSuspension", new[] { "CreatedByUserId" });
            DropIndex("dbo.TraineeSuspension", new[] { "GraduateApplicationId" });
            DropIndex("dbo.TraineeRenewal", new[] { "ReceiptId" });
            DropIndex("dbo.TraineeRenewal", new[] { "TraineeId" });
            DropIndex("dbo.TrainingSession", new[] { "TrainingCourseId" });
            DropIndex("dbo.TraineeAttendance", new[] { "SessionId" });
            DropIndex("dbo.TraineeAttendance", new[] { "TraineeId" });
            DropIndex("dbo.SupervisorChangeRequest", new[] { "PaymentVoucherId" });
            DropIndex("dbo.SupervisorChangeRequest", new[] { "NewSupervisorId" });
            DropIndex("dbo.SupervisorChangeRequest", new[] { "OldSupervisorId" });
            DropIndex("dbo.SupervisorChangeRequest", new[] { "TraineeId" });
            DropIndex("dbo.StockIssueItem", new[] { "ItemId" });
            DropIndex("dbo.StockIssueItem", new[] { "StockIssueId" });
            DropIndex("dbo.StampSale", new[] { "GraduateApplicationId" });
            DropIndex("dbo.StampSale", new[] { "ContractorId" });
            DropIndex("dbo.StampSale", "IX_StampSale_StampId");
            DropIndex("dbo.Stamps", new[] { "SoldToLawyerId" });
            DropIndex("dbo.Stamps", new[] { "SerialNumber" });
            DropIndex("dbo.Stamps", new[] { "StampBookId" });
            DropIndex("dbo.StampBookIssuances", new[] { "PaymentVoucherId" });
            DropIndex("dbo.StampBookIssuances", new[] { "StampBookId" });
            DropIndex("dbo.StampBookIssuances", new[] { "ContractorId" });
            DropIndex("dbo.Supplier", new[] { "AccountId" });
            DropIndex("dbo.PurchaseInvoice", new[] { "SupplierId" });
            DropIndex("dbo.PurchaseInvoiceItem", new[] { "ItemId" });
            DropIndex("dbo.PurchaseInvoiceItem", new[] { "PurchaseInvoiceId" });
            DropIndex("dbo.PracticingLawyerRenewal", new[] { "ReceiptId" });
            DropIndex("dbo.PracticingLawyerRenewal", new[] { "PaymentVoucherId" });
            DropIndex("dbo.PracticingLawyerRenewal", new[] { "GraduateApplicationId" });
            DropIndex("dbo.OralExamEnrollment", new[] { "OralExamCommitteeId" });
            DropIndex("dbo.OralExamEnrollment", new[] { "GraduateApplicationId" });
            DropIndex("dbo.OralExamCommitteeMember", new[] { "MemberLawyerId" });
            DropIndex("dbo.OralExamCommitteeMember", new[] { "OralExamCommitteeId" });
            DropIndex("dbo.TraineeAnswer", new[] { "SelectedAnswerId" });
            DropIndex("dbo.TraineeAnswer", new[] { "QuestionId" });
            DropIndex("dbo.TraineeAnswer", new[] { "ExamEnrollmentId" });
            DropIndex("dbo.ManualGrade", new[] { "GraderId" });
            DropIndex("dbo.ManualGrade", new[] { "TraineeAnswerId" });
            DropIndex("dbo.LeaveRequest", new[] { "LeaveTypeId" });
            DropIndex("dbo.LeaveRequest", new[] { "EmployeeId" });
            DropIndex("dbo.LawyerFinancialAids", new[] { "ExpenseId" });
            DropIndex("dbo.LawyerFinancialAids", new[] { "CurrencyId" });
            DropIndex("dbo.LawyerFinancialAids", new[] { "AidTypeId" });
            DropIndex("dbo.LawyerFinancialAids", new[] { "LawyerId" });
            DropIndex("dbo.Item", new[] { "ItemCategoryId" });
            DropIndex("dbo.MessageAttachment", new[] { "InternalMessageId" });
            DropIndex("dbo.InternalMessage", new[] { "RecipientId" });
            DropIndex("dbo.InternalMessage", new[] { "SenderId" });
            DropIndex("dbo.InternalMessage", new[] { "ParentMessageId" });
            DropIndex("dbo.GeneralExpense", new[] { "TreasuryAccountId" });
            DropIndex("dbo.GeneralExpense", new[] { "CostCenterId" });
            DropIndex("dbo.GeneralExpense", new[] { "ExpenseAccountId" });
            DropIndex("dbo.FeeDistributions", new[] { "LawyerId" });
            DropIndex("dbo.FeeDistributions", new[] { "ContractTransactionId" });
            DropIndex("dbo.FeeDistributions", new[] { "ReceiptId" });
            DropIndex("dbo.ExchangeRate", new[] { "CurrencyId" });
            DropIndex("dbo.PayrollSlip", new[] { "EmployeeId" });
            DropIndex("dbo.PayrollSlip", new[] { "MonthlyPayrollId" });
            DropIndex("dbo.JournalEntryDetail", new[] { "CurrencyId" });
            DropIndex("dbo.JournalEntryDetail", new[] { "CostCenterId" });
            DropIndex("dbo.JournalEntryDetail", new[] { "AccountId" });
            DropIndex("dbo.JournalEntryDetail", new[] { "JournalEntryId" });
            DropIndex("dbo.JournalEntry", new[] { "CreatedByUserId" });
            DropIndex("dbo.MonthlyPayroll", new[] { "JournalEntryId" });
            DropIndex("dbo.EmployeePayrollSlip", new[] { "EmployeeId" });
            DropIndex("dbo.EmployeePayrollSlip", new[] { "MonthlyPayrollId" });
            DropIndex("dbo.Employee", new[] { "UserId" });
            DropIndex("dbo.Employee", new[] { "JobTitleId" });
            DropIndex("dbo.Employee", new[] { "DepartmentId" });
            DropIndex("dbo.EmployeeFinancialHistory", new[] { "EmployeeId" });
            DropIndex("dbo.DeferredFee", new[] { "OathPaymentVoucherId" });
            DropIndex("dbo.DeferredFee", new[] { "FeeTypeId" });
            DropIndex("dbo.DeferredFee", new[] { "GraduateApplicationId" });
            DropIndex("dbo.CostCenter", new[] { "ParentId" });
            DropIndex("dbo.TransactionParties", new[] { "PartyRoleId" });
            DropIndex("dbo.TransactionParties", new[] { "ProvinceId" });
            DropIndex("dbo.TransactionParties", new[] { "ContractTransactionId" });
            DropIndex("dbo.PassportMinors", new[] { "MinorRelationshipId" });
            DropIndex("dbo.PassportMinors", new[] { "ContractTransactionId" });
            DropIndex("dbo.ContractTypes", new[] { "CurrencyId" });
            DropIndex("dbo.ContractTransactions", new[] { "PaymentVoucherId" });
            DropIndex("dbo.ContractTransactions", new[] { "EmployeeId" });
            DropIndex("dbo.ContractTransactions", new[] { "ExemptionReasonId" });
            DropIndex("dbo.ContractTransactions", new[] { "ContractTypeId" });
            DropIndex("dbo.ContractTransactions", new[] { "LawyerId" });
            DropIndex("dbo.CheckPortfolio", new[] { "ReceiptId" });
            DropIndex("dbo.CheckPortfolio", new[] { "CurrencyId" });
            DropIndex("dbo.CaseSessions", new[] { "CommitteeCaseId" });
            DropIndex("dbo.CommitteePanelMembers", new[] { "CommitteeId" });
            DropIndex("dbo.CommitteeMeetings", new[] { "CommitteeId" });
            DropIndex("dbo.CommitteeCases", new[] { "CommitteeId" });
            DropIndex("dbo.CaseDocuments", new[] { "CommitteeCaseId" });
            DropIndex("dbo.BarExpenses", new[] { "BankAccountId" });
            DropIndex("dbo.Permissions", new[] { "ModuleId" });
            DropIndex("dbo.Permissions", new[] { "UserTypeId" });
            DropIndex("dbo.AuditLogs", new[] { "UserId" });
            DropIndex("dbo.Users", new[] { "UserTypeId" });
            DropIndex("dbo.Users", new[] { "Email" });
            DropIndex("dbo.Users", new[] { "Username" });
            DropIndex("dbo.SupervisorHistory", new[] { "NewSupervisorId" });
            DropIndex("dbo.SupervisorHistory", new[] { "OldSupervisorId" });
            DropIndex("dbo.SupervisorHistory", new[] { "GraduateApplicationId" });
            DropIndex("dbo.Qualification", new[] { "GraduateApplicationId" });
            DropIndex("dbo.Qualification", new[] { "QualificationTypeId" });
            DropIndex("dbo.OathRequest", new[] { "PaymentVoucherId" });
            DropIndex("dbo.OathRequest", new[] { "GraduateApplicationId" });
            DropIndex("dbo.LoanTypes", new[] { "BankAccountForRepaymentId" });
            DropIndex("dbo.FeeType", new[] { "RevenueAccountId" });
            DropIndex("dbo.FeeType", new[] { "BankAccountId" });
            DropIndex("dbo.FeeType", new[] { "CurrencyId" });
            DropIndex("dbo.BankAccount", new[] { "RelatedAccountId" });
            DropIndex("dbo.BankAccount", new[] { "CurrencyId" });
            DropIndex("dbo.VoucherDetail", new[] { "BankAccountId" });
            DropIndex("dbo.VoucherDetail", new[] { "FeeTypeId" });
            DropIndex("dbo.VoucherDetail", new[] { "PaymentVoucherId" });
            DropIndex("dbo.Receipt", "IX_Receipt_Year_Sequence");
            DropIndex("dbo.Receipt", new[] { "Id" });
            DropIndex("dbo.PaymentVoucher", new[] { "GraduateApplicationId" });
            DropIndex("dbo.LoanInstallments", new[] { "ReceiptId" });
            DropIndex("dbo.LoanInstallments", new[] { "PaymentVoucherId" });
            DropIndex("dbo.LoanInstallments", new[] { "LoanApplicationId" });
            DropIndex("dbo.Guarantors", new[] { "LawyerGuarantorId" });
            DropIndex("dbo.Guarantors", new[] { "LoanApplicationId" });
            DropIndex("dbo.LoanApplications", new[] { "LoanTypeId" });
            DropIndex("dbo.LoanApplications", new[] { "LawyerId" });
            DropIndex("dbo.CommitteeDecision", new[] { "LegalResearchId" });
            DropIndex("dbo.CommitteeMember", new[] { "MemberLawyerId" });
            DropIndex("dbo.CommitteeMember", new[] { "DiscussionCommitteeId" });
            DropIndex("dbo.LegalResearch", new[] { "DiscussionCommitteeId" });
            DropIndex("dbo.LegalResearch", new[] { "GraduateApplicationId" });
            DropIndex("dbo.LawyerSpouses", new[] { "LawyerId" });
            DropIndex("dbo.OfficePartners", new[] { "LawyerId" });
            DropIndex("dbo.LawyerOffice", new[] { "LawyerId" });
            DropIndex("dbo.InjuryRecords", new[] { "LawyerId" });
            DropIndex("dbo.SecurityHealthRecord", new[] { "LawyerId" });
            DropIndex("dbo.LawyerChildren", new[] { "LawyerId" });
            DropIndex("dbo.LawyerPersonalData", new[] { "LawyerId" });
            DropIndex("dbo.ContactInfo", new[] { "Id" });
            DropIndex("dbo.Attachment", new[] { "AttachmentTypeId" });
            DropIndex("dbo.Attachment", new[] { "GraduateApplicationId" });
            DropIndex("dbo.GraduateApplication", new[] { "WalletProviderId" });
            DropIndex("dbo.GraduateApplication", new[] { "ExamApplicationId" });
            DropIndex("dbo.GraduateApplication", new[] { "OathCeremonyId" });
            DropIndex("dbo.GraduateApplication", new[] { "MembershipId" });
            DropIndex("dbo.GraduateApplication", new[] { "UserId" });
            DropIndex("dbo.GraduateApplication", new[] { "SupervisorId" });
            DropIndex("dbo.GraduateApplication", new[] { "ApplicationStatusId" });
            DropIndex("dbo.GraduateApplication", new[] { "GenderId" });
            DropIndex("dbo.GraduateApplication", new[] { "NationalIdTypeId" });
            DropIndex("dbo.ExamQualification", new[] { "ExamApplicationId" });
            DropIndex("dbo.ExamApplication", new[] { "GenderId" });
            DropIndex("dbo.ExamApplication", new[] { "NationalIdNumber" });
            DropIndex("dbo.ExamEnrollment", new[] { "GraduateApplicationId" });
            DropIndex("dbo.ExamEnrollment", new[] { "ExamApplicationId" });
            DropIndex("dbo.ExamEnrollment", new[] { "ExamId" });
            DropIndex("dbo.Exam", new[] { "RequiredApplicationStatusId" });
            DropIndex("dbo.Exam", new[] { "ExamTypeId" });
            DropIndex("dbo.Question", new[] { "QuestionTypeId" });
            DropIndex("dbo.Question", new[] { "ExamId" });
            DropIndex("dbo.Answer", new[] { "QuestionId" });
            DropIndex("dbo.SessionAttendances", new[] { "CouncilSessionId" });
            DropIndex("dbo.AgendaItems", new[] { "CouncilSessionId" });
            DropIndex("dbo.AgendaAttachments", new[] { "AgendaItemId" });
            DropTable("dbo.TrainingLog");
            DropTable("dbo.TraineeSuspension");
            DropTable("dbo.TraineeRenewal");
            DropTable("dbo.TrainingCourse");
            DropTable("dbo.TrainingSession");
            DropTable("dbo.TraineeAttendance");
            DropTable("dbo.SystemSetting");
            DropTable("dbo.SupervisorChangeRequest");
            DropTable("dbo.StockIssue");
            DropTable("dbo.StockIssueItem");
            DropTable("dbo.StampSale");
            DropTable("dbo.Stamps");
            DropTable("dbo.StampBooks");
            DropTable("dbo.StampContractors");
            DropTable("dbo.StampBookIssuances");
            DropTable("dbo.Supplier");
            DropTable("dbo.PurchaseInvoice");
            DropTable("dbo.PurchaseInvoiceItem");
            DropTable("dbo.PracticingLawyerRenewal");
            DropTable("dbo.OralExamEnrollment");
            DropTable("dbo.OralExamCommittee");
            DropTable("dbo.OralExamCommitteeMember");
            DropTable("dbo.TraineeAnswer");
            DropTable("dbo.ManualGrade");
            DropTable("dbo.LeaveType");
            DropTable("dbo.LeaveRequest");
            DropTable("dbo.LawyerFinancialAids");
            DropTable("dbo.Item");
            DropTable("dbo.ItemCategory");
            DropTable("dbo.MessageAttachment");
            DropTable("dbo.InternalMessage");
            DropTable("dbo.GeneralExpense");
            DropTable("dbo.FiscalYear");
            DropTable("dbo.FinancialAidTypes");
            DropTable("dbo.FeeDistributions");
            DropTable("dbo.ExchangeRate");
            DropTable("dbo.PayrollSlip");
            DropTable("dbo.JournalEntryDetail");
            DropTable("dbo.JournalEntry");
            DropTable("dbo.MonthlyPayroll");
            DropTable("dbo.EmployeePayrollSlip");
            DropTable("dbo.JobTitle");
            DropTable("dbo.Employee");
            DropTable("dbo.EmployeeFinancialHistory");
            DropTable("dbo.Department");
            DropTable("dbo.DeferredFee");
            DropTable("dbo.CouncilMember");
            DropTable("dbo.CostCenter");
            DropTable("dbo.Provinces");
            DropTable("dbo.PartyRoles");
            DropTable("dbo.TransactionParties");
            DropTable("dbo.MinorRelationships");
            DropTable("dbo.PassportMinors");
            DropTable("dbo.ContractTypes");
            DropTable("dbo.ContractTransactions");
            DropTable("dbo.ContractExemptionReasons");
            DropTable("dbo.CheckPortfolio");
            DropTable("dbo.CaseSessions");
            DropTable("dbo.CommitteePanelMembers");
            DropTable("dbo.CommitteeMeetings");
            DropTable("dbo.Committees");
            DropTable("dbo.CommitteeCases");
            DropTable("dbo.CaseDocuments");
            DropTable("dbo.BarExpenses");
            DropTable("dbo.QuestionType");
            DropTable("dbo.ExamType");
            DropTable("dbo.SystemLookup");
            DropTable("dbo.Modules");
            DropTable("dbo.Permissions");
            DropTable("dbo.UserTypes");
            DropTable("dbo.AuditLogs");
            DropTable("dbo.Users");
            DropTable("dbo.SupervisorHistory");
            DropTable("dbo.QualificationType");
            DropTable("dbo.Qualification");
            DropTable("dbo.OathRequest");
            DropTable("dbo.OathCeremony");
            DropTable("dbo.NationalIdType");
            DropTable("dbo.LoanTypes");
            DropTable("dbo.FeeType");
            DropTable("dbo.Currency");
            DropTable("dbo.BankAccount");
            DropTable("dbo.VoucherDetail");
            DropTable("dbo.Receipt");
            DropTable("dbo.PaymentVoucher");
            DropTable("dbo.LoanInstallments");
            DropTable("dbo.Guarantors");
            DropTable("dbo.LoanApplications");
            DropTable("dbo.CommitteeDecision");
            DropTable("dbo.CommitteeMember");
            DropTable("dbo.DiscussionCommittee");
            DropTable("dbo.LegalResearch");
            DropTable("dbo.LawyerSpouses");
            DropTable("dbo.OfficePartners");
            DropTable("dbo.LawyerOffice");
            DropTable("dbo.InjuryRecords");
            DropTable("dbo.SecurityHealthRecord");
            DropTable("dbo.LawyerChildren");
            DropTable("dbo.LawyerPersonalData");
            DropTable("dbo.ContactInfo");
            DropTable("dbo.AttachmentType");
            DropTable("dbo.Attachment");
            DropTable("dbo.ApplicationStatus");
            DropTable("dbo.GraduateApplication");
            DropTable("dbo.ExamQualification");
            DropTable("dbo.Gender");
            DropTable("dbo.ExamApplication");
            DropTable("dbo.ExamEnrollment");
            DropTable("dbo.Exam");
            DropTable("dbo.Question");
            DropTable("dbo.Answer");
            DropTable("dbo.SessionAttendances");
            DropTable("dbo.CouncilSessions");
            DropTable("dbo.AgendaItems");
            DropTable("dbo.AgendaAttachments");
            DropTable("dbo.Account");
        }
    }
}
