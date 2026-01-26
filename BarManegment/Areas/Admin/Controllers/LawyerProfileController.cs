using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using BarManegment.ViewModels; // 💡 لاستخدام GraduateApplicationViewModel
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web; // لـ HttpPostedFileBase
using System.IO;  // لـ Path
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class LawyerProfileController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/LawyerProfile/Index
        public ActionResult Index(string searchTerm)
        {
            var practicingStatusIds = db.ApplicationStatuses
                .Where(s => s.Name.Contains("مزاول") || s.Name == "Advocate")
                .Select(s => s.Id).ToList();

            var query = db.GraduateApplications.AsNoTracking()
                .Include(g => g.ApplicationStatus)
                .Include(g => g.ContactInfo)
                .Where(g => practicingStatusIds.Contains(g.ApplicationStatusId));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(a => a.ArabicName.Contains(searchTerm) ||
                                         a.NationalIdNumber.Contains(searchTerm) ||
                                         a.MembershipId.Contains(searchTerm));
            }

            return View(query.OrderBy(a => a.MembershipId).Take(50).ToList());
        }

        // ============================================================
        // تفاصيل ملف المحامي
        // ============================================================
        // داخل LawyerProfileController
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var lawyer = db.GraduateApplications.AsNoTracking()
                .Include(a => a.ContactInfo)
                .Include(a => a.Qualifications.Select(q => q.QualificationType))
                .Include(a => a.Attachments.Select(at => at.AttachmentType))
                .Include(a => a.ApplicationStatus)
                .Include(a => a.Gender)
                .FirstOrDefault(a => a.Id == id);

            if (lawyer == null) return HttpNotFound();

            // التحقق من أنه محامٍ (نفس الكود السابق)
            if (!lawyer.ApplicationStatus.Name.Contains("مزاول") &&
                !lawyer.ApplicationStatus.Name.Contains("متقاعد") &&
                !lawyer.ApplicationStatus.Name.Contains("Advocate"))
            {
                return RedirectToAction("Details", "RegisteredTrainees", new { id = id });
            }

            // جلب البيانات
            var paymentHistory = db.Receipts.AsNoTracking()
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType))
                .Where(r => r.PaymentVoucher.GraduateApplicationId == id)
                .OrderByDescending(r => r.BankPaymentDate).ToList();

            var practicingRenewals = db.PracticingLawyerRenewals.AsNoTracking()
                .Include(r => r.Receipt.PaymentVoucher)
                .Where(r => r.GraduateApplicationId == id)
                .OrderByDescending(r => r.RenewalYear).ToList();

            var myTrainees = db.GraduateApplications.AsNoTracking()
                .Include(g => g.ApplicationStatus) // نحتاج الحالة لعرضها
                .Where(g => g.SupervisorId == id) // جلب الكل (وليس المقيدين فقط) للأرشيف
                .OrderByDescending(g => g.TrainingStartDate)
                .ToList();

            var pendingLogs = db.TrainingLogs.AsNoTracking()
                .Include(l => l.Trainee)
                .Where(l => l.SupervisorId == id && l.Status == "بانتظار موافقة المشرف")
                .OrderBy(l => l.SubmissionDate).ToList();

            var loans = db.LoanApplications.AsNoTracking()
                .Include(l => l.LoanType)
                .Where(l => l.LawyerId == id).OrderByDescending(l => l.ApplicationDate).ToList();

            // ✅ 6. جلب قرارات المجلس الخاصة بالمحامي
            var councilDecisions = db.AgendaItems.AsNoTracking()
                .Include(a => a.CouncilSession) // لجلب رقم وتاريخ الجلسة
                .Where(a => a.RequesterLawyerId == id) // ⚠️ هذا هو الحقل الصحيح بناءً على ملفك
                .Where(a => !string.IsNullOrEmpty(a.DecisionText) || a.CouncilDecisionType != "Pending") // نجلب القرارات المبتوت فيها
                .OrderByDescending(a => a.CouncilSession.SessionDate)
                .ToList();
            // حساب الإحصائيات
            int yearsExp = lawyer.PracticeStartDate.HasValue
                ? (DateTime.Now.Year - lawyer.PracticeStartDate.Value.Year)
                : 0;

            var viewModel = new LawyerProfileViewModel
            {
                Id = lawyer.Id,
                ArabicName = lawyer.ArabicName,
                EnglishName = lawyer.EnglishName,
                NationalIdNumber = lawyer.NationalIdNumber,
                MembershipId = lawyer.MembershipId,
                PracticeStartDate = lawyer.PracticeStartDate,
                Status = lawyer.ApplicationStatus.Name,
                PersonalPhotoPath = lawyer.PersonalPhotoPath,
                BirthDate = lawyer.BirthDate,
                ContactInfo = lawyer.ContactInfo ?? new ContactInfo(),
                Gender = lawyer.Gender,

                Qualifications = lawyer.Qualifications.ToList(),
                Attachments = lawyer.Attachments.ToList(),
                PaymentHistory = paymentHistory,
                PracticingRenewals = practicingRenewals,
                MyTrainees = myTrainees,
                PendingTrainingLogs = pendingLogs,
                Loans = loans, // ✅ الآن جزء من الموديل
                CouncilDecisions = councilDecisions, // ✅ تمرير القائمة
                // الإحصائيات
                YearsOfExperience = yearsExp,
                ActiveTraineesCount = myTrainees.Count(t => t.ApplicationStatus.Name == "متدرب مقيد"),
                TotalLoansAmount = loans.Sum(l => l.Amount),
                LastRenewalYear = practicingRenewals.FirstOrDefault()?.RenewalYear.ToString() ?? "لا يوجد"
            };

            // القوائم المنسدلة للمودالات
            ViewBag.QualificationTypes = new SelectList(db.QualificationTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");
            ViewBag.AttachmentTypes = new SelectList(db.AttachmentTypes.OrderBy(t => t.Name).ToList(), "Id", "Name");

            return View(viewModel);
        }

        // ============================================================
        // تعديل بيانات المحامي (Edit)
        // ============================================================
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var lawyer = db.GraduateApplications
                .Include(a => a.ContactInfo)
                .FirstOrDefault(a => a.Id == id);

            if (lawyer == null) return HttpNotFound();

            var viewModel = new GraduateApplicationViewModel
            {
                Id = lawyer.Id,
                ArabicName = lawyer.ArabicName,
                EnglishName = lawyer.EnglishName,
                NationalIdNumber = lawyer.NationalIdNumber,
                NationalIdTypeId = lawyer.NationalIdTypeId,
                BirthDate = lawyer.BirthDate,
                BirthPlace = lawyer.BirthPlace,
                Nationality = lawyer.Nationality,
                GenderId = lawyer.GenderId,

                ContactInfo = lawyer.ContactInfo ?? new ContactInfo { Id = lawyer.Id },

                // البيانات البنكية
                BankName = lawyer.BankName,
                BankBranch = lawyer.BankBranch,
                AccountNumber = lawyer.AccountNumber,
                Iban = lawyer.Iban,

                // القوائم المنسدلة
                Genders = new SelectList(db.Genders, "Id", "Name", lawyer.GenderId),
                NationalIdTypes = new SelectList(db.NationalIdTypes, "Id", "Name", lawyer.NationalIdTypeId),
                Countries = new SelectList(new[] { new SelectListItem { Text = "فلسطين", Value = "فلسطين" } }, "Value", "Text", lawyer.Nationality),
                Governorates = new SelectList(new[] { new SelectListItem { Text = "غزة", Value = "غزة" }, new SelectListItem { Text = "شمال غزة", Value = "شمال غزة" }, new SelectListItem { Text = "الوسطى", Value = "الوسطى" }, new SelectListItem { Text = "خانيونس", Value = "خانيونس" }, new SelectListItem { Text = "رفح", Value = "رفح" } }, "Value", "Text", lawyer.ContactInfo?.Governorate)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(GraduateApplicationViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var lawyerInDb = db.GraduateApplications
                    .Include(a => a.ContactInfo)
                    .FirstOrDefault(a => a.Id == viewModel.Id);

                if (lawyerInDb == null) return HttpNotFound();

                // تحديث البيانات
                lawyerInDb.ArabicName = viewModel.ArabicName;
                lawyerInDb.EnglishName = viewModel.EnglishName;
                lawyerInDb.NationalIdNumber = viewModel.NationalIdNumber;
                lawyerInDb.NationalIdTypeId = viewModel.NationalIdTypeId;
                lawyerInDb.BirthDate = viewModel.BirthDate;
                lawyerInDb.BirthPlace = viewModel.BirthPlace;
                lawyerInDb.Nationality = viewModel.Nationality;
                lawyerInDb.GenderId = viewModel.GenderId;

                // تحديث الصورة
                if (viewModel.PersonalPhotoFile != null && viewModel.PersonalPhotoFile.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(viewModel.PersonalPhotoFile.FileName);
                    string path = Path.Combine(Server.MapPath("~/Uploads/PersonalPhotos"), fileName);
                    // تأكد من وجود المجلد
                    if (!Directory.Exists(Server.MapPath("~/Uploads/PersonalPhotos"))) Directory.CreateDirectory(Server.MapPath("~/Uploads/PersonalPhotos"));

                    viewModel.PersonalPhotoFile.SaveAs(path);
                    lawyerInDb.PersonalPhotoPath = "/Uploads/PersonalPhotos/" + fileName;
                }

                // البيانات المالية
                lawyerInDb.BankName = viewModel.BankName;
                lawyerInDb.BankBranch = viewModel.BankBranch;
                lawyerInDb.AccountNumber = viewModel.AccountNumber;
                lawyerInDb.Iban = viewModel.Iban;

                // بيانات الاتصال
                if (lawyerInDb.ContactInfo == null)
                {
                    viewModel.ContactInfo.Id = lawyerInDb.Id;
                    db.ContactInfos.Add(viewModel.ContactInfo);
                }
                else
                {
                    lawyerInDb.ContactInfo.Governorate = viewModel.ContactInfo.Governorate;
                    lawyerInDb.ContactInfo.City = viewModel.ContactInfo.City;
                    lawyerInDb.ContactInfo.Street = viewModel.ContactInfo.Street;
                    lawyerInDb.ContactInfo.MobileNumber = viewModel.ContactInfo.MobileNumber;
                    lawyerInDb.ContactInfo.Email = viewModel.ContactInfo.Email;
                }

                db.SaveChanges();
                AuditService.LogAction("Edit Lawyer Profile", "LawyerProfile", $"Updated profile for Lawyer {lawyerInDb.ArabicName}");

                TempData["SuccessMessage"] = "تم حفظ التعديلات بنجاح.";
                return RedirectToAction("Details", new { id = viewModel.Id });
            }

            // إعادة تعبئة القوائم عند الفشل
            viewModel.Genders = new SelectList(db.Genders, "Id", "Name", viewModel.GenderId);
            viewModel.NationalIdTypes = new SelectList(db.NationalIdTypes, "Id", "Name", viewModel.NationalIdTypeId);
            viewModel.Countries = new SelectList(new[] { new SelectListItem { Text = "فلسطين", Value = "فلسطين" } }, "Value", "Text", viewModel.Nationality);
            viewModel.Governorates = new SelectList(new[] { new SelectListItem { Text = "غزة", Value = "غزة" }, new SelectListItem { Text = "شمال غزة", Value = "شمال غزة" }, new SelectListItem { Text = "الوسطى", Value = "الوسطى" }, new SelectListItem { Text = "خانيونس", Value = "خانيونس" }, new SelectListItem { Text = "رفح", Value = "رفح" } }, "Value", "Text", viewModel.ContactInfo?.Governorate);

            return View(viewModel);
        }
        // POST: Admin/LawyerProfile/AddQualification
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AddQualification(FormCollection form)
        {
            int applicationId = int.Parse(form["applicationId"]);
            try
            {
                var qualification = new Qualification
                {
                    GraduateApplicationId = applicationId,
                    QualificationTypeId = int.Parse(form["QualificationTypeId"]),
                    UniversityName = form["UniversityName"],
                    Faculty = form["Faculty"],
                    Specialization = form["Specialization"],
                    GraduationYear = int.Parse(form["GraduationYear"])
                };

                if (!string.IsNullOrWhiteSpace(form["GradePercentage"]))
                {
                    if (double.TryParse(form["GradePercentage"], out double grade))
                        qualification.GradePercentage = grade;
                }

                db.Qualifications.Add(qualification);
                db.SaveChanges();

                AuditService.LogAction("Add Qualification", "LawyerProfile", $"Lawyer ID {applicationId}, Added: {qualification.UniversityName}");
                TempData["SuccessMessage"] = "تمت إضافة المؤهل بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ: " + ex.Message;
            }
            return RedirectToAction("Details", new { id = applicationId });
        }

        // POST: Admin/LawyerProfile/AddAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AddAttachment(int applicationId, int AttachmentTypeId, HttpPostedFileBase UploadedFile)
        {
            if (UploadedFile == null || UploadedFile.ContentLength == 0)
            {
                TempData["ErrorMessage"] = "الرجاء اختيار ملف.";
                return RedirectToAction("Details", new { id = applicationId });
            }

            try
            {
                string path = Server.MapPath($"~/Uploads/Attachments/{applicationId}");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                string extension = Path.GetExtension(UploadedFile.FileName);
                string fileName = Guid.NewGuid().ToString() + extension;
                string fullPath = Path.Combine(path, fileName);
                UploadedFile.SaveAs(fullPath);

                var attachment = new Attachment
                {
                    GraduateApplicationId = applicationId,
                    AttachmentTypeId = AttachmentTypeId,
                    FilePath = $"/Uploads/Attachments/{applicationId}/" + fileName,
                    OriginalFileName = Path.GetFileName(UploadedFile.FileName),
                    UploadDate = DateTime.Now
                };

                db.Attachments.Add(attachment);
                db.SaveChanges();

                AuditService.LogAction("Add Attachment", "LawyerProfile", $"Lawyer ID {applicationId}, File: {attachment.OriginalFileName}");
                TempData["SuccessMessage"] = "تم رفع المرفق بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء الرفع: " + ex.Message;
            }
            return RedirectToAction("Details", new { id = applicationId });
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}