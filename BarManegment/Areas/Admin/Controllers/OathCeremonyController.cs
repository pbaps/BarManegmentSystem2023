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
    public class OathCeremonyController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. العرض والبحث
        public ActionResult Index(string filter = "Active")
        {
            var query = db.OathCeremonies.Include(c => c.Attendees);

            if (filter == "Active") { query = query.Where(c => c.IsActive); }
            else if (filter == "Inactive") { query = query.Where(c => !c.IsActive); }

            var ceremonies = query.OrderByDescending(c => c.CeremonyDate).ToList();

            var viewModelList = ceremonies.Select(c => new OathCeremonyViewModel
            {
                Id = c.Id,
                CeremonyDate = c.CeremonyDate,
                Location = c.Location,
                IsActive = c.IsActive,
                AttendeesCount = c.Attendees.Count
            }).ToList();

            ViewBag.Filter = filter;
            return View(viewModelList);
        }

        // 2. إنشاء موعد جديد
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            return View(new OathCeremonyViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(OathCeremonyViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var ceremony = new OathCeremony
                {
                    CeremonyDate = viewModel.CeremonyDate,
                    Location = viewModel.Location,
                    IsActive = viewModel.IsActive
                };
                db.OathCeremonies.Add(ceremony);
                db.SaveChanges();

                AuditService.LogAction("Create Oath Ceremony", "OathCeremony", $"Created ceremony on {ceremony.CeremonyDate:yyyy-MM-dd}");

                TempData["SuccessMessage"] = "تم إنشاء موعد حفل اليمين بنجاح.";
                return RedirectToAction("Index");
            }
            return View(viewModel);
        }

        // 3. التفاصيل وإضافة المتدربين
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var ceremony = db.OathCeremonies
                .Include("Attendees.ApplicationStatus")
                .FirstOrDefault(c => c.Id == id);

            if (ceremony == null) return HttpNotFound();

            var viewModel = new OathCeremonyDetailsViewModel
            {
                CeremonyId = ceremony.Id,
                CeremonyDate = ceremony.CeremonyDate,
                Location = ceremony.Location,
                IsActive = ceremony.IsActive,
                AssignedAttendees = ceremony.Attendees.ToList(),
                // استخدام الدالة المحسنة لجلب الأسماء
                AvailableTrainees = GetAvailableTraineesForOath(ceremony.Id)
            };

            return View(viewModel);
        }

        // 4. تنفيذ الترقية
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignTrainees(OathCeremonyDetailsViewModel viewModel)
        {
            if (viewModel.SelectedTraineeIds == null || !viewModel.SelectedTraineeIds.Any())
            {
                TempData["ErrorMessage"] = "يجب اختيار متدرب واحد على الأقل.";
                return RedirectToAction("Details", new { id = viewModel.CeremonyId });
            }

            var lawyerStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول");
            if (lawyerStatus == null)
            {
                TempData["ErrorMessage"] = "خطأ في النظام: حالة 'محامي مزاول' غير معرفة.";
                return RedirectToAction("Details", new { id = viewModel.CeremonyId });
            }

            var ceremony = db.OathCeremonies.Find(viewModel.CeremonyId);
            if (ceremony == null) return HttpNotFound();

            var traineesToAssign = db.GraduateApplications
                .Include(g => g.OathRequests)
                .Include(g => g.ApplicationStatus)
                .Where(g => viewModel.SelectedTraineeIds.Contains(g.Id))
                .ToList();

            int successCount = 0;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int lastMembershipSequence = GetLastMembershipSequence();

                    foreach (var trainee in traineesToAssign)
                    {
                        // البحث عن أي طلب يمين مفتوح
                        var oathRequest = trainee.OathRequests
                            .OrderByDescending(r => r.RequestDate)
                            .FirstOrDefault(o => o.Status != "مكتمل" && o.Status != "مرفوض");

                        if (trainee.ApplicationStatus.Name == "متدرب مقيد" && oathRequest != null)
                        {
                            lastMembershipSequence++;

                            trainee.ApplicationStatusId = lawyerStatus.Id;
                            trainee.PracticeStartDate = ceremony.CeremonyDate;
                            trainee.OathCeremonyId = ceremony.Id;
                            trainee.MembershipId = lastMembershipSequence.ToString();

                            oathRequest.Status = "مكتمل";

                            db.Entry(oathRequest).State = EntityState.Modified;
                            db.Entry(trainee).State = EntityState.Modified;

                            successCount++;
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    AuditService.LogAction("Execute Oath Ceremony", "OathCeremony", $"Promoted {successCount} trainees to Lawyers in ceremony ID {ceremony.Id}");
                    TempData["SuccessMessage"] = $"تم بنجاح ترقية {successCount} متدربين إلى محامين مزاولين ومنحهم أرقام عضوية.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "حدث خطأ أثناء الترقية: " + ex.Message;
                }
            }

            return RedirectToAction("Details", new { id = viewModel.CeremonyId });
        }

        // 5. الطباعة
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintAttendeesList(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var ceremony = db.OathCeremonies.Include(c => c.Attendees).FirstOrDefault(c => c.Id == id);
            if (ceremony == null) return HttpNotFound();

            var councilMembers = db.CouncilMembers.Where(m => m.IsActive).OrderBy(m => m.Name).ToList();

            var viewModel = new OathAttendeesViewModel
            {
                CeremonyId = ceremony.Id,
                CeremonyDate = ceremony.CeremonyDate,
                Location = ceremony.Location,
                Attendees = ceremony.Attendees.OrderBy(a => a.MembershipId).ToList(),
                SigningMembers = councilMembers
            };

            AuditService.LogAction("Print Oath Attendees", "OathCeremony", $"Printed list for ceremony ID {id}");
            return View(viewModel);
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintLicenseCertificate(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var lawyer = db.GraduateApplications.Include(g => g.ApplicationStatus).FirstOrDefault(g => g.Id == id && g.ApplicationStatus.Name == "محامي مزاول");
            if (lawyer == null) { TempData["ErrorMessage"] = "المحامي غير موجود أو غير مزاول."; return RedirectToAction("Index"); }
            AuditService.LogAction("Print License", "OathCeremony", $"Printed license for Lawyer {lawyer.ArabicName}");
            return View(lawyer);
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintPracticingCard(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var lawyer = db.GraduateApplications.Include(g => g.ContactInfo).Include(g => g.ApplicationStatus).FirstOrDefault(g => g.Id == id);
            if (lawyer == null) return HttpNotFound();

            var vm = new TraineeIdCardViewModel
            {
                TraineeName = lawyer.ArabicName,
                NationalIdNumber = lawyer.NationalIdNumber,
                MembershipId = lawyer.MembershipId,
                ProfessionalStatus = "محامي مزاول",
                CardIssueDate = DateTime.Now,
                CardExpiryDate = new DateTime(DateTime.Now.Year, 12, 31),
                PersonalPhotoPath = lawyer.PersonalPhotoPath,
                QRCodeData = $"Lawyer: {lawyer.MembershipId} | ID: {lawyer.NationalIdNumber}"
            };
            AuditService.LogAction("Print Lawyer Card", "OathCeremony", $"Printed card for Lawyer {lawyer.ArabicName}");
            return View(vm);
        }

        // 6. دوال مساعدة (Helper Methods)

        // دالة شاملة جداً لجلب أي شخص دفع رسوم اليمين
        // انسخ هذه الدالة واستبدل الدالة القديمة في OathCeremonyController.cs
        private IEnumerable<SelectListItem> GetAvailableTraineesForOath(int ceremonyId)
        {
            // 1. تحديد اسم الرسم المطلوب بدقة كما ذكرت
            string targetFeePart = "انتماء مزاولة"; // جزء من الاسم للبحث المرن
            string targetFeePart2 = "أول مرة";      // لزيادة التأكيد

            // 2. البحث عن معرّفات المتدربين الذين سددوا هذا الرسم بالتحديد
            // ندخل عبر جدول التفاصيل لأن نوع الرسم موجود هناك وليس في القسيمة نفسها
            var paidTraineeIds = db.VoucherDetails
                .Where(d => (d.FeeType.Name.Contains(targetFeePart) && d.FeeType.Name.Contains(targetFeePart2)) && // التأكد من اسم الرسم
                            d.PaymentVoucher.Status == "مسدد" &&           // التأكد أن القسيمة مسددة
                            d.PaymentVoucher.GraduateApplicationId != null) // التأكد أنها مرتبطة بمتدرب
                .Select(d => d.PaymentVoucher.GraduateApplicationId.Value)
                .Distinct()
                .ToList();

            // 3. استبعاد المتدربين المحجوزين بالفعل في حفلات أخرى (فعالة)
            var assignedTraineeIds = db.GraduateApplications
                .Where(g => g.OathCeremonyId.HasValue && g.OathCeremonyId != ceremonyId)
                .Select(g => g.Id)
                .ToList();

            // 4. جلب بيانات المتدربين (مع فلتر الحالة)
            var eligibleTrainees = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Where(g => paidTraineeIds.Contains(g.Id) &&           // شرط السداد
                            !assignedTraineeIds.Contains(g.Id) &&      // شرط عدم الحجز المسبق
                            g.ApplicationStatus.Name != "محامي مزاول" && // استبعاد من هو محامي مزاول فعلاً
                            g.ApplicationStatus.Name != "مشطوب" &&       // استبعاد المشطوبين
                                                                         // يفضل إضافة شرط الحالة الحالية، مثلاً:
                            (g.ApplicationStatus.Name.Contains("متدرب") || g.ApplicationStatus.Name.Contains("يمين")))
                .ToList();

            // 5. إرجاع القائمة للعرض
            return eligibleTrainees.Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = $"{t.ArabicName} (هوية: {t.NationalIdNumber}) - مسدد الرسوم"
            });
        }

        private int GetLastMembershipSequence()
        {
            const int STARTING_NUMBER = 10000;
            var allIds = db.GraduateApplications.Where(g => g.MembershipId != null).Select(g => g.MembershipId).ToList();
            int maxId = 0;
            foreach (var idStr in allIds)
            {
                if (int.TryParse(idStr, out int parsedId))
                {
                    if (parsedId > maxId) maxId = parsedId;
                }
            }
            return (maxId > 0) ? maxId : STARTING_NUMBER;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}