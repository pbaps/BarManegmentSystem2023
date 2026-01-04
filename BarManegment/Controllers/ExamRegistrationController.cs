using BarManegment.Models;
using BarManegment.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization; // 💡 مهمة جداً لتنسيق الأرقام
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Controllers
{
    [AllowAnonymous]
    public class ExamRegistrationController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: ExamRegistration/Create
        public ActionResult Create()
        {
            // 1. التحقق من التواريخ
            var startDateSetting = db.SystemSettings.Find("ExamRegistrationStartDate");
            var endDateSetting = db.SystemSettings.Find("ExamRegistrationEndDate");
            var today = DateTime.Now.Date;

            if (startDateSetting != null && endDateSetting != null)
            {
                DateTime startDate, endDate;
                bool isStartValid = DateTime.TryParse(startDateSetting.SettingValue, out startDate);
                bool isEndValid = DateTime.TryParse(endDateSetting.SettingValue, out endDate);

                if (isStartValid && isEndValid)
                {
                    if (today < startDate || today > endDate)
                    {
                        ViewBag.RegistrationStartDate = startDate;
                        ViewBag.RegistrationEndDate = endDate;
                        return View("RegistrationClosed");
                    }
                }
            }

            // 2. جلب معدلات القبول للعرض (مع معالجة القيم الفارغة)
            double minHighSchool = 75; // القيمة الافتراضية
            double minBachelor = 65;   // القيمة الافتراضية

            var highSchoolSetting = db.SystemSettings.Find("MinHighSchoolScore");
            // نستخدم InvariantCulture لضمان قراءة النقطة العشرية بشكل صحيح
            if (highSchoolSetting != null) double.TryParse(highSchoolSetting.SettingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out minHighSchool);

            var bachelorSetting = db.SystemSettings.Find("MinBachelorScore");
            if (bachelorSetting != null) double.TryParse(bachelorSetting.SettingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out minBachelor);

            ViewBag.MinHighSchoolScore = minHighSchool;
            ViewBag.MinBachelorScore = minBachelor;

            var viewModel = new ExamApplicationViewModel
            {
                Genders = new SelectList(db.Genders, "Id", "Name")
            };
            return View(viewModel);
        }

        // POST: ExamRegistration/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ExamApplicationViewModel viewModel)
        {
            // 1. التحقق من التكرار
            if (db.ExamApplications.Any(a => a.NationalIdNumber == viewModel.NationalIdNumber))
            {
                ModelState.AddModelError("NationalIdNumber", "عذراً، هذا الرقم الوطني مسجل لدينا مسبقاً.");
            }

            // === 💡 2. التحقق الصارم من المعدلات ===
            double minHighSchool = 50;
            double minBachelor = 60;

            // جلب القيم من القاعدة مرة أخرى
            var highSchoolSetting = db.SystemSettings.Find("MinHighSchoolScore");
            if (highSchoolSetting != null) double.TryParse(highSchoolSetting.SettingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out minHighSchool);

            var bachelorSetting = db.SystemSettings.Find("MinBachelorScore");
            if (bachelorSetting != null) double.TryParse(bachelorSetting.SettingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out minBachelor);

            // المقارنة
            if (viewModel.HighSchoolPercentage < minHighSchool)
            {
                ModelState.AddModelError("HighSchoolPercentage", $"عذراً، معدل الثانوية العامة ({viewModel.HighSchoolPercentage}%) أقل من الحد الأدنى المطلوب للقبول ({minHighSchool}%).");
            }

            if (viewModel.BachelorPercentage < minBachelor)
            {
                ModelState.AddModelError("BachelorPercentage", $"عذراً، معدل البكالوريوس ({viewModel.BachelorPercentage}%) أقل من الحد الأدنى المطلوب للقبول ({minBachelor}%).");
            }
            // ========================================

            // 3. التحقق من المرفقات
            if (viewModel.HighSchoolCertificateFile == null || viewModel.HighSchoolCertificateFile.ContentLength == 0)
                ModelState.AddModelError("HighSchoolCertificateFile", "شهادة الثانوية العامة مطلوبة.");
            if (viewModel.BachelorCertificateFile == null || viewModel.BachelorCertificateFile.ContentLength == 0)
                ModelState.AddModelError("BachelorCertificateFile", "الشهادة الجامعية مطلوبة.");
            if (viewModel.PersonalIdFile == null || viewModel.PersonalIdFile.ContentLength == 0)
                ModelState.AddModelError("PersonalIdFile", "صورة الهوية مطلوبة.");

            if (ModelState.IsValid)
            {
                try
                {
                    var application = new ExamApplication
                    {
                        FullName = viewModel.FullName,
                        NationalIdNumber = viewModel.NationalIdNumber,
                        BirthDate = viewModel.BirthDate,
                        GenderId = viewModel.GenderId,
                        MobileNumber = viewModel.MobileNumber,
                        WhatsAppNumber = viewModel.WhatsAppNumber,
                        Email = viewModel.Email,
                        TelegramChatId = viewModel.TelegramChatId,
                        ApplicationDate = DateTime.Now,
                        Status = "قيد المراجعة",
                        IsAccountCreated = false,
                        Qualifications = new List<ExamQualification>()
                    };

                    application.HighSchoolCertificatePath = SaveFile(viewModel.HighSchoolCertificateFile, viewModel.NationalIdNumber, "HighSchool");
                    application.BachelorCertificatePath = SaveFile(viewModel.BachelorCertificateFile, viewModel.NationalIdNumber, "Bachelor");
                    application.PersonalIdPath = SaveFile(viewModel.PersonalIdFile, viewModel.NationalIdNumber, "ID");

                    application.Qualifications.Add(new ExamQualification { QualificationType = "الثانوية العامة", GraduationYear = viewModel.HighSchoolYear, GradePercentage = viewModel.HighSchoolPercentage });
                    application.Qualifications.Add(new ExamQualification { QualificationType = "البكالوريوس", UniversityName = viewModel.UniversityName, GraduationYear = viewModel.BachelorYear, GradePercentage = viewModel.BachelorPercentage });

                    db.ExamApplications.Add(application);
                    db.SaveChanges();

                    return RedirectToAction("Success");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء حفظ الطلب: " + ex.Message);
                }
            }

            viewModel.Genders = new SelectList(db.Genders, "Id", "Name", viewModel.GenderId);

            // إعادة تعبئة القيم للعرض عند الخطأ
            ViewBag.MinHighSchoolScore = minHighSchool;
            ViewBag.MinBachelorScore = minBachelor;

            return View(viewModel);
        }

        public ActionResult Success() { return View(); }
        public ActionResult RegistrationClosed() { return View(); }

        private string SaveFile(HttpPostedFileBase file, string nationalId, string typeSuffix)
        {
            if (file == null || file.ContentLength == 0) return null;
            string ext = Path.GetExtension(file.FileName);
            string fileName = $"{nationalId}_{typeSuffix}_{Guid.NewGuid()}{ext}";
            var directoryPath = Server.MapPath($"~/Uploads/ExamApplicants/{nationalId}");
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            var path = Path.Combine(directoryPath, fileName);
            file.SaveAs(path);
            return $"/Uploads/ExamApplicants/{nationalId}/{fileName}";
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}