using BarManegment.Models;
using BarManegment.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace BarManegment.Services
{
    public class SupervisorService : IDisposable
    {
        private readonly ApplicationDbContext _db;

        public SupervisorService()
        {
            _db = new ApplicationDbContext();
        }

        public CheckResult CheckEligibility(int lawyerId)
        {
            var result = new CheckResult { IsEligible = true };

            var lawyer = _db.GraduateApplications
                .Include(l => l.ApplicationStatus)
                .FirstOrDefault(l => l.Id == lawyerId);

            if (lawyer == null)
                return new CheckResult { IsEligible = false, Message = "المحامي غير موجود." };

            // 1. التحقق من الحالة (يجب أن يكون مزاولاً)
            if (!LawyerStatusHelper.IsActiveLawyer(lawyer))
            {
                return new CheckResult
                {
                    IsEligible = false,
                    Message = $"الحالة الحالية هي '{lawyer.ApplicationStatus.Name}' (يجب أن يكون 'محامي مزاول')."
                };
            }

            // 2. التحقق من عدد المتدربين (الحد الأقصى 2)
            var activeTraineeStatuses = new List<string> {
                "متدرب مقيد", "متدرب موقوف", "بانتظار الموافقة النهائية",
                "مقبول (بانتظار الدفع)", "قيد المراجعة"
            };

            var currentTraineesCount = _db.GraduateApplications
                .Count(t => t.SupervisorId == lawyerId && activeTraineeStatuses.Contains(t.ApplicationStatus.Name));

            if (currentTraineesCount >= 2)
                return new CheckResult { IsEligible = false, Message = $"لديه الحد الأقصى من المتدربين ({currentTraineesCount})." };

            // 3. التحقق من مرور 5 سنوات على المزاولة
            var fiveYearsAgo = DateTime.Now.AddYears(-5);

            // نستخدم PracticeStartDate، وإذا كان فارغاً نستخدم SubmissionDate كاحتياط
            DateTime calculationDate = lawyer.PracticeStartDate ?? lawyer.SubmissionDate;

            bool isOldEnough = calculationDate <= fiveYearsAgo;

            if (!isOldEnough)
            {
                string dateString = calculationDate.ToString("yyyy-MM-dd");
                return new CheckResult { IsEligible = false, Message = $"لم يمر 5 سنوات على مزاولته (تاريخ المزاولة: {dateString})." };
            }

            // 4. التحقق من سنوات السداد
            var paidYearsCount = _db.Receipts
                .Where(r => r.PaymentVoucher.GraduateApplicationId == lawyerId)
                .Where(r => r.PaymentVoucher.VoucherDetails.Any(d => d.FeeType.Name.Contains("تجديد مزاولة")))
                .Count();

            if (paidYearsCount < 5)
                return new CheckResult { IsEligible = false, Message = $"غير مستوفِ للشروط المالية (سدد {paidYearsCount} سنوات فقط)." };

            return result;
        }

        public List<SupervisorDto> SearchEligibleSupervisors(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return new List<SupervisorDto>();

            int searchId = 0;
            bool isIdSearch = int.TryParse(searchTerm, out searchId);

            // البحث يشمل الاسم، الرقم الوطني، رقم العضوية، أو رقم الملف
            var candidates = _db.GraduateApplications
                .Include(l => l.ApplicationStatus)
                .Where(l => l.ArabicName.Contains(searchTerm) ||
                            l.NationalIdNumber.Contains(searchTerm) ||
                            l.MembershipId.Contains(searchTerm) || // 💡 إضافة البحث برقم العضوية
                            (isIdSearch && l.Id == searchId))
                .Take(50)
                .ToList();

            var resultList = new List<SupervisorDto>();

            foreach (var lawyer in candidates)
            {
                // استبعاد الطلاب والمتدربين من نتائج البحث
                if (lawyer.ApplicationStatus.Name.Contains("متدرب") ||
                    lawyer.ApplicationStatus.Name.Contains("طلب") ||
                    lawyer.ApplicationStatus.Name.Contains("استكمال"))
                {
                    continue;
                }

                var check = CheckEligibility(lawyer.Id);

                var activeTraineeStatuses = new List<string> { "متدرب مقيد", "متدرب موقوف" };
                var count = _db.GraduateApplications.Count(t => t.SupervisorId == lawyer.Id && activeTraineeStatuses.Contains(t.ApplicationStatus.Name));

                // تحديد تاريخ العرض
                string displayDate = lawyer.PracticeStartDate.HasValue
                    ? lawyer.PracticeStartDate.Value.ToString("yyyy-MM-dd")
                    : lawyer.SubmissionDate.ToString("yyyy-MM-dd") + " (تقديم)";

                resultList.Add(new SupervisorDto
                {
                    Id = lawyer.Id,
                    Name = lawyer.ArabicName,
                    PracticeDate = displayDate,
                    CurrentTraineeCount = count,
                    IsEligible = check.IsEligible,
                    IneligibilityReason = check.Message
                });
            }

            return resultList;
        }

        public void Dispose() { _db.Dispose(); }
    }

    public class CheckResult { public bool IsEligible { get; set; } public string Message { get; set; } }
    public class SupervisorDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PracticeDate { get; set; }
        public int CurrentTraineeCount { get; set; }
        public bool IsEligible { get; set; }
        public string IneligibilityReason { get; set; }
    }
}