using BarManegment.Models;
using BarManegment.ViewModels;
using BarManegment.Helpers;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BarManegment.Services;

namespace BarManegment.Controllers
{
    [AllowAnonymous]
    public class ExamRegistrationController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: ExamRegistration/Create
        public ActionResult Create()
        {
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

            double minHighSchool = 75;
            double minBachelor = 65;

            var highSchoolSetting = db.SystemSettings.Find("MinHighSchoolScore");
            if (highSchoolSetting != null) double.TryParse(highSchoolSetting.SettingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out minHighSchool);

            var bachelorSetting = db.SystemSettings.Find("MinBachelorScore");
            if (bachelorSetting != null) double.TryParse(bachelorSetting.SettingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out minBachelor);

            ViewBag.MinHighSchoolScore = minHighSchool;
            ViewBag.MinBachelorScore = minBachelor;

            // ✅ جلب إعداد الرسوم لعرضه في الـ View
            var feeSetting = db.SystemSettings.Find("IsExamFeeEnabled");
            ViewBag.IsExamFeeEnabled = feeSetting != null ? bool.Parse(feeSetting.SettingValue) : true;

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
            if (db.ExamApplications.Any(a => a.NationalIdNumber == viewModel.NationalIdNumber))
            {
                ModelState.AddModelError("NationalIdNumber", "عذراً، هذا الرقم الوطني مسجل لدينا مسبقاً.");
            }

            double minHighSchool = 50;
            double minBachelor = 60;

            var highSchoolSetting = db.SystemSettings.Find("MinHighSchoolScore");
            if (highSchoolSetting != null) double.TryParse(highSchoolSetting.SettingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out minHighSchool);

            var bachelorSetting = db.SystemSettings.Find("MinBachelorScore");
            if (bachelorSetting != null) double.TryParse(bachelorSetting.SettingValue, NumberStyles.Any, CultureInfo.InvariantCulture, out minBachelor);

            if (viewModel.HighSchoolPercentage < minHighSchool)
                ModelState.AddModelError("HighSchoolPercentage", $"عذراً، معدل الثانوية العامة ({viewModel.HighSchoolPercentage}%) أقل من الحد الأدنى المطلوب للقبول ({minHighSchool}%).");

            if (viewModel.BachelorPercentage < minBachelor)
                ModelState.AddModelError("BachelorPercentage", $"عذراً، معدل البكالوريوس ({viewModel.BachelorPercentage}%) أقل من الحد الأدنى المطلوب للقبول ({minBachelor}%).");

            if (viewModel.HighSchoolCertificateFile == null || viewModel.HighSchoolCertificateFile.ContentLength == 0)
                ModelState.AddModelError("HighSchoolCertificateFile", "شهادة الثانوية العامة مطلوبة.");
            if (viewModel.BachelorCertificateFile == null || viewModel.BachelorCertificateFile.ContentLength == 0)
                ModelState.AddModelError("BachelorCertificateFile", "الشهادة الجامعية مطلوبة.");
            if (viewModel.PersonalIdFile == null || viewModel.PersonalIdFile.ContentLength == 0)
                ModelState.AddModelError("PersonalIdFile", "صورة الهوية مطلوبة.");

            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // ✅ فحص تفعيل الرسوم
                        var feeSetting = db.SystemSettings.Find("IsExamFeeEnabled");
                        bool isFeeEnabled = feeSetting != null ? bool.Parse(feeSetting.SettingValue) : true;

                        // ✅ تحديد الحالة الأولية
                        string initialStatus = isFeeEnabled ? "بانتظار دفع الرسوم" : "قيد المراجعة";

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
                            Status = initialStatus, // ✅ الحالة تعتمد على الرسوم
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

                        int? createdVoucherId = null;

                        // ✅ إنشاء القسيمة فقط إذا كانت الرسوم مفعلة
                        if (isFeeEnabled)
                        {
                            createdVoucherId = CreateExamFeeVoucher(application);
                        }

                        transaction.Commit();

                        // ✅ تمرير رقم القسيمة لصفحة النجاح (ليظهر زر الطباعة)
                        return RedirectToAction("Success", new { voucherId = createdVoucherId });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "حدث خطأ أثناء حفظ الطلب: " + ex.Message);
                    }
                }
            }

            viewModel.Genders = new SelectList(db.Genders, "Id", "Name", viewModel.GenderId);
            ViewBag.MinHighSchoolScore = minHighSchool;
            ViewBag.MinBachelorScore = minBachelor;

            return View(viewModel);
        }

        // ✅ دالة مساعدة لإنشاء قسيمة الرسوم
        // ✅ دالة مساعدة لإنشاء قسيمة الرسوم (محدثة ومرنة)
        private int? CreateExamFeeVoucher(ExamApplication app)
        {
            // 1. محاولة جلب نوع الرسم المحدد من إعدادات النظام
            int? feeTypeId = null;
            var setting = db.SystemSettings.FirstOrDefault(s => s.SettingKey == "Exam_Registration_FeeTypeId");

            if (setting != null && setting.ValueInt.HasValue)
            {
                feeTypeId = setting.ValueInt.Value;
            }

            // جلب نوع الرسم بناءً على المعرف (أو البحث كخيار بديل أخير)
            var examFeeType = db.FeeTypes
                .Include(f => f.Currency)
                .Include(f => f.BankAccount) // نحتاج البنك أيضاً
                .FirstOrDefault(f => (feeTypeId != null && f.Id == feeTypeId) ||
                                     (feeTypeId == null && f.Name.Contains("امتحان القبول")));

            if (examFeeType != null)
            {
                var voucher = new PaymentVoucher
                {
                    GraduateApplicationId = null,
                    IssueDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(14),
                    TotalAmount = examFeeType.DefaultAmount,
                    Status = "صادر",
                    PaymentMethod = "بنكي", // أو "نقدي" حسب الحاجة
                    IssuedByUserName = "Online Registration System",
                    CheckNumber = app.FullName, // مؤقتاً للاسم

                    VoucherDetails = new List<VoucherDetail>
            {
                new VoucherDetail
                {
                    FeeTypeId = examFeeType.Id,
                    Amount = examFeeType.DefaultAmount,
                    // استخدام حساب البنك المربوط بنوع الرسم مباشرة
                    BankAccountId = examFeeType.BankAccountId,
                    Description = $"رسوم امتحان القبول - {app.FullName} ({app.NationalIdNumber})"
                }
            }
                };

                db.PaymentVouchers.Add(voucher);
                db.SaveChanges();

                return voucher.Id;
            }

            // تسجيل خطأ في حال لم يتم العثور على نوع الرسم
            AuditService.LogAction("Error", "ExamRegistration", "لم يتم العثور على إعداد 'Exam_Registration_FeeTypeId' لإنشاء القسيمة.");
            return null;
        }

        // ✅ دالة النجاح تستقبل رقم القسيمة
        public ActionResult Success(int? voucherId)
        {
            ViewBag.VoucherId = voucherId;
            return View();
        }

        public ActionResult PrintVoucher(int id)
        {
            var voucher = db.PaymentVouchers
                .Include(v => v.GraduateApplication)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                .Include(v => v.VoucherDetails.Select(d => d.BankAccount))
                .FirstOrDefault(v => v.Id == id);

            if (voucher == null) return HttpNotFound();

            string applicantName = voucher.CheckNumber;

            string currencySymbol = voucher.VoucherDetails.FirstOrDefault()?.FeeType?.Currency?.Symbol ?? "₪";
            string amountInWords = TafqeetHelper.ConvertToArabic(voucher.TotalAmount, currencySymbol);

            ViewBag.AmountInWords = amountInWords;
            ViewBag.CurrencySymbol = currencySymbol;

            var viewModel = new PrintVoucherViewModel
            {
                VoucherId = voucher.Id,
                TraineeName = applicantName,
                IssueDate = voucher.IssueDate,
                ExpiryDate = voucher.ExpiryDate,
                TotalAmount = voucher.TotalAmount,
                PaymentMethod = voucher.PaymentMethod,
                IssuedByUserName = voucher.IssuedByUserName,
                Details = voucher.VoucherDetails.Select(d => new VoucherPrintDetail
                {
                    FeeTypeName = d.Description ?? d.FeeType?.Name,
                    Amount = d.Amount,
                    CurrencySymbol = d.FeeType?.Currency?.Symbol ?? "",
                    BankName = d.BankAccount?.BankName ?? "",
                    AccountName = d.BankAccount?.AccountName ?? "",
                    AccountNumber = d.BankAccount?.AccountNumber ?? "",
                    Iban = d.BankAccount?.Iban ?? ""
                }).ToList()
            };

            return View(viewModel);
        }

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