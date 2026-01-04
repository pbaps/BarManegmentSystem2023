using System;
using System.Collections.Generic;
using BarManegment.Models;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class CommitteeMemberDashboardViewModel
    {
        public string LawyerName { get; set; }
        public List<OralExamCommittee> OralCommittees { get; set; }
        public List<DiscussionCommittee> ResearchCommittees { get; set; }
    }

    public class MemberOralGradingViewModel
    {
        public int CommitteeId { get; set; }
        public string CommitteeName { get; set; }
        public DateTime ExamDate { get; set; }
        public List<TraineeGradeItem> Trainees { get; set; }
    }

    public class TraineeGradeItem
    {
        public int EnrollmentId { get; set; }
        public string TraineeName { get; set; }
        public string TraineeNumber { get; set; }
        public string CurrentResult { get; set; }
        public double? MemberScore { get; set; } // الدرجة التي يضعها العضو
        public string MemberNotes { get; set; }
    }

    public class MemberResearchEvaluationViewModel
    {
        public int CommitteeId { get; set; }
        public string CommitteeName { get; set; }
        public List<ResearchEvaluationItem> Researches { get; set; }
    }

    public class ResearchEvaluationItem
    {
        public int ResearchId { get; set; }
        public string Title { get; set; }
        public string TraineeName { get; set; }
        public string CurrentStatus { get; set; }
    }
}