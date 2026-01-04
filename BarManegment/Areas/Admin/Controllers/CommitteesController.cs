using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    public class CommitteesController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // 1. قائمة اللجان
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult Index()
        {
            var committees = db.Committees.ToList();
            return View(committees);
        }

        // 2. لوحة تحكم اللجنة (أعضاء، اجتماعات، قضايا)
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            // 1. جلب اللجنة مع الأعضاء (PanelMembers) والقضايا (Cases)
            var committee = db.Committees
                .Include(c => c.PanelMembers)
                .Include(c => c.Cases)
                .FirstOrDefault(c => c.Id == id);

            if (committee == null) return HttpNotFound();

            // =======================================================
            // ===            ✅ بداية جلب الأسماء (التحسين)            ===
            // =======================================================

            // 2. فصل المعرفات (IDs) للمحامين وأسماء المستخدمين للموظفين
            var lawyerIds = committee.PanelMembers
                .Where(m => m.LawyerId != null)
                .Select(m => m.LawyerId.Value)
                .ToList();

            var empUsernames = committee.PanelMembers
                .Where(m => !string.IsNullOrEmpty(m.EmployeeUserId))
                .Select(m => m.EmployeeUserId)
                .ToList();

            // 3. جلب الأسماء الفعلية في استعلامين منفصلين (أكثر كفاءة)
            var lawyerNames = db.GraduateApplications
                .Where(g => lawyerIds.Contains(g.Id))
                .ToDictionary(g => g.Id, g => g.ArabicName);

            var employeeNames = db.Users
                .Where(u => empUsernames.Contains(u.Username))
                .ToDictionary(u => u.Username, u => u.FullNameArabic);

            // 4. بناء قائمة العرض النهائية
            var displayMembers = new List<CommitteeMemberDisplayViewModel>();
            foreach (var member in committee.PanelMembers.OrderBy(m => m.Role))
            {
                string name = "غير معروف";
                string icon = "bi bi-person-question";

                if (member.LawyerId != null && lawyerNames.ContainsKey(member.LawyerId.Value))
                {
                    name = lawyerNames[member.LawyerId.Value];
                    icon = "bi bi-briefcase";
                }
                else if (!string.IsNullOrEmpty(member.EmployeeUserId) && employeeNames.ContainsKey(member.EmployeeUserId))
                {
                    name = employeeNames[member.EmployeeUserId];
                    icon = "bi bi-person-badge";
                }

                displayMembers.Add(new CommitteeMemberDisplayViewModel
                {
                    PanelMemberId = member.Id,
                    DisplayName = name,
                    Role = member.Role,
                    JoinDate = member.JoinDate,
                    IsActive = member.IsActive,
                    TypeIcon = icon
                });
            }

            // 5. تمرير القائمة الجاهزة للعرض إلى الواجهة
            ViewBag.DisplayMembers = displayMembers;

            return View(committee);
        }

        // 3. إضافة قضية/ملف جديد للجنة
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateCase(int committeeId, string caseNumber, string subject, string complainantName)
        {
            var newCase = new CommitteeCase
            {
                CommitteeId = committeeId,
                CaseNumber = caseNumber,
                Subject = subject,
                ComplainantName = complainantName,
                SourceType = "شكوى داخلية",
                Status = "جديد",
                CreatedDate = DateTime.Now
            };

            db.CommitteeCases.Add(newCase);
            db.SaveChanges();

            // >>> تسجيل العملية <<<
            AuditService.LogAction("Create Committee Case", "CommitteeCases", $"CommitteeId {committeeId}, CaseNo: {caseNumber}, Subject: {subject}");

            return RedirectToAction("Details", new { id = committeeId });
        }


        // 4. حفظ توصية اللجنة ورفعها للمجلس (الربط الجوهري)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult SendRecommendation(int caseId, string recommendationText)
        {
            var committeeCase = db.CommitteeCases
                .Include(c => c.Committee)
                .Include(c => c.Documents)
                .FirstOrDefault(c => c.Id == caseId);

            if (committeeCase != null)
            {
                committeeCase.FinalRecommendation = recommendationText;
                committeeCase.Status = "تم الرفع للمجلس";

                var agendaItem = new AgendaItem
                {
                    CouncilSessionId = null,
                    Title = $"توصية لجنة {committeeCase.Committee.Name} - ملف رقم {committeeCase.CaseNumber}",
                    Description = $"{committeeCase.Subject}\n\nالتوصية المرفوعة: {recommendationText}",
                    RequestType = "لجان",
                    Source = "Committee",
                    CreatedByUserId = Session["UserId"]?.ToString(),
                    IsApprovedForAgenda = false,
                    CouncilDecisionType = "Pending",
                    RequesterLawyerId = committeeCase.TargetLawyerId,
                    Attachments = new List<AgendaAttachment>()
                };

                if (committeeCase.Documents != null)
                {
                    foreach (var doc in committeeCase.Documents)
                    {
                        var newAttachment = new AgendaAttachment
                        {
                            FileName = doc.Description ?? System.IO.Path.GetFileName(doc.FilePath),
                            FilePath = doc.FilePath,
                            UploadedBy = "System (Copied from Committee)"
                        };
                        agendaItem.Attachments.Add(newAttachment);
                    }
                }

                db.AgendaItems.Add(agendaItem);
                db.SaveChanges();

                // >>> تسجيل العملية <<<
                AuditService.LogAction("Send Recommendation to Council", "AgendaItems", $"CaseId {caseId}, AgendaItemId {agendaItem.Id}, Rec: {recommendationText}");

                TempData["Success"] = $"تم رفع التوصية وعدد ({agendaItem.Attachments.Count}) مرفق إلى منسق المجلس بنجاح.";
            }

            return RedirectToAction("Details", new { id = committeeCase.CommitteeId });
        }

        // 5. إنشاء لجنة جديدة (للاستخدام في صفحة Index)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(Committee committee)
        {
            if (ModelState.IsValid)
            {
                committee.IsActive = true; // تفعيل افتراضي
                db.Committees.Add(committee);
                db.SaveChanges();

                // >>> تسجيل العملية <<<
 
                AuditService.LogAction("Create Committee", "Committees", $"Name: {committee.Name}");

                TempData["Success"] = "تم إنشاء اللجنة بنجاح.";
            }
            return RedirectToAction("Index");
        }

        // 6. إضافة عضو للجنة (عبر البحث)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult AddMember(int committeeId, string selectedMemberId, string role)
        {
            if (string.IsNullOrEmpty(selectedMemberId))
            {
                TempData["Error"] = "يجب اختيار عضو من قائمة البحث.";
                return RedirectToAction("Details", new { id = committeeId });
            }

            var member = new CommitteePanelMember
            {
                CommitteeId = committeeId,
                Role = role,
                JoinDate = DateTime.Now,
                IsActive = true
            };

            // تحليل الـ ID المختار
            if (selectedMemberId.StartsWith("L-"))
            {
                member.LawyerId = int.Parse(selectedMemberId.Substring(2));
                member.EmployeeUserId = null;
            }
            else if (selectedMemberId.StartsWith("E-"))
            {
                member.LawyerId = null;
                member.EmployeeUserId = selectedMemberId.Substring(2);
            }

            db.CommitteePanelMembers.Add(member);
            db.SaveChanges();

            // >>> تسجيل العملية <<<
            AuditService.LogAction("Add Committee Member", "CommitteePanelMembers", $"CommitteeId {committeeId}, MemberKey: {selectedMemberId}, Role: {role}");

            TempData["Success"] = "تم إضافة العضو بنجاح.";
            return RedirectToAction("Details", new { id = committeeId });
        }

        // 7. حذف عضو
        [HttpPost]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteMember(int id)
        {
            var member = db.CommitteePanelMembers.Find(id);
            if (member != null)
            {
                int commId = member.CommitteeId;
                db.CommitteePanelMembers.Remove(member);
                db.SaveChanges();

                // >>> تسجيل العملية <<<
                AuditService.LogAction("Delete Committee Member", "CommitteePanelMembers", $"MemberId {id}, CommitteeId {commId}");

                return RedirectToAction("Details", new { id = commId });
            }
            return RedirectToAction("Index");
        }

        // 8. صفحة إدارة الملف التفصيلية
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult ManageCase(int id)
        {
            var caseFile = db.CommitteeCases
                .Include(c => c.Sessions)
                .Include(c => c.Documents)
                .Include(c => c.Committee)
                .FirstOrDefault(c => c.Id == id);

            if (caseFile == null) return HttpNotFound();

            return View(caseFile);
        }

        // 9. جدولة جلسة جديدة للملف
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AddSession(int caseId, DateTime date, string title)
        {
            var session = new CaseSession
            {
                CommitteeCaseId = caseId,
                SessionDate = date,
                Title = title,
                IsCompleted = false
            };
            db.CaseSessions.Add(session);
            db.SaveChanges();

            // >>> تسجيل العملية <<<
            AuditService.LogAction("Add Committee Session", "CaseSessions", $"CaseId {caseId}, Date: {date:yyyy-MM-dd}, Title: {title}");

            return RedirectToAction("ManageCase", new { id = caseId });
        }

        // 10. حفظ محضر الجلسة وقرارها
        // 10. حفظ محضر الجلسة وقرارها (مصححة)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        // التغيير هنا: bool? isCompleted بدلاً من bool
        public ActionResult UpdateSession(int sessionId, string minutes, string decision, bool? isCompleted)
        {
            var session = db.CaseSessions.Find(sessionId);
            if (session != null)
            {
                session.Minutes = minutes;
                session.InterimDecision = decision;
                // التغيير هنا: استخدام القيمة أو false كافتراضي
                session.IsCompleted = isCompleted.GetValueOrDefault(false);
                db.SaveChanges();

                // >>> تسجيل العملية <<<
                AuditService.LogAction("Update Committee Session", "CaseSessions", $"SessionId {sessionId}, Completed: {session.IsCompleted}");
            }
            return RedirectToAction("ManageCase", new { id = session.CommitteeCaseId });
        }
        // 11. رفع مستند
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UploadDocument(int caseId, string description, System.Web.HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                // حفظ الملف في السيرفر
                var fileName = Guid.NewGuid() + "_" + System.IO.Path.GetFileName(file.FileName);
                var path = System.IO.Path.Combine(Server.MapPath("~/Uploads/CommitteeCases/"), fileName);

                System.IO.Directory.CreateDirectory(Server.MapPath("~/Uploads/CommitteeCases/"));

                file.SaveAs(path);

                // حفظ في قاعدة البيانات
                var doc = new CaseDocument
                {
                    CommitteeCaseId = caseId,
                    Description = description,
                    FilePath = "/Uploads/CommitteeCases/" + fileName,
                    UploadDate = DateTime.Now
                };
                db.CaseDocuments.Add(doc);
                db.SaveChanges();

                // >>> تسجيل العملية <<<
                AuditService.LogAction("Upload Committee Document", "CaseDocuments", $"CaseId {caseId}, File: {fileName}, Desc: {description}");
            }
            return RedirectToAction("ManageCase", new { id = caseId });
        }

        // 12. استعراض مستند مرفق
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult ViewDocument(int id)
        {
            var doc = db.CaseDocuments.Find(id);

            if (doc == null || string.IsNullOrEmpty(doc.FilePath))
            {
                return HttpNotFound("المستند غير موجود.");
            }

            string physicalPath;
            try
            {
                physicalPath = Server.MapPath(doc.FilePath);
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "مسار الملف غير صالح.");
            }

            if (!System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound("الملف المحدد غير موجود على الخادم.");
            }

            string mimeType = MimeMapping.GetMimeMapping(physicalPath);
            return File(physicalPath, mimeType);
        }

        // للبحث عن (المحامين) و (الموظفين) معاً
        [HttpGet]
        public JsonResult SearchMembers(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new { results = new List<object>() }, JsonRequestBehavior.AllowGet);
            }

            // 1. البحث عن المحامين
            var lawyers = db.GraduateApplications
                .Where(g => g.ArabicName.Contains(term) || g.Id.ToString().Contains(term))
                .Take(10)
                .ToList()
                .Select(g => new {
                    id = "L-" + g.Id,
                    text = g.ArabicName + " (محامي)"
                });

            // 2. البحث عن الموظفين
            var excludedRoles = new List<string> { "Graduate", "Advocate" };
            var employees = db.Users
                .Where(u => (u.FullNameArabic.Contains(term) || u.Username.Contains(term))
                            && !excludedRoles.Contains(u.UserType.NameEnglish))
                .Take(10)
                .ToList()
                .Select(u => new {
                    id = "E-" + u.Username,
                    text = u.FullNameArabic + " (موظف)"
                });

            // 3. دمج النتائج
            var results = lawyers.Concat(employees).ToList();

            return Json(new { results = results }, JsonRequestBehavior.AllowGet);
        }
    }
}