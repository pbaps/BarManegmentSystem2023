using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;

namespace BarManegment.Models
{
    // ==========================================
    // أولاً: قسم اللجان (Committees)
    // ==========================================

    [Table("Committees")]
    public class Committee
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم اللجنة مطلوب")]
        [Display(Name = "اسم اللجنة")]
        public string Name { get; set; } // مثال: لجنة الشكاوى

        [Display(Name = "وصف اللجنة")]
        public string Description { get; set; }

        [Display(Name = "فعال")]
        public bool IsActive { get; set; }
        public virtual ICollection<CommitteePanelMember> PanelMembers { get; set; }
        // العلاقات
 
        public virtual ICollection<CommitteeMeeting> Meetings { get; set; }
        public virtual ICollection<CommitteeCase> Cases { get; set; }
    }

    // تم تغيير الاسم هنا من CommitteeMember إلى CommitteePanelMember
    [Table("CommitteePanelMembers")]
    public class CommitteePanelMember
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Committee")]
        public int CommitteeId { get; set; }

        [Display(Name = "المحامي")]
        public int? LawyerId { get; set; }

        [Display(Name = "الموظف")]
        public string EmployeeUserId { get; set; }

        [Required]
        [Display(Name = "المنصب")]
        public string Role { get; set; } // رئيس، مقرر، سكرتير، عضو

        [Display(Name = "تاريخ الانضمام")]
        public DateTime JoinDate { get; set; }

        public bool IsActive { get; set; }

        public virtual Committee Committee { get; set; }
    }

    [Table("CommitteeMeetings")]
    public class CommitteeMeeting
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Committee")]
        public int CommitteeId { get; set; }

        [Display(Name = "تاريخ الجلسة")]
        public DateTime MeetingDate { get; set; }

        [Display(Name = "مكان الانعقاد")]
        public string Location { get; set; }

        [Display(Name = "نص المحضر")]
        public string MinutesText { get; set; }

        [Display(Name = "ملف المحضر")]
        public string MinutesFilePath { get; set; } // PDF path

        [Display(Name = "هل انتهت الجلسة؟")]
        public bool IsCompleted { get; set; } // False = مؤجلة/تستكمل

        public virtual Committee Committee { get; set; }
    }

    [Table("CommitteeCases")]
    public class CommitteeCase
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Committee")]
        public int CommitteeId { get; set; }

        [Display(Name = "رقم الملف/القضية")]
        public string CaseNumber { get; set; }

        [Required]
        [Display(Name = "الموضوع")]
        public string Subject { get; set; }

        [Display(Name = "جهة الورود")]
        public string SourceType { get; set; } // شكوى مواطن، قرار مجلس، طلب داخلي

        [Display(Name = "اسم المشتكي/مقدم الطلب")]
        public string ComplainantName { get; set; }

        [Display(Name = "المحامي المعني")]
        public int? TargetLawyerId { get; set; }

        [Display(Name = "الحالة")]
        public string Status { get; set; } // جديد، قيد الدراسة، تم البت

        [Display(Name = "توصية اللجنة")]
        public string FinalRecommendation { get; set; }

        [Display(Name = "ملاحظات قرار المجلس")]
        public string CouncilDecisionNotes { get; set; }

        public DateTime CreatedDate { get; set; }

        public virtual Committee Committee { get; set; }
        // العلاقات الجديدة
        public virtual ICollection<CaseSession> Sessions { get; set; }
        public virtual ICollection<CaseDocument> Documents { get; set; }


    }
    // 1. جدول جلسات التحقيق/المناقشة الخاصة بالملف
    [Table("CaseSessions")]
    public class CaseSession
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("CommitteeCase")]
        public int CommitteeCaseId { get; set; }

        [Display(Name = "تاريخ الجلسة")]
        public DateTime SessionDate { get; set; }

        [Display(Name = "عنوان/غرض الجلسة")]
        public string Title { get; set; } // مثلاً: سماع أقوال، مداولة، نطق بالقرار

        [Display(Name = "محضر الجلسة")]
        public string Minutes { get; set; } // تفاصيل ما دار في الجلسة

        [Display(Name = "القرار المرحلي")]
        public string InterimDecision { get; set; } // تأجيل، حجز للحكم..

        [Display(Name = "منعقدة")]
        public bool IsCompleted { get; set; }

        public virtual CommitteeCase CommitteeCase { get; set; }
    }

    // 2. جدول المرفقات والمستندات
    [Table("CaseDocuments")]
    public class CaseDocument
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("CommitteeCase")]
        public int CommitteeCaseId { get; set; }

        [Display(Name = "وصف المستند")]
        public string Description { get; set; }

        [Display(Name = "مسار الملف")]
        public string FilePath { get; set; }

        [Display(Name = "تاريخ الرفع")]
        public DateTime UploadDate { get; set; }

        public virtual CommitteeCase CommitteeCase { get; set; }
    }
    // ==========================================
    // ثانياً: قسم مجلس النقابة (Council)
    // ==========================================

    [Table("CouncilSessions")]
    public class CouncilSession
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "رقم الجلسة")]
        public int SessionNumber { get; set; }

        [Display(Name = "العام")]
        public int Year { get; set; }

        [Display(Name = "تاريخ الانعقاد")]
        public DateTime SessionDate { get; set; }

        [Display(Name = "مكان الانعقاد")]
        public string Location { get; set; }

        [Display(Name = "ملف المحضر الموقع")]
        public string SignedMinutesPath { get; set; }

        [Display(Name = "تم الإغلاق")]
        public bool IsFinalized { get; set; }

        public virtual ICollection<SessionAttendance> Attendees { get; set; }
        public virtual ICollection<AgendaItem> AgendaItems { get; set; }
    }

    [Table("SessionAttendances")]
    public class SessionAttendance
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("CouncilSession")]
        public int CouncilSessionId { get; set; }

        [Display(Name = "اسم العضو")]
        public string MemberName { get; set; } // أو ربط مع جدول الأعضاء

        [Display(Name = "حاضر")]
        public bool IsPresent { get; set; }

        [Display(Name = "ملاحظات")]
        public string Notes { get; set; } // تأخر، غياب بعذر

        public virtual CouncilSession CouncilSession { get; set; }
    }

    [Table("AgendaItems")]
    public class AgendaItem
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("CouncilSession")]
        public int? CouncilSessionId { get; set; } // Nullable: الطلب قد يكون قيد الدراسة ولم يدرج في جلسة بعد

        // البيانات الأساسية
        [Display(Name = "نوع الطلب")]
        public string RequestType { get; set; } // مالي، لجان، عام...

        [Display(Name = "المصدر")]
        public string Source { get; set; } // بوابة، يدوي، لجنة

        [Display(Name = "العنوان")]
        public string Title { get; set; }

        [Display(Name = "التفاصيل")]
        public string Description { get; set; }

        // الارتباطات
        public int? RequesterLawyerId { get; set; } // للمحامين والمتدربين
        public string CreatedByUserId { get; set; } // الموظف مدخل البيانات

        // مرحلة التنسيق والدراسة
        [Display(Name = "موافقة للعرض")]
        public bool IsApprovedForAgenda { get; set; }

        [Display(Name = "الموظف المختص للدراسة")]
        public string AssignedEmployeeId { get; set; } // تحويل للمحاسب أو غيره

        [Display(Name = "ملاحظات الدراسة")]
        public string EmployeeStudyNotes { get; set; }

        // القرار
        [Display(Name = "قرار المجلس")]
        public string CouncilDecisionType { get; set; } // موافقة، رفض، تأجيل

        [Display(Name = "نص القرار")]
        public string DecisionText { get; set; }

        [Display(Name = "ملف القرار")]
        public string DecisionFilePath { get; set; }

        // النشر
        [Display(Name = "عرض للمحامي")]
        public bool IsVisibleToRequester { get; set; }

        // (جديد) حالة التنفيذ (بانتظار التعيين، بانتظار التنفيذ، تم التنفيذ)
        [Display(Name = "حالة التنفيذ")]
        public string ExecutionStatus { get; set; }
        // ... (داخل كلاس AgendaItem)

        [Display(Name = "ملاحظات الموظف المنفذ")]
        [AllowHtml] // للسماح بكتابة نصوص طويلة
        public string EmployeeExecutionNotes { get; set; }

        // (جديد) الموظف المكلف بالتنفيذ
        [Display(Name = "مكلف بالتنفيذ")]
        public string AssignedForExecutionUserId { get; set; } // نستخدم string لاسم المستخدم

        public virtual CouncilSession CouncilSession { get; set; }
        public virtual ICollection<AgendaAttachment> Attachments { get; set; }
    }

    [Table("AgendaAttachments")]
    public class AgendaAttachment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("AgendaItem")]
        public int AgendaItemId { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string UploadedBy { get; set; }

        public virtual AgendaItem AgendaItem { get; set; }
    }
}