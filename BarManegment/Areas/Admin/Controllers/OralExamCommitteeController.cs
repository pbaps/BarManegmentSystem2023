using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class OralExamCommitteeController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ==================================================================
        // 1. العرض والبحث (Index)
        // ==================================================================
        public ActionResult Index(string filter = "Active")
        {
            var query = db.OralExamCommittees
                          .Include(c => c.Members)
                          .Include(c => c.Enrollments);

            if (filter == "Active") { query = query.Where(c => c.IsActive); }
            else if (filter == "Inactive") { query = query.Where(c => !c.IsActive); }

            var committees = query.OrderByDescending(c => c.FormationDate).ToList();

            var viewModelList = committees.Select(c => new OralExamCommitteeViewModel
            {
                Id = c.Id,
                CommitteeName = c.CommitteeName,
                FormationDate = c.FormationDate,
                IsActive = c.IsActive,
                MemberCount = c.Members.Count,
                AssignedTraineesCount = c.Enrollments.Count
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
            var viewModel = new OralExamCommitteeViewModel();
            viewModel.AvailableMembers = GetAvailableMembers();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(OralExamCommitteeViewModel viewModel)
        {
            var memberIds = viewModel.Members?.Where(m => m.MemberLawyerId > 0).Select(m => m.MemberLawyerId).ToList() ?? new List<int>();

            if (memberIds.Count != memberIds.Distinct().Count())
                ModelState.AddModelError("Members", "لا يمكن اختيار نفس العضو أكثر من مرة.");

            if (viewModel.Members == null || !viewModel.Members.Any(m => m.Role == "رئيس اللجنة" && m.MemberLawyerId > 0))
                ModelState.AddModelError("Members", "يجب تحديد 'رئيس اللجنة'.");

            if (ModelState.IsValid)
            {
                var committee = new OralExamCommittee
                {
                    CommitteeName = viewModel.CommitteeName,
                    FormationDate = viewModel.FormationDate,
                    IsActive = viewModel.IsActive,
                    Members = new List<OralExamCommitteeMember>()
                };

                foreach (var memberVM in viewModel.Members.Where(m => m.MemberLawyerId > 0 && !string.IsNullOrEmpty(m.Role)))
                {
                    committee.Members.Add(new OralExamCommitteeMember
                    {
                        MemberLawyerId = memberVM.MemberLawyerId,
                        Role = memberVM.Role
                    });
                }

                if (committee.Members.Any())
                {
                    db.OralExamCommittees.Add(committee);
                    try
                    {
                        db.SaveChanges();
                        AuditService.LogAction("Create Oral Exam Committee", "OralExamCommittee", $"Created committee '{committee.CommitteeName}' with {committee.Members.Count} members.");
                        TempData["SuccessMessage"] = "تم إنشاء اللجنة الشفوية بنجاح.";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + (ex.InnerException?.Message ?? ex.Message));
                    }
                }
                else
                {
                    ModelState.AddModelError("Members", "يجب اختيار عضو واحد على الأقل.");
                }
            }
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
            var committee = db.OralExamCommittees.Include(c => c.Members).FirstOrDefault(c => c.Id == id);
            if (committee == null) return HttpNotFound();

            if (committee.Enrollments.Any())
            {
                TempData["ErrorMessage"] = "لا يمكن تعديل لجنة تم تعيين متدربين لها.";
                return RedirectToAction("Index");
            }

            var viewModel = new OralExamCommitteeViewModel
            {
                Id = committee.Id,
                CommitteeName = committee.CommitteeName,
                FormationDate = committee.FormationDate,
                IsActive = committee.IsActive,
                AvailableMembers = GetAvailableMembers(),
                AvailableRoles = new List<string> { "رئيس اللجنة", "عضو ممتحن" },
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
        public ActionResult Edit(OralExamCommitteeViewModel viewModel)
        {
            var memberIds = viewModel.Members?.Where(m => m.MemberLawyerId > 0).Select(m => m.MemberLawyerId).ToList() ?? new List<int>();
            if (memberIds.Count != memberIds.Distinct().Count()) ModelState.AddModelError("Members", "تكرار العضو.");

            if (ModelState.IsValid)
            {
                var committeeInDb = db.OralExamCommittees.Include(c => c.Members).FirstOrDefault(c => c.Id == viewModel.Id);
                if (committeeInDb == null) return HttpNotFound();

                if (db.OralExamEnrollments.Any(e => e.OralExamCommitteeId == viewModel.Id))
                {
                    TempData["ErrorMessage"] = "لا يمكن تعديل لجنة مرتبطة بمتدربين.";
                    return RedirectToAction("Index");
                }

                committeeInDb.CommitteeName = viewModel.CommitteeName;
                committeeInDb.FormationDate = viewModel.FormationDate;
                committeeInDb.IsActive = viewModel.IsActive;

                db.OralExamCommitteeMembers.RemoveRange(committeeInDb.Members);

                foreach (var memberVM in viewModel.Members.Where(m => m.MemberLawyerId > 0 && !string.IsNullOrEmpty(m.Role)))
                {
                    db.OralExamCommitteeMembers.Add(new OralExamCommitteeMember
                    {
                        OralExamCommitteeId = committeeInDb.Id,
                        MemberLawyerId = memberVM.MemberLawyerId,
                        Role = memberVM.Role
                    });
                }

                try
                {
                    db.SaveChanges();
                    AuditService.LogAction("Edit Oral Exam Committee", "OralExamCommittee", $"Updated committee ID {committeeInDb.Id}.");
                    TempData["SuccessMessage"] = "تم تعديل اللجنة بنجاح.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ: " + (ex.InnerException?.Message ?? ex.Message));
                }
            }
            viewModel.AvailableMembers = GetAvailableMembers();
            return View(viewModel);
        }

        // ==================================================================
        // 4. تفعيل/تعطيل (ToggleStatus)
        // ==================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ToggleStatus(int id)
        {
            var committee = db.OralExamCommittees.Find(id);
            if (committee == null) return HttpNotFound();
            if (committee.IsActive && db.OralExamEnrollments.Any(e => e.OralExamCommitteeId == id && e.Result == null))
            {
                TempData["ErrorMessage"] = "لا يمكن إلغاء تفعيل لجنة مرتبطة بمتدربين لم يمتحنوا بعد.";
                return RedirectToAction("Index");
            }
            committee.IsActive = !committee.IsActive;
            db.SaveChanges();
            AuditService.LogAction("Toggle Committee Status", "OralExamCommittee", $"Committee '{committee.CommitteeName}' status set to {committee.IsActive}.");
            TempData["SuccessMessage"] = $"تم {(committee.IsActive ? "تفعيل" : "إلغاء تفعيل")} اللجنة.";
            return RedirectToAction("Index");
        }

        // ==================================================================
        // 5. تفاصيل اللجنة وتعيين المتدربين
        // ==================================================================
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var committee = db.OralExamCommittees
                .Include(c => c.Members.Select(m => m.MemberLawyer))
                .Include(c => c.Enrollments.Select(e => e.Trainee))
                .FirstOrDefault(c => c.Id == id);
            if (committee == null) return HttpNotFound();

            var enrolledTraineeIds = committee.Enrollments.Select(e => e.GraduateApplicationId).ToHashSet();
            var successfulWrittenExamTraineeIds = db.ExamEnrollments
                .Where(e => e.Exam.ExamType.Name.Contains("إنهاء تدريب") && e.Result == "ناجح")
                .Select(e => e.GraduateApplicationId).Where(i => i.HasValue).Select(i => i.Value)
                .Distinct().ToHashSet();

            var availableTrainees = db.GraduateApplications
                .Where(ga => ga.ApplicationStatus.Name == "متدرب مقيد" &&
                             successfulWrittenExamTraineeIds.Contains(ga.Id) &&
                             !enrolledTraineeIds.Contains(ga.Id))
                .Select(ga => new SelectListItem
                {
                    Value = ga.Id.ToString(),
                    Text = ga.ArabicName + " (رقم: " + ga.TraineeSerialNo + ")"
                }).ToList();

            var viewModel = new OralExamDetailsViewModel
            {
                CommitteeId = committee.Id,
                CommitteeName = committee.CommitteeName,
                IsActive = committee.IsActive,
                FormationDate = committee.FormationDate,
                Members = committee.Members.ToList(),
                EnrolledTrainees = committee.Enrollments.ToList(),
                AvailableTrainees = availableTrainees
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignTrainees(OralExamDetailsViewModel viewModel)
        {
            if (viewModel.SelectedTraineeIds != null && viewModel.SelectedTraineeIds.Any())
            {
                int count = 0;
                foreach (var traineeId in viewModel.SelectedTraineeIds)
                {
                    bool alreadyEnrolled = db.OralExamEnrollments.Any(e => e.GraduateApplicationId == traineeId && e.OralExamCommitteeId == viewModel.CommitteeId);
                    if (!alreadyEnrolled)
                    {
                        db.OralExamEnrollments.Add(new OralExamEnrollment
                        {
                            GraduateApplicationId = traineeId,
                            OralExamCommitteeId = viewModel.CommitteeId,
                            ExamDate = viewModel.ExamDate,
                            Result = "قيد الانتظار"
                        });
                        count++;
                    }
                }
                db.SaveChanges();
                AuditService.LogAction("Assign Trainees to Oral Exam", "OralExamCommittee", $"Assigned {count} trainees to committee ID {viewModel.CommitteeId}.");
                TempData["SuccessMessage"] = "تم تسجيل المتدربين المختارين في اللجنة بنجاح.";
            }
            else
            {
                TempData["InfoMessage"] = "لم يتم اختيار أي متدربين جدد.";
            }
            return RedirectToAction("Details", new { id = viewModel.CommitteeId });
        }

        // ==================================================================
        // 6. رصد النتائج (Record Result) - 💡 التعديل هنا
        // ==================================================================
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult RecordResult(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var enrollment = db.OralExamEnrollments
                .Include(e => e.Trainee)
                .Include(e => e.OralExamCommittee)
                .FirstOrDefault(e => e.Id == id);

            if (enrollment == null) return HttpNotFound();

            if (enrollment.Result != "قيد الانتظار")
            {
                TempData["InfoMessage"] = "تم تسجيل نتيجة هذا الاختبار مسبقاً.";
                // السماح بالتعديل للمصحح
            }

            var viewModel = new RecordOralExamResultViewModel
            {
                EnrollmentId = enrollment.Id,
                TraineeName = enrollment.Trainee.ArabicName,
                CommitteeName = enrollment.OralExamCommittee.CommitteeName,
                ExamDate = enrollment.ExamDate,
                Result = enrollment.Result,
                CommitteeId = enrollment.OralExamCommitteeId
            };

            ViewBag.ResultsList = new SelectList(new[] { "ناجح", "راسب", "لم يحضر" });
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult RecordResult(RecordOralExamResultViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ResultsList = new SelectList(new[] { "ناجح", "راسب", "لم يحضر" }, viewModel.Result);
                return View(viewModel);
            }

            var enrollment = db.OralExamEnrollments
                .Include(e => e.Trainee)
                .FirstOrDefault(e => e.Id == viewModel.EnrollmentId);

            if (enrollment == null) return HttpNotFound();

            // تحديث النتيجة فقط
            enrollment.Result = viewModel.Result;
            enrollment.Score = viewModel.Score;
            enrollment.Notes = viewModel.Notes;

            // 💡 تم إزالة كود الترقية التلقائية لـ "محامي مزاول"
            // الترقية ستتم لاحقاً في OathCeremonyController بعد حلف اليمين

            try
            {
                db.SaveChanges();

                AuditService.LogAction("Record Oral Exam Result", "OralExamCommittee", $"Result '{viewModel.Result}' recorded for Trainee {enrollment.Trainee?.ArabicName}.");

                TempData["SuccessMessage"] = $"تم رصد النتيجة ({viewModel.Result}) بنجاح.";

                // إذا نجح، يمكن إضافة رسالة تنبيهية صغيرة
                if (viewModel.Result == "ناجح")
                {
                    TempData["InfoMessage"] = "أصبح المتدرب الآن مؤهلاً للتقدم بطلب أداء اليمين (إذا استوفى باقي الشروط).";
                }

                return RedirectToAction("Details", new { id = enrollment.OralExamCommitteeId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                ViewBag.ResultsList = new SelectList(new[] { "ناجح", "راسب", "لم يحضر" }, viewModel.Result);
                return View(viewModel);
            }
        }
        // ============================================================
        // 7. نماذج الطباعة (المصححة)
        // ============================================================

        // أ. كشف الرصد النهائي (Master Sheet)
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintMasterSheet(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // 💡 التصحيح: إرسال كائن اللجنة (Entity) وليس الـ ViewModel لتجنب الخطأ
            var committee = db.OralExamCommittees
                .Include(c => c.Members.Select(m => m.MemberLawyer))
                .Include(c => c.Enrollments.Select(e => e.Trainee))
                .FirstOrDefault(c => c.Id == id);

            if (committee == null) return HttpNotFound();

            AuditService.LogAction("Print Master Sheet", "OralExamCommittee", $"Printed master sheet for committee {committee.CommitteeName}");

            // إرسال الـ Model من نوع BarManegment.Models.OralExamCommittee
            return View("PrintMasterSheet", committee);
        }

        // ب. أوراق التقييم الفردية للأعضاء
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintMemberScoreSheets(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var committee = db.OralExamCommittees
                .Include(c => c.Members.Select(m => m.MemberLawyer))
                .Include(c => c.Enrollments.Select(e => e.Trainee))
                .FirstOrDefault(c => c.Id == id);

            if (committee == null) return HttpNotFound();

            AuditService.LogAction("Print Member Sheets", "OralExamCommittee", $"Printed member score sheets for committee {committee.CommitteeName}");

            return View("PrintMemberScoreSheets", committee);
        }


        // ... (Helpers) ...
        [HttpGet]
        public ActionResult GetNewMemberRow(int index)
        {
            ViewData["Index"] = index;
            ViewData["AvailableMembers"] = GetAvailableMembers();
            ViewData["AvailableRoles"] = new List<string> { "رئيس اللجنة", "عضو ممتحن" };
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
                              .Select(ga => new { Id = ga.Id, Name = ga.ArabicName + " (رقم: " + ga.Id + ")" })
                              .ToList();
                return new SelectList(members, "Id", "Name");
            }
            return new SelectList(Enumerable.Empty<SelectListItem>());
        }

        // --- AssignTrainee المفقودة التي قد يحتاجها ملف المتدرب ---
        // GET: Admin/OralExamCommittee/AssignTrainee?traineeId=5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignTrainee(int? traineeId)
        {
            if (traineeId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var trainee = db.GraduateApplications.Find(traineeId.Value);
            if (trainee == null) return HttpNotFound();

            var viewModel = new AssignTraineeToOralCommitteeViewModel
            {
                TraineeId = trainee.Id,
                TraineeName = trainee.ArabicName,
                AvailableCommittees = new SelectList(db.OralExamCommittees.Where(c => c.IsActive).OrderBy(c => c.CommitteeName).ToList(), "Id", "CommitteeName")
            };

            if (!viewModel.AvailableCommittees.Any())
            {
                TempData["ErrorMessage"] = "لا توجد لجان فعالة حالياً.";
                return RedirectToAction("Details", "RegisteredTrainees", new { id = traineeId });
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignTrainee(AssignTraineeToOralCommitteeViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                bool alreadyEnrolled = db.OralExamEnrollments.Any(e => e.GraduateApplicationId == viewModel.TraineeId);
                if (alreadyEnrolled)
                {
                    TempData["ErrorMessage"] = "المتدرب مسجل مسبقاً.";
                    return RedirectToAction("Details", "RegisteredTrainees", new { id = viewModel.TraineeId });
                }

                db.OralExamEnrollments.Add(new OralExamEnrollment
                {
                    GraduateApplicationId = viewModel.TraineeId,
                    OralExamCommitteeId = viewModel.SelectedCommitteeId,
                    ExamDate = viewModel.ExamDate,
                    Result = "قيد الانتظار"
                });
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم التسجيل بنجاح.";
                return RedirectToAction("Details", "RegisteredTrainees", new { id = viewModel.TraineeId });
            }
            viewModel.AvailableCommittees = new SelectList(db.OralExamCommittees.Where(c => c.IsActive).ToList(), "Id", "CommitteeName");
            return View(viewModel);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}