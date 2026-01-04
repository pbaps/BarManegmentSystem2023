using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.IO;
using System.Web;
using System.Collections.Generic;
using BarManegment.Services;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class LegalResearchController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. العرض والبحث
        public ActionResult Index(string statusFilter = null, string searchTerm = null)
        {
            var query = db.LegalResearches
                .Include(r => r.Trainee)
                .Include(r => r.Decisions)
                .Include(r => r.Committee)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(r => r.Status == statusFilter);
            }
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r => r.Title.Contains(searchTerm) || r.Trainee.ArabicName.Contains(searchTerm));
            }

            var researchList = query.OrderByDescending(r => r.SubmissionDate).ToList();
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Statuses = new List<string> { "مُقدم", "تم تعيين لجنة", "تعديلات مطلوبة", "مقبول", "مرفوض", "مكتمل" };

            return View(researchList);
        }

        // 2. التفاصيل
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var research = db.LegalResearches
                .Include(r => r.Trainee)
                .Include(r => r.Committee.Members.Select(m => m.MemberLawyer))
                .Include(r => r.Decisions)
                .FirstOrDefault(r => r.Id == id);

            if (research == null) return HttpNotFound();
            return View(research);
        }

        // 3. تقديم بحث جديد
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult SubmitResearch(int? traineeId)
        {
            if (traineeId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var trainee = db.GraduateApplications.Include(t => t.ApplicationStatus).FirstOrDefault(t => t.Id == traineeId);

            if (trainee == null || trainee.ApplicationStatus?.Name != "متدرب مقيد")
            {
                TempData["ErrorMessage"] = "لا يمكن تقديم بحث إلا لمتدرب مقيد.";
                return RedirectToAction("Index", "RegisteredTrainees");
            }

            // منع تقديم أكثر من بحث نشط في نفس الوقت
            bool hasPending = db.LegalResearches.Any(r => r.GraduateApplicationId == traineeId && r.Status != "مقبول" && r.Status != "مكتمل" && r.Status != "مرفوض");
            if (hasPending)
            {
                var existing = db.LegalResearches.FirstOrDefault(r => r.GraduateApplicationId == traineeId && r.Status != "مقبول" && r.Status != "مكتمل");
                TempData["InfoMessage"] = "يوجد بحث قيد المعالجة لهذا المتدرب.";
                return RedirectToAction("Details", new { id = existing.Id });
            }

            var viewModel = new SubmitResearchViewModel
            {
                GraduateApplicationId = traineeId.Value,
                TraineeName = trainee.ArabicName,
                SubmissionDate = DateTime.Now
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult SubmitResearch(SubmitResearchViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var research = new LegalResearch
                {
                    GraduateApplicationId = viewModel.GraduateApplicationId,
                    Title = viewModel.Title,
                    SubmissionDate = viewModel.SubmissionDate,
                    Status = "مُقدم"
                };
                db.LegalResearches.Add(research);
                db.SaveChanges();

                AuditService.LogAction("Submit Research", "LegalResearch", $"Research '{research.Title}' submitted for Trainee ID {viewModel.GraduateApplicationId}");
                TempData["SuccessMessage"] = "تم تسجيل عنوان البحث بنجاح.";
                return RedirectToAction("Details", new { id = research.Id });
            }
            return View(viewModel);
        }

        // 4. تعيين لجنة المناقشة
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignCommittee(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var research = db.LegalResearches.Include(r => r.Trainee).FirstOrDefault(r => r.Id == id);
            if (research == null) return HttpNotFound();

            var activeCommittees = db.DiscussionCommittees.Where(c => c.IsActive).OrderBy(c => c.CommitteeName).ToList();

            var viewModel = new AssignCommitteeViewModel
            {
                ResearchId = research.Id,
                ResearchTitle = research.Title,
                TraineeName = research.Trainee?.ArabicName,
                FormationDate = DateTime.Now,
                AvailableCommittees = new SelectList(activeCommittees, "Id", "CommitteeName")
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignCommittee(AssignCommitteeViewModel viewModel)
        {
            if (viewModel.SelectedCommitteeId <= 0) ModelState.AddModelError("SelectedCommitteeId", "يجب اختيار لجنة.");

            if (ModelState.IsValid)
            {
                var research = db.LegalResearches.Find(viewModel.ResearchId);
                if (research == null) return HttpNotFound();

                research.DiscussionCommitteeId = viewModel.SelectedCommitteeId;
                research.Status = "تم تعيين لجنة";
                db.SaveChanges();

                TempData["SuccessMessage"] = "تم تعيين لجنة المناقشة بنجاح.";
                return RedirectToAction("Details", new { id = viewModel.ResearchId });
            }

            var activeCommittees = db.DiscussionCommittees.Where(c => c.IsActive).ToList();
            viewModel.AvailableCommittees = new SelectList(activeCommittees, "Id", "CommitteeName");
            return View(viewModel);
        }

        // 5. تسجيل القرار
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult RecordDecision(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var research = db.LegalResearches.Include(r => r.Trainee).Include(r => r.Committee).FirstOrDefault(r => r.Id == id);
            if (research == null) return HttpNotFound();

            if (research.DiscussionCommitteeId == null)
            {
                TempData["ErrorMessage"] = "يجب تعيين لجنة أولاً.";
                return RedirectToAction("Details", new { id = id });
            }

            var viewModel = new RecordDecisionViewModel
            {
                ResearchId = research.Id,
                ResearchTitle = research.Title,
                TraineeName = research.Trainee?.ArabicName,
                CommitteeId = research.DiscussionCommitteeId.Value,
                DecisionDate = DateTime.Now
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult RecordDecision(RecordDecisionViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var research = db.LegalResearches.Find(viewModel.ResearchId);
                if (research == null) return HttpNotFound();

                var decision = new CommitteeDecision
                {
                    LegalResearchId = viewModel.ResearchId,
                    Result = viewModel.Result,
                    DecisionDate = viewModel.DecisionDate,
                    Notes = viewModel.Notes
                };
                db.CommitteeDecisions.Add(decision);

                if (viewModel.Result == "ناجح") research.Status = "مقبول";
                else if (viewModel.Result == "راسب") research.Status = "مرفوض";
                else if (viewModel.Result.Contains("تعديل")) research.Status = "تعديلات مطلوبة";
                else research.Status = "بانتظار القرار";

                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حفظ القرار بنجاح.";
                return RedirectToAction("Details", new { id = viewModel.ResearchId });
            }
            return View(viewModel);
        }

        // 6. رفع الملف النهائي
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UploadFinalDocument(int researchId, HttpPostedFileBase finalDocument)
        {
            if (finalDocument != null && finalDocument.ContentLength > 0)
            {
                try
                {
                    var research = db.LegalResearches.Find(researchId);
                    if (research == null) return HttpNotFound();

                    string subFolder = $"LegalResearch/{research.GraduateApplicationId}";
                    string directoryPath = Server.MapPath($"~/Uploads/{subFolder}");
                    if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);

                    string fileName = $"Final_{Guid.NewGuid()}{Path.GetExtension(finalDocument.FileName)}";
                    string fullPath = Path.Combine(directoryPath, fileName);
                    finalDocument.SaveAs(fullPath);

                    research.FinalDocumentPath = $"/Uploads/{subFolder}/{fileName}";

                    // 💡 إذا تم الرفع والبحث مقبول، نعتبره "مكتمل" نهائياً
                    if (research.Status == "مقبول") research.Status = "مكتمل";

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "تم رفع النسخة النهائية للبحث.";
                }
                catch (Exception ex) { TempData["ErrorMessage"] = "فشل الرفع: " + ex.Message; }
            }
            else
            {
                TempData["ErrorMessage"] = "الرجاء اختيار ملف.";
            }
            return RedirectToAction("Details", new { id = researchId });
        }

        // 7. استعراض الملف
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult GetResearchFile(int id)
        {
            var research = db.LegalResearches.Find(id);
            if (research == null || string.IsNullOrEmpty(research.FinalDocumentPath)) return HttpNotFound();

            var physicalPath = Server.MapPath(research.FinalDocumentPath);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound("الملف غير موجود على الخادم.");

            string contentType = MimeMapping.GetMimeMapping(physicalPath);
            return File(physicalPath, contentType);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}