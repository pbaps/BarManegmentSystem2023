using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels; // استخدام الـ ViewModels المشتركة
using BarManegment.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Web;

namespace BarManegment.Areas.Members.Controllers
{
    // 1. إزالة BaseController لتجنب توجيهات الأدمن (كما هو مطلوب)
    public class CommitteePortalController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 2. حماية الجلسة: التأكد من أن المستخدم "عضو" وليس مجرد زائر
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;
            if (session["UserId"] == null)
            {
                // التوجيه لتسجيل دخول الأعضاء
                filterContext.Result = RedirectToAction("Login", "Account", new { area = "Members" });
                return;
            }
            base.OnActionExecuting(filterContext);
        }

        // لوحة القيادة (Dashboard)
        public ActionResult Dashboard()
        {
            int currentUserId = (int)Session["UserId"];

            // التأكد من أن المستخدم الحالي مرتبط بملف محامٍ
            var currentLawyer = db.GraduateApplications.FirstOrDefault(g => g.User.Id == currentUserId);

            if (currentLawyer == null)
            {
                TempData["ErrorMessage"] = "عذراً، حسابك غير مرتبط بملف محامٍ.";
                return RedirectToAction("Index", "Dashboard", new { area = "Members" });
            }

            int lawyerId = currentLawyer.Id;

            // 1. لجان الاختبار الشفوي التي أنا عضو فيها
            var myOralCommittees = db.OralExamCommittees
                .Include(c => c.Members)
                .Include(c => c.Enrollments)
                .Where(c => c.IsActive && c.Members.Any(m => m.MemberLawyerId == lawyerId))
                .OrderByDescending(c => c.FormationDate)
                .ToList();

            // 2. لجان الأبحاث التي أنا عضو فيها
            var myResearchCommittees = db.DiscussionCommittees
                .Include(c => c.Members)
                .Include(c => c.Researches)
                .Where(c => c.IsActive && c.Members.Any(m => m.MemberLawyerId == lawyerId))
                .OrderByDescending(c => c.FormationDate)
                .ToList();

            var viewModel = new CommitteeMemberDashboardViewModel
            {
                LawyerName = currentLawyer.ArabicName,
                OralCommittees = myOralCommittees,
                ResearchCommittees = myResearchCommittees
            };

            return View(viewModel);
        }

        // صفحة رصد درجات الاختبار الشفوي
        public ActionResult GradeOralExam(int committeeId)
        {
            var committee = db.OralExamCommittees
                .Include(c => c.Enrollments.Select(e => e.Trainee))
                .FirstOrDefault(c => c.Id == committeeId);

            if (committee == null) return HttpNotFound();

            // التحقق الأمني: هل أنا عضو في هذه اللجنة؟
            int currentUserId = (int)Session["UserId"];
            var currentLawyer = db.GraduateApplications.FirstOrDefault(g => g.User.Id == currentUserId);

            if (currentLawyer == null) return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            int lawyerId = currentLawyer.Id;

            if (!db.OralExamCommitteeMembers.Any(m => m.OralExamCommitteeId == committeeId && m.MemberLawyerId == lawyerId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "لست عضواً في هذه اللجنة");
            }

            var viewModel = new MemberOralGradingViewModel
            {
                CommitteeId = committee.Id,
                CommitteeName = committee.CommitteeName,
                ExamDate = committee.Enrollments.FirstOrDefault()?.ExamDate ?? DateTime.Now,
                Trainees = committee.Enrollments.Select(e => new TraineeGradeItem
                {
                    EnrollmentId = e.Id,
                    TraineeName = e.Trainee.ArabicName,
                    TraineeNumber = e.Trainee.TraineeSerialNo,
                    CurrentResult = e.Result,
                    MemberScore = e.Score
                }).ToList()
            };

            // 📝 تسجيل الدخول للصفحة
            AuditService.LogAction("View Oral Grading", "CommitteePortal", $"Member {lawyerId} viewed grading page for Committee {committeeId}");

            return View(viewModel);
        }

        // حفظ درجة الاختبار الشفوي (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitOralGrade(int enrollmentId, double score, string notes, string recommendation)
        {
            var enrollment = db.OralExamEnrollments.Find(enrollmentId);
            if (enrollment != null)
            {
                // يمكن إضافة تحقق إضافي هنا للتأكد من أن المستخدم عضو في اللجنة المرتبطة بهذا القيد

                enrollment.Score = score;
                enrollment.Notes = notes;
                enrollment.Result = recommendation;
                db.SaveChanges();

                // 📝 تسجيل الحدث
                AuditService.LogAction("Submit Oral Grade", "CommitteePortal",
                    $"EnrollmentId: {enrollmentId}, Score: {score}, Result: {recommendation}, By User: {Session["UserId"]}");

                return Json(new { success = true });
            }
            return Json(new { success = false, message = "سجل غير موجود" });
        }

        // صفحة تقييم الأبحاث
        public ActionResult EvaluateResearch(int committeeId)
        {
            var committee = db.DiscussionCommittees
                .Include(c => c.Researches.Select(r => r.Trainee))
                .FirstOrDefault(c => c.Id == committeeId);

            if (committee == null) return HttpNotFound();

            int currentUserId = (int)Session["UserId"];
            var currentLawyer = db.GraduateApplications.FirstOrDefault(g => g.User.Id == currentUserId);

            if (currentLawyer == null) return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            int lawyerId = currentLawyer.Id;

            if (!db.CommitteeMembers.Any(m => m.DiscussionCommitteeId == committeeId && m.MemberLawyerId == lawyerId))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "لست عضواً في هذه اللجنة");
            }

            var viewModel = new MemberResearchEvaluationViewModel
            {
                CommitteeId = committee.Id,
                CommitteeName = committee.CommitteeName,
                Researches = committee.Researches.Select(r => new ResearchEvaluationItem
                {
                    ResearchId = r.Id,
                    Title = r.Title,
                    TraineeName = r.Trainee.ArabicName,
                    CurrentStatus = r.Status
                }).ToList()
            };

            // 📝 تسجيل الحدث
            AuditService.LogAction("View Research Evaluation", "CommitteePortal", $"Member {lawyerId} viewed research evaluation for Committee {committeeId}");

            return View(viewModel);
        }

        // حفظ تقييم البحث (AJAX) - (إضافة مقترحة لاستكمال الوظيفة)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitResearchEvaluation(int researchId, string status, string notes)
        {
            var research = db.LegalResearches.Find(researchId);
            if (research != null)
            {
                // تحقق أمني سريع (اختياري)

                research.Status = status;
                // يمكن إضافة حقل لملاحظات اللجنة في جدول الأبحاث إذا لم يكن موجوداً
                // research.CommitteeNotes = notes; 
                db.SaveChanges();

                // 📝 تسجيل الحدث
                AuditService.LogAction("Evaluate Research", "CommitteePortal",
                    $"ResearchId: {researchId}, Status: {status}, By User: {Session["UserId"]}");

                return Json(new { success = true });
            }
            return Json(new { success = false, message = "البحث غير موجود" });
        }

        // عرض المرفقات (توجيه للأدمن)
        public ActionResult ViewAttachment(int id)
        {
            // 📝 تسجيل الحدث (من المهم معرفة من اطلع على الملفات)
            AuditService.LogAction("View Attachment", "CommitteePortal", $"User {Session["UserId"]} viewed attachment {id}");

            // إعادة التوجيه إلى متحكم الأدمن الذي يملك صلاحية فتح الملفات
            // ملاحظة: تأكد أن متحكم RegisteredTrainees يسمح بالوصول لهذا الأكشن أو انسخ كود العرض هنا
            return RedirectToAction("GetAttachmentFile", "RegisteredTrainees", new { area = "Admin", id = id });
        }

        // تسجيل الخروج
        public ActionResult LogOff()
        {
            // 📝 تسجيل الحدث
            AuditService.LogAction("Member Logout", "Account", $"User {Session["UserId"]} logged out.");

            System.Web.Security.FormsAuthentication.SignOut();
            Session.Abandon();
            Session.Clear();

            if (Request.Cookies[System.Web.Security.FormsAuthentication.FormsCookieName] != null)
            {
                var cookie = new HttpCookie(System.Web.Security.FormsAuthentication.FormsCookieName) { Expires = DateTime.Now.AddDays(-1) };
                Response.Cookies.Add(cookie);
            }

            return RedirectToAction("Login", "Account", new { area = "Members" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}