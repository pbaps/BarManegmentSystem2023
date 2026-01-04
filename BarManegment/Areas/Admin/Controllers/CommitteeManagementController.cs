using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Services; // لإضافة التدقيق
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class CommitteeManagementController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ==================================================================
        // 1. العرض والبحث (Index)
        // ==================================================================
        public ActionResult Index(string filter = "Active")
        {
            var query = db.DiscussionCommittees
                          .Include(c => c.Members)
                          .Include(c => c.Researches);

            if (filter == "Active") { query = query.Where(c => c.IsActive); }
            else if (filter == "Inactive") { query = query.Where(c => !c.IsActive); }
            // else "All" -> no filter

            var committees = query.OrderByDescending(c => c.FormationDate).ToList();

            var viewModelList = committees.Select(c => new CommitteeViewModel
            {
                Id = c.Id,
                CommitteeName = c.CommitteeName,
                FormationDate = c.FormationDate,
                IsActive = c.IsActive,
                MemberCount = c.Members.Count,
                AssignedResearchesCount = c.Researches.Count
            }).ToList();

            ViewBag.Filter = filter;
            return View(viewModelList);
        }

        // ==================================================================
        // 2. تشكيل لجنة جديدة (Create)
        // ==================================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            var viewModel = new CommitteeViewModel
            {
                // إضافة 3 صفوف افتراضية لتسهيل الإدخال
                Members = new List<CommitteeMemberSelection>
                {
                     new CommitteeMemberSelection { Role = "رئيس اللجنة" },
                     new CommitteeMemberSelection { Role = "عضو مناقش" },
                     new CommitteeMemberSelection { Role = "مشرف البحث" }
                }
            };
            viewModel.AvailableMembers = GetAvailableMembers();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(CommitteeViewModel viewModel)
        {
            // 1. تنظيف القائمة من الصفوف الفارغة
            var validMembers = viewModel.Members?.Where(m => m.MemberLawyerId > 0 && !string.IsNullOrEmpty(m.Role)).ToList();

            // 2. التحقق من القواعد
            if (validMembers == null || !validMembers.Any())
            {
                ModelState.AddModelError("Members", "يجب اختيار عضو واحد على الأقل.");
            }
            else
            {
                if (!validMembers.Any(m => m.Role == "رئيس اللجنة"))
                    ModelState.AddModelError("Members", "يجب تعيين 'رئيس اللجنة'.");

                if (validMembers.GroupBy(x => x.MemberLawyerId).Any(g => g.Count() > 1))
                    ModelState.AddModelError("Members", "لا يمكن تكرار نفس العضو في اللجنة.");
            }

            if (ModelState.IsValid)
            {
                var committee = new DiscussionCommittee
                {
                    CommitteeName = viewModel.CommitteeName,
                    FormationDate = viewModel.FormationDate,
                    IsActive = viewModel.IsActive,
                    Members = new List<CommitteeMember>()
                };

                foreach (var memberVM in validMembers)
                {
                    committee.Members.Add(new CommitteeMember
                    {
                        MemberLawyerId = memberVM.MemberLawyerId,
                        Role = memberVM.Role
                    });
                }

                try
                {
                    db.DiscussionCommittees.Add(committee);
                    db.SaveChanges();

                    // ✅ Audit
                    AuditService.LogAction("Create Committee", "CommitteeManagement", $"Created committee '{committee.CommitteeName}'");

                    TempData["SuccessMessage"] = "تم تشكيل اللجنة بنجاح.";

                    // التوجيه لصفحة التفاصيل مباشرة لإضافة الأبحاث
                    return RedirectToAction("Details", new { id = committee.Id });
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                    if (ex.InnerException != null) msg += " " + ex.InnerException.Message;
                    ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + msg);
                }
            }

            // إعادة تعبئة القائمة عند الفشل
            viewModel.AvailableMembers = GetAvailableMembers();
            return View(viewModel);
        }

        // ==================================================================
        // 3. تعديل اللجنة (Edit)
        // ==================================================================
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var committee = db.DiscussionCommittees.Include(c => c.Members).FirstOrDefault(c => c.Id == id);
            if (committee == null) return HttpNotFound();

            var viewModel = new CommitteeViewModel
            {
                Id = committee.Id,
                CommitteeName = committee.CommitteeName,
                FormationDate = committee.FormationDate,
                IsActive = committee.IsActive,
                AvailableMembers = GetAvailableMembers(),
                Members = committee.Members.Select(m => new CommitteeMemberSelection
                {
                    MemberLawyerId = m.MemberLawyerId,
                    Role = m.Role
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(CommitteeViewModel viewModel)
        {
            var validMembers = viewModel.Members?.Where(m => m.MemberLawyerId > 0 && !string.IsNullOrEmpty(m.Role)).ToList();

            if (validMembers != null && validMembers.GroupBy(x => x.MemberLawyerId).Any(g => g.Count() > 1))
                ModelState.AddModelError("Members", "لا يمكن تكرار نفس العضو.");

            if (validMembers != null && !validMembers.Any(m => m.Role == "رئيس اللجنة"))
                ModelState.AddModelError("Members", "يجب تعيين 'رئيس اللجنة'.");

            if (ModelState.IsValid)
            {
                var committeeInDb = db.DiscussionCommittees.Include(c => c.Members).FirstOrDefault(c => c.Id == viewModel.Id);
                if (committeeInDb == null) return HttpNotFound();

                // تحديث البيانات الأساسية
                committeeInDb.CommitteeName = viewModel.CommitteeName;
                committeeInDb.FormationDate = viewModel.FormationDate;
                committeeInDb.IsActive = viewModel.IsActive;

                // تحديث الأعضاء
                db.CommitteeMembers.RemoveRange(committeeInDb.Members);

                foreach (var memberVM in validMembers)
                {
                    db.CommitteeMembers.Add(new CommitteeMember
                    {
                        DiscussionCommitteeId = committeeInDb.Id,
                        MemberLawyerId = memberVM.MemberLawyerId,
                        Role = memberVM.Role
                    });
                }

                try
                {
                    db.SaveChanges();

                    // ✅ Audit
                    AuditService.LogAction("Edit Committee", "CommitteeManagement", $"Updated committee ID {committeeInDb.Id}");

                    TempData["SuccessMessage"] = "تم تحديث بيانات اللجنة بنجاح.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء التعديل: " + ex.Message);
                }
            }

            viewModel.AvailableMembers = GetAvailableMembers();
            return View(viewModel);
        }

        // ==================================================================
        // 4. تغيير الحالة (Toggle Status)
        // ==================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ToggleStatus(int id)
        {
            var committee = db.DiscussionCommittees.Find(id);
            if (committee == null) return HttpNotFound();

            // منع تعطيل لجنة مرتبطة بأبحاث نشطة
            if (committee.IsActive && db.LegalResearches.Any(r => r.DiscussionCommitteeId == id && r.Status != "مكتمل" && r.Status != "مقبول"))
            {
                TempData["ErrorMessage"] = "لا يمكن إلغاء تفعيل لجنة مرتبطة بأبحاث قيد المعالجة.";
                return RedirectToAction("Index");
            }

            committee.IsActive = !committee.IsActive;
            db.SaveChanges();

            // ✅ Audit
            AuditService.LogAction("Toggle Status", "CommitteeManagement", $"Changed status of committee ID {id} to {committee.IsActive}");

            TempData["SuccessMessage"] = $"تم {(committee.IsActive ? "تفعيل" : "إلغاء تفعيل")} اللجنة بنجاح.";
            return RedirectToAction("Index");
        }

        // ==================================================================
        // 💡💡 6. تفاصيل اللجنة وتعيين الأبحاث (Details & Assign) - جديد 💡💡
        // ==================================================================
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var committee = db.DiscussionCommittees
                .Include(c => c.Members.Select(m => m.MemberLawyer))
                .Include(c => c.Researches.Select(r => r.Trainee)) // لتظهر أسماء المتدربين
                .FirstOrDefault(c => c.Id == id);

            if (committee == null) return HttpNotFound();

            // جلب الأبحاث المتاحة (التي حالتها "مُقدم" ولم تُعين للجنة بعد)
            var availableResearches = db.LegalResearches
                .Include(r => r.Trainee)
                .Where(r => r.Status == "مُقدم" && r.DiscussionCommitteeId == null)
                .ToList() // Materialize first
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = $"بحث: {r.Title} - (المتدرب: {r.Trainee?.ArabicName})"
                }).ToList();

            var viewModel = new CommitteeDetailsViewModel
            {
                CommitteeId = committee.Id,
                CommitteeName = committee.CommitteeName,
                FormationDate = committee.FormationDate,
                IsActive = committee.IsActive,
                Members = committee.Members.ToList(),
                AssignedResearches = committee.Researches.ToList(),
                AvailableResearches = availableResearches
            };

            return View(viewModel);
        }

        // POST: Admin/CommitteeManagement/AssignResearches
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignResearches(CommitteeDetailsViewModel viewModel)
        {
            if (viewModel.SelectedResearchIds != null && viewModel.SelectedResearchIds.Any())
            {
                int count = 0;
                foreach (var researchId in viewModel.SelectedResearchIds)
                {
                    var research = db.LegalResearches.Find(researchId);
                    if (research != null && research.DiscussionCommitteeId == null)
                    {
                        research.DiscussionCommitteeId = viewModel.CommitteeId;
                        research.Status = "تم تعيين لجنة"; // تحديث الحالة تلقائياً
                        count++;
                    }
                }
                db.SaveChanges();

                // ✅ Audit
                AuditService.LogAction("Assign Researches", "CommitteeManagement", $"Assigned {count} researches to Committee ID {viewModel.CommitteeId}.");

                TempData["SuccessMessage"] = $"تم إضافة {count} أبحاث للجنة بنجاح.";
            }
            else
            {
                TempData["InfoMessage"] = "لم يتم اختيار أي أبحاث.";
            }
            return RedirectToAction("Details", new { id = viewModel.CommitteeId });
        }

        // POST: Admin/CommitteeManagement/RemoveResearch/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult RemoveResearch(int researchId)
        {
            var research = db.LegalResearches.Find(researchId);
            if (research == null) return HttpNotFound();

            int committeeId = research.DiscussionCommitteeId ?? 0;

            // السماح بالإزالة فقط إذا لم يصدر قرار
            if (research.Status == "تم تعيين لجنة")
            {
                research.DiscussionCommitteeId = null;
                research.Status = "مُقدم"; // إعادة الحالة
                db.SaveChanges();

                // ✅ Audit
                AuditService.LogAction("Remove Research", "CommitteeManagement", $"Removed Research ID {researchId} from Committee.");

                TempData["SuccessMessage"] = "تم إزالة البحث من اللجنة وإعادته لقائمة الانتظار.";
                return RedirectToAction("Details", new { id = committeeId });
            }

            TempData["ErrorMessage"] = "لا يمكن إزالة بحث تم اتخاذ قرار بشأنه.";
            return RedirectToAction("Details", new { id = committeeId });
        }

        // ==================================================================
        // 5. دوال مساعدة (Helpers & Partial Views)
        // ==================================================================

        [HttpGet]
        public ActionResult GetNewMemberRow(int index)
        {
            ViewBag.Index = index;
            ViewBag.AvailableMembers = GetAvailableMembers();
            ViewBag.AvailableRoles = new List<string> { "رئيس اللجنة", "عضو مناقش", "مشرف البحث", "عضو إضافي" };

            var model = new CommitteeMemberSelection();
            return PartialView("_CommitteeMemberEditorRow", model);
        }

        private SelectList GetAvailableMembers()
        {
            var practicingStatusId = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول")?.Id ?? 0;

            if (practicingStatusId > 0)
            {
                var members = db.GraduateApplications
                              .Where(ga => ga.ApplicationStatusId == practicingStatusId)
                              .OrderBy(ga => ga.ArabicName)
                              .Select(ga => new { Id = ga.Id, Name = ga.ArabicName + " (" + ga.Id + ")" })
                              .ToList();

                return new SelectList(members, "Id", "Name");
            }

            return new SelectList(Enumerable.Empty<SelectListItem>());
        }

        // ============================================================
        // 💡💡 7. نماذج الطباعة للجان الأبحاث (جديد) 💡💡
        // ============================================================

        // أ. جدول أعمال اللجنة (كشف بالأبحاث المعينة)
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintCommitteeAgenda(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var committee = db.DiscussionCommittees
                .Include(c => c.Members.Select(m => m.MemberLawyer))
                .Include(c => c.Researches.Select(r => r.Trainee))
                .FirstOrDefault(c => c.Id == id);

            if (committee == null) return HttpNotFound();

            AuditService.LogAction("Print Research Agenda", "CommitteeManagement", $"Printed agenda for committee {committee.CommitteeName}");

            return View("PrintCommitteeAgenda", committee);
        }

        // ب. نماذج تقييم الأبحاث (لكل بحث نموذج)
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintEvaluationSheets(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var committee = db.DiscussionCommittees
                .Include(c => c.Members.Select(m => m.MemberLawyer))
                .Include(c => c.Researches.Select(r => r.Trainee))
                .FirstOrDefault(c => c.Id == id);

            if (committee == null) return HttpNotFound();

            AuditService.LogAction("Print Research Evaluations", "CommitteeManagement", $"Printed evaluation sheets for committee {committee.CommitteeName}");

            return View("PrintEvaluationSheets", committee);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}