using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanEdit")] // صلاحية للمدراء فقط
    public class DataExchangeController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // عرض صفحة الاستيراد والتصدير
        public ActionResult Index()
        {
            return View();
        }

        // ==========================================
        // 1. تحميل القوالب (Templates)
        // ==========================================
        public ActionResult DownloadTemplate(string type)
        {
         //   ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(type);
                List<string> headers = new List<string>();

                switch (type)
                {
                    // --- المرحلة 1: الثوابت ---
                    case "UserTypes": headers.AddRange(new[] { "الاسم العربي", "الاسم الإنجليزي" }); break;
                    case "Currencies": headers.AddRange(new[] { "اسم العملة", "الرمز" }); break;
                    case "QualificationTypes": headers.AddRange(new[] { "نوع المؤهل", "نسبة القبول" }); break;
                    case "ApplicationStatuses":
                    case "Genders":
                    case "NationalIdTypes":
                    case "ExamTypes":
                    case "AttachmentTypes":
                    case "Provinces":
                    case "PartyRoles":
                    case "ContractExemptionReasons":
                    case "ContractTypes":
                        headers.Add("الاسم");
                        break;

                    // --- المرحلة 2: المالية (تعريفات) ---
                    case "BankAccounts": headers.AddRange(new[] { "اسم البنك", "اسم الحساب", "رقم الحساب", "الآيبان", "العملة" }); break;
                    case "FeeTypes": headers.AddRange(new[] { "اسم الرسم", "القيمة", "العملة", "رقم حساب البنك", "نسبة المحامي", "نسبة النقابة" }); break;

                    // --- المرحلة 3: الخريجين ---
                    case "Graduates":
                        headers.AddRange(new[] {
                            "الاسم العربي", "الاسم الإنجليزي", "الرقم الوطني", "نوع الهوية", "الجنس", "الجنسية",
                            "تاريخ الميلاد", "مكان الميلاد", "حالة الطلب", "الرقم المتسلسل", "رقم العضوية",
                            "تاريخ بدء التدريب", "تاريخ بدء المزاولة", "ملاحظات", "رقم الجوال", "البريد الإلكتروني",
                            "المحافظة", "المدينة", "الشارع", "البناية", "رقم الوطنية", "الهاتف الأرضي",
                            "واتساب", "شخص الطوارئ", "رقم الطوارئ", "اسم البنك", "الفرع", "رقم الحساب",
                            "الآيبان", "معرف تليجرام", "الرقم الوطني للمشرف"
                        });
                        break;

                    // --- المرحلة 4: الامتحانات ---
                    case "Exams": headers.AddRange(new[] { "عنوان الامتحان", "نوع الامتحان", "وقت البدء", "وقت الانتهاء", "المدة (دقائق)", "نسبة النجاح", "الحالة المطلوبة" }); break;
                    case "ExamResults": headers.AddRange(new[] { "عنوان الامتحان", "الرقم الوطني للمتقدم", "العلامة", "النتيجة (ناجح/راسب)" }); break;

                    // --- المرحلة 5: الدفعات ---
                    case "Payments": headers.AddRange(new[] { "الرقم الوطني", "نوع الرسم", "المبلغ", "تاريخ الدفع", "رقم وصل البنك", "ملاحظات" }); break;

                    // --- المرحلة 6: العقود ---
                    case "Contracts":
                        headers.AddRange(new[] {
                            "رقم هوية المحامي", "نوع العقد", "تاريخ المعاملة", "قيمة الرسوم", "الحالة", "ملاحظات",
                            "هل معفى؟", "سبب الإعفاء",
                            "اسم الطرف الأول", "هوية الطرف الأول", "صفة الطرف الأول", "محافظة الطرف الأول",
                            "اسم الطرف الثاني", "هوية الطرف الثاني", "صفة الطرف الثاني", "محافظة الطرف الثاني"
                        });
                        break;

                    // ...
                    case "StampContractors":
                        headers.AddRange(new[] { "اسم المتعهد", "رقم الجوال", "رقم الهوية", "المحافظة", "الموقع" });
                        break;
                    case "StampBooks":
                        headers.AddRange(new[] { "الرقم التسلسلي البداية", "الرقم التسلسلي النهاية", "القيمة للطابع", "الحالة (متاح/مصروف)", "اسم المتعهد (إن وجد)" });
                        break;
                    case "StampSales":
                        headers.AddRange(new[] { "رقم الطابع", "اسم المتعهد", "رقم هوية المحامي المشتري", "تاريخ البيع", "هل تم الصرف؟ (نعم/لا)" });
                        break;
                    // ...
                    // ...
                    case "OralExamResults":
                        headers.AddRange(new[] { "الرقم الوطني", "تاريخ الامتحان", "النتيجة (ناجح/راسب)", "الدرجة", "ملاحظات" });
                        break;
                    case "LegalResearch":
                        headers.AddRange(new[] { "الرقم الوطني", "عنوان البحث", "تاريخ التقديم", "حالة البحث (مقبول/مرفوض)" });
                        break;
                    case "Loans":
                        headers.AddRange(new[] { "الرقم الوطني", "مبلغ القرض", "عدد الأقساط", "تاريخ البدء", "الحالة (مسدد/قائم)", "ملاحظات" });
                        break;
                    // ...
                    default: return HttpNotFound();
                }

                // كتابة وتنسيق العناوين
                for (int i = 0; i < headers.Count; i++) worksheet.Cells[1, i + 1].Value = headers[i];

                using (var range = worksheet.Cells[1, 1, 1, headers.Count])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                }
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Template_{type}.xlsx");
            }
        }

        // ==========================================
        // 2. التصدير (Export)
        // ==========================================
        // ==========================================
        // 2. التصدير (Export) - المحدث الشامل
        // ==========================================
        [HttpPost] // جعلناها Post لأننا نستخدم Form الآن
        public ActionResult ExportData(string type)
        {
          //  ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                switch (type)
                {
                    // --- جداول بسيطة (Lookups) ---
                    case "UserTypes": ExportLookupSheet(package, type, new[] { "الاسم العربي", "الاسم الإنجليزي" }); break;
                    case "Currencies": ExportLookupSheet(package, type, new[] { "اسم العملة", "الرمز" }); break;
                    case "QualificationTypes": ExportLookupSheet(package, type, new[] { "نوع المؤهل", "نسبة القبول" }); break;

                    case "ApplicationStatuses":
                    case "Genders":
                    case "NationalIdTypes":
                    case "ExamTypes":
                    case "AttachmentTypes":
                    case "Provinces":
                    case "PartyRoles":
                    case "ContractExemptionReasons":
                    case "ContractTypes":
                        ExportLookupSheet(package, type, new[] { "الاسم" });
                        break;

                    // --- جداول متقدمة ---
                    case "BankAccounts": ExportBankAccounts(package); break;
                    case "FeeTypes": ExportFeeTypes(package); break;
                    case "Graduates": ExportGraduatesSheet(package); break; // (موجودة سابقاً)
                    case "Exams": ExportExams(package); break;
                    case "ExamResults": ExportExamResults(package); break;
                    case "Payments": ExportPayments(package); break;
                    case "Contracts": ExportContracts(package); break;

                    // --- الطوابع ---
                    case "StampContractors": ExportStampContractors(package); break;
                    case "StampBooks": ExportStampBooks(package); break;
                    case "StampSales": ExportStampSales(package); break;

                    // --- أخرى ---
                    case "OralExamResults": ExportOralResults(package); break;
                    case "LegalResearch": ExportResearch(package); break;
                    case "Loans": ExportLoans(package); break;

                    default: return new HttpStatusCodeResult(400, "Invalid Type");
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{type}_Export_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }

        private void ExportGraduatesSheet(ExcelPackage package)
        {
            var worksheet = package.Workbook.Worksheets.Add("الخريجين");
            // العناوين مطابقة تماماً للقالب
            string[] headers = {
                "الاسم العربي", "الاسم الإنجليزي", "الرقم الوطني", "نوع الهوية", "الجنس", "الجنسية",
                "تاريخ الميلاد", "مكان الميلاد", "حالة الطلب", "الرقم المتسلسل", "رقم العضوية",
                "تاريخ بدء التدريب", "تاريخ بدء المزاولة", "ملاحظات", "رقم الجوال", "البريد الإلكتروني",
                "المحافظة", "المدينة", "الشارع", "البناية", "رقم الوطنية", "الهاتف الأرضي",
                "واتساب", "شخص الطوارئ", "رقم الطوارئ", "اسم البنك", "الفرع", "رقم الحساب",
                "الآيبان", "معرف تليجرام", "الرقم الوطني للمشرف"
            };

            for (int i = 0; i < headers.Length; i++) worksheet.Cells[1, i + 1].Value = headers[i];

            var data = db.GraduateApplications.AsNoTracking()
                .Include(g => g.Gender).Include(g => g.ApplicationStatus).Include(g => g.NationalIdType)
                .Include(g => g.ContactInfo).Include(g => g.Supervisor).ToList();

            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cells[row, 1].Value = item.ArabicName;
                worksheet.Cells[row, 2].Value = item.EnglishName;
                worksheet.Cells[row, 3].Value = item.NationalIdNumber;
                worksheet.Cells[row, 4].Value = item.NationalIdType?.Name;
                worksheet.Cells[row, 5].Value = item.Gender?.Name;
                worksheet.Cells[row, 6].Value = item.Nationality;
                worksheet.Cells[row, 7].Value = item.BirthDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 8].Value = item.BirthPlace;
                worksheet.Cells[row, 9].Value = item.ApplicationStatus?.Name;
                worksheet.Cells[row, 10].Value = item.TraineeSerialNo;
                worksheet.Cells[row, 11].Value = item.MembershipId;
                worksheet.Cells[row, 12].Value = item.TrainingStartDate?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 13].Value = item.PracticeStartDate?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 14].Value = item.Notes;
                // Contact Info
                worksheet.Cells[row, 15].Value = item.ContactInfo?.MobileNumber;
                worksheet.Cells[row, 16].Value = item.ContactInfo?.Email;
                worksheet.Cells[row, 17].Value = item.ContactInfo?.Governorate;
                worksheet.Cells[row, 18].Value = item.ContactInfo?.City;
                worksheet.Cells[row, 19].Value = item.ContactInfo?.Street;
                worksheet.Cells[row, 20].Value = item.ContactInfo?.BuildingNumber;
                worksheet.Cells[row, 21].Value = item.ContactInfo?.NationalMobileNumber;
                worksheet.Cells[row, 22].Value = item.ContactInfo?.HomePhoneNumber;
                worksheet.Cells[row, 23].Value = item.ContactInfo?.WhatsAppNumber;
                worksheet.Cells[row, 24].Value = item.ContactInfo?.EmergencyContactPerson;
                worksheet.Cells[row, 25].Value = item.ContactInfo?.EmergencyContactNumber;
                // Bank Info
                worksheet.Cells[row, 26].Value = item.BankName;
                worksheet.Cells[row, 27].Value = item.BankBranch;
                worksheet.Cells[row, 28].Value = item.AccountNumber;
                worksheet.Cells[row, 29].Value = item.Iban;
                // Other
                worksheet.Cells[row, 30].Value = item.TelegramChatId;
                worksheet.Cells[row, 31].Value = item.Supervisor?.NationalIdNumber;
                row++;
            }
            worksheet.Cells.AutoFitColumns();
        }

        // ==========================================
        // 3. الاستيراد (Import Main)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ImportData(ImportViewModel model)
        {
            if (!ModelState.IsValid || model.File == null || model.File.ContentLength == 0)
            {
                TempData["ErrorMessage"] = "يرجى اختيار ملف صالح.";
                return RedirectToAction("Index");
            }

          ///  ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try
            {
                using (var package = new ExcelPackage(model.File.InputStream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;

                    switch (model.EntityType)
                    {
                        case "UserTypes":
                        case "ApplicationStatuses":
                        case "Genders":
                        case "NationalIdTypes":
                        case "ExamTypes":
                        case "Currencies":
                        case "AttachmentTypes":
                        case "QualificationTypes":
                        case "Provinces":
                        case "PartyRoles":
                        case "ContractExemptionReasons":
                        case "ContractTypes":
                            ImportLookupsLogic(worksheet, model.EntityType, rowCount);
                            break;

                        case "BankAccounts":
                        case "FeeTypes":
                            ImportLookupsLogic(worksheet, model.EntityType, rowCount);
                            break;

                        case "Graduates":
                            ImportGraduatesLogic(worksheet, rowCount);
                            break;

                        case "Exams":
                            ImportExamsLogic(worksheet, rowCount);
                            break;
                        case "ExamResults":
                            ImportExamResultsLogic(worksheet, rowCount);
                            break;

                        case "Payments":
                            ImportPaymentsLogic(worksheet, rowCount);
                            break;

                        case "Contracts":
                            ImportContractsLogic(worksheet, rowCount);
                            break;
                        // ...
                        case "StampContractors":
                            ImportContractorsLogic(worksheet, rowCount);
                            break;
                        case "StampBooks":
                            ImportStampBooksLogic(worksheet, rowCount);
                            break;
                        case "StampSales":
                            ImportStampSalesLogic(worksheet, rowCount);
                            break;
                        // ...
                        // ...
                        case "OralExamResults":
                            ImportOralExamResultsLogic(worksheet, rowCount);
                            break;
                        case "LegalResearch":
                            ImportLegalResearchLogic(worksheet, rowCount);
                            break;
                        case "Loans":
                            ImportLoansLogic(worksheet, rowCount);
                            break;
                        // ...

                        default:
                            TempData["ErrorMessage"] = "نوع البيانات غير معروف.";
                            return RedirectToAction("Index");
                    }
                }
                TempData["SuccessMessage"] = "تم استيراد البيانات بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "خطأ: " + ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
            }

            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. دوال المنطق (Logic Methods)
        // ==========================================

        private void ImportLookupsLogic(ExcelWorksheet sheet, string entityType, int rowCount)
        {
            for (int row = 2; row <= rowCount; row++)
            {
                string name = sheet.Cells[row, 1].Text.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                if (entityType == "UserTypes" && !db.UserTypes.Any(x => x.NameArabic == name))
                    db.UserTypes.Add(new UserTypeModel { NameArabic = name, NameEnglish = sheet.Cells[row, 2].Text });

                else if (entityType == "ApplicationStatuses" && !db.ApplicationStatuses.Any(x => x.Name == name))
                    db.ApplicationStatuses.Add(new ApplicationStatus { Name = name });

                else if (entityType == "Genders" && !db.Genders.Any(x => x.Name == name))
                    db.Genders.Add(new Gender { Name = name });

                else if (entityType == "NationalIdTypes" && !db.NationalIdTypes.Any(x => x.Name == name))
                    db.NationalIdTypes.Add(new NationalIdType { Name = name });

                else if (entityType == "Provinces" && !db.Provinces.Any(x => x.Name == name))
                    db.Provinces.Add(new Province { Name = name });

                else if (entityType == "PartyRoles" && !db.PartyRoles.Any(x => x.Name == name))
                    db.PartyRoles.Add(new PartyRole { Name = name });

                else if (entityType == "ContractExemptionReasons" && !db.ContractExemptionReasons.Any(x => x.Reason == name))
                    db.ContractExemptionReasons.Add(new ContractExemptionReason { Reason = name });

                else if (entityType == "ContractTypes" && !db.ContractTypes.Any(x => x.Name == name))
                    db.ContractTypes.Add(new ContractType { Name = name });

                else if (entityType == "Currencies" && !db.Currencies.Any(x => x.Name == name))
                    db.Currencies.Add(new Currency { Name = name, Symbol = sheet.Cells[row, 2].Text });

                else if (entityType == "QualificationTypes" && !db.QualificationTypes.Any(x => x.Name == name))
                {
                    double.TryParse(sheet.Cells[row, 2].Text, out double min);
                    db.QualificationTypes.Add(new QualificationType { Name = name, MinimumAcceptancePercentage = min });
                }

                // --- المالية ---
                else if (entityType == "BankAccounts")
                {
                    string accNum = sheet.Cells[row, 3].Text.Trim();
                    if (!string.IsNullOrEmpty(accNum) && !db.BankAccounts.Any(x => x.AccountNumber == accNum))
                    {
                        var curName = sheet.Cells[row, 5].Text;
                        var cur = db.Currencies.FirstOrDefault(c => c.Name == curName) ?? db.Currencies.FirstOrDefault();
                        db.BankAccounts.Add(new BankAccount { BankName = name, AccountName = sheet.Cells[row, 2].Text, AccountNumber = accNum, Iban = sheet.Cells[row, 4].Text, CurrencyId = cur?.Id ?? 1, IsActive = true });
                    }
                }
                else if (entityType == "FeeTypes")
                {
                    if (!db.FeeTypes.Any(x => x.Name == name))
                    {
                        decimal.TryParse(sheet.Cells[row, 2].Text, out decimal amt);
                        string curName = sheet.Cells[row, 3].Text;
                        string accNum = sheet.Cells[row, 4].Text;
                        decimal.TryParse(sheet.Cells[row, 5].Text, out decimal lPer);
                        decimal.TryParse(sheet.Cells[row, 6].Text, out decimal bPer);

                        var cur = db.Currencies.FirstOrDefault(c => c.Name == curName) ?? db.Currencies.FirstOrDefault();
                        var bank = db.BankAccounts.FirstOrDefault(b => b.AccountNumber == accNum) ?? db.BankAccounts.FirstOrDefault();

                        db.FeeTypes.Add(new FeeType { Name = name, DefaultAmount = amt, CurrencyId = cur?.Id ?? 1, BankAccountId = bank?.Id ?? 1, LawyerPercentage = lPer, BarSharePercentage = bPer == 0 ? 1 : bPer, IsActive = true });
                    }
                }
            }
            db.SaveChanges();
        }

        private void ImportGraduatesLogic(ExcelWorksheet sheet, int rowCount)
        {
            var genders = db.Genders.ToList();
            var statuses = db.ApplicationStatuses.ToList();
            var idTypes = db.NationalIdTypes.ToList();
            var userTypes = db.UserTypes.ToList();
            var defaultIdType = idTypes.FirstOrDefault()?.Id ?? 1;
            var defaultUserType = userTypes.FirstOrDefault(u => u.NameEnglish == "Graduate")?.Id ?? 1;
            var defaultStatus = statuses.FirstOrDefault(s => s.Name == "طلب جديد");
            var importedNationalIds = new List<string>();

            // الدورة 1: إنشاء المستخدمين والملفات
            for (int row = 2; row <= rowCount; row++)
            {
                string nameAr = sheet.Cells[row, 1].Text.Trim();
                string natId = sheet.Cells[row, 3].Text.Trim();
                if (string.IsNullOrEmpty(natId) || string.IsNullOrEmpty(nameAr) || db.Users.Any(u => u.Username == natId)) continue;

                var gender = genders.FirstOrDefault(g => g.Name.Contains(sheet.Cells[row, 5].Text)) ?? genders.First();
                var status = statuses.FirstOrDefault(s => s.Name.Contains(sheet.Cells[row, 9].Text)) ?? defaultStatus;
                DateTime.TryParse(sheet.Cells[row, 7].Text, out DateTime birth);
                if (birth == DateTime.MinValue) birth = new DateTime(1990, 1, 1);

                DateTime? tStart = null; if (DateTime.TryParse(sheet.Cells[row, 12].Text, out DateTime d1)) tStart = d1;
                DateTime? pStart = null; if (DateTime.TryParse(sheet.Cells[row, 13].Text, out DateTime d2)) pStart = d2;
                long.TryParse(sheet.Cells[row, 30].Text, out long tId);

                var user = new UserModel { FullNameArabic = nameAr, Username = natId, IdentificationNumber = natId, Email = $"{natId}@sys.local", UserTypeId = defaultUserType, IsActive = true, HashedPassword = PasswordHelper.HashPassword("User@1234") };
                db.Users.Add(user);

                var app = new GraduateApplication
                {
                    ArabicName = nameAr,
                    EnglishName = sheet.Cells[row, 2].Text,
                    NationalIdNumber = natId,
                    NationalIdTypeId = defaultIdType,
                    GenderId = gender.Id,
                    Nationality = sheet.Cells[row, 6].Text,
                    BirthDate = birth,
                    BirthPlace = sheet.Cells[row, 8].Text,
                    ApplicationStatusId = status.Id,
                    TraineeSerialNo = sheet.Cells[row, 10].Text,
                    MembershipId = sheet.Cells[row, 11].Text,
                    TrainingStartDate = tStart,
                    PracticeStartDate = pStart,
                    Notes = sheet.Cells[row, 14].Text,
                    BankName = sheet.Cells[row, 26].Text,
                    BankBranch = sheet.Cells[row, 27].Text,
                    AccountNumber = sheet.Cells[row, 28].Text,
                    Iban = sheet.Cells[row, 29].Text,
                    TelegramChatId = tId,
                    User = user,
                    SubmissionDate = DateTime.Now,
                    ContactInfo = new ContactInfo { MobileNumber = sheet.Cells[row, 15].Text, Email = sheet.Cells[row, 16].Text, Governorate = sheet.Cells[row, 17].Text, City = sheet.Cells[row, 18].Text, Street = sheet.Cells[row, 19].Text, BuildingNumber = sheet.Cells[row, 20].Text }
                };
                db.GraduateApplications.Add(app);
                importedNationalIds.Add(natId);
            }
            db.SaveChanges();

            // الدورة 2: ربط المشرفين
            for (int row = 2; row <= rowCount; row++)
            {
                string traineeId = sheet.Cells[row, 3].Text.Trim();
                string superId = sheet.Cells[row, 31].Text.Trim();
                if (!string.IsNullOrEmpty(superId))
                {
                    var trainee = db.GraduateApplications.FirstOrDefault(g => g.NationalIdNumber == traineeId);
                    var super = db.GraduateApplications.FirstOrDefault(g => g.NationalIdNumber == superId);
                    if (trainee != null && super != null && trainee.Id != super.Id) trainee.SupervisorId = super.Id;
                }
            }
            db.SaveChanges();
        }

        private void ImportExamsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var types = db.ExamTypes.ToList();
            for (int row = 2; row <= rowCount; row++)
            {
                string title = sheet.Cells[row, 1].Text.Trim();
                if (string.IsNullOrEmpty(title) || db.Exams.Any(e => e.Title == title)) continue;
                var type = types.FirstOrDefault(t => t.Name.Contains(sheet.Cells[row, 2].Text)) ?? types.First();
                DateTime.TryParse(sheet.Cells[row, 3].Text, out DateTime start);
                DateTime.TryParse(sheet.Cells[row, 4].Text, out DateTime end);
                int.TryParse(sheet.Cells[row, 5].Text, out int dur);
                double.TryParse(sheet.Cells[row, 6].Text, out double pass);
                db.Exams.Add(new Exam { Title = title, ExamTypeId = type.Id, StartTime = start, EndTime = end, DurationInMinutes = dur > 0 ? dur : 120, PassingPercentage = pass > 0 ? pass : 50, IsActive = false });
            }
            db.SaveChanges();
        }

        private void ImportExamResultsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var exams = db.Exams.ToList();
            var grads = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();

            for (int row = 2; row <= rowCount; row++)
            {
                string exTitle = sheet.Cells[row, 1].Text;
                string natId = sheet.Cells[row, 2].Text;
                if (string.IsNullOrEmpty(natId)) continue;

                var exam = exams.FirstOrDefault(e => e.Title == exTitle);
                var grad = grads.FirstOrDefault(g => g.NationalIdNumber == natId);
                if (exam == null || grad == null) continue;
                if (db.ExamEnrollments.Any(e => e.ExamId == exam.Id && e.GraduateApplicationId == grad.Id)) continue;

                double.TryParse(sheet.Cells[row, 3].Text, out double score);
                db.ExamEnrollments.Add(new ExamEnrollment { ExamId = exam.Id, GraduateApplicationId = grad.Id, Score = score, Result = sheet.Cells[row, 4].Text });
            }
            db.SaveChanges();
        }

        private void ImportPaymentsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var fees = db.FeeTypes.ToList();
            var defBank = db.BankAccounts.FirstOrDefault();
            if (defBank == null) throw new Exception("لا يوجد حسابات بنكية معرفة.");
            var grads = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();
            var admin = db.Users.FirstOrDefault();

            for (int row = 2; row <= rowCount; row++)
            {
                string natId = sheet.Cells[row, 1].Text;
                if (string.IsNullOrEmpty(natId)) continue;
                var grad = grads.FirstOrDefault(g => g.NationalIdNumber == natId);
                if (grad == null) continue;

                string feeName = sheet.Cells[row, 2].Text;
                var fee = fees.FirstOrDefault(f => f.Name.Contains(feeName));
                decimal.TryParse(sheet.Cells[row, 3].Text, out decimal amt);
                DateTime.TryParse(sheet.Cells[row, 4].Text, out DateTime date);
                if (date == DateTime.MinValue) date = DateTime.Now;

                var voucher = new PaymentVoucher { GraduateApplicationId = grad.Id, TotalAmount = amt, IssueDate = date, ExpiryDate = date.AddMonths(1), Status = "مسدد", PaymentMethod = "بنكي", IssuedByUserId = admin.Id, IssuedByUserName = "System" };
                voucher.VoucherDetails = new List<VoucherDetail> { new VoucherDetail { FeeTypeId = fee?.Id ?? fees.First().Id, BankAccountId = fee?.BankAccountId ?? defBank.Id, Amount = amt, Description = feeName } };
                db.PaymentVouchers.Add(voucher);
                db.SaveChanges();

                // إيصال قبض
                int year = date.Year;
                var lastSeq = db.Receipts.Where(r => r.Year == year).Max(r => (int?)r.SequenceNumber) ?? 0;
                db.Receipts.Add(new Receipt { Id = voucher.Id, Year = year, SequenceNumber = lastSeq + 1, BankPaymentDate = date, BankReceiptNumber = sheet.Cells[row, 5].Text, CreationDate = DateTime.Now, Notes = sheet.Cells[row, 6].Text, IssuedByUserId = admin.Id, IssuedByUserName = "System" });
                db.SaveChanges();
            }
        }

        private void ImportContractsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var lawyers = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();
            var types = db.ContractTypes.ToList();
            var provs = db.Provinces.ToList();
            var roles = db.PartyRoles.ToList();
            var admin = db.Users.FirstOrDefault();

            for (int row = 2; row <= rowCount; row++)
            {
                string lawId = sheet.Cells[row, 1].Text;
                var lawyer = lawyers.FirstOrDefault(l => l.NationalIdNumber == lawId);
                if (lawyer == null) continue;

                var cType = types.FirstOrDefault(c => c.Name.Contains(sheet.Cells[row, 2].Text)) ?? types.First();
                DateTime.TryParse(sheet.Cells[row, 3].Text, out DateTime date);
                decimal.TryParse(sheet.Cells[row, 4].Text, out decimal fee);

                var trans = new ContractTransaction { LawyerId = lawyer.Id, ContractTypeId = cType.Id, TransactionDate = date == DateTime.MinValue ? DateTime.Now : date, FinalFee = fee, Status = "مكتمل", Notes = sheet.Cells[row, 6].Text, EmployeeId = admin.Id, CertificationDate = date, IsActingForSelf = true };
                db.ContractTransactions.Add(trans);
                db.SaveChanges();

                // أطراف (مثال لطرفين فقط للتبسيط)
                var p1Prov = provs.FirstOrDefault(p => p.Name == sheet.Cells[row, 12].Text) ?? provs.First();
                var p1Role = roles.FirstOrDefault(r => r.Name == sheet.Cells[row, 11].Text) ?? roles.First();
                if (!string.IsNullOrEmpty(sheet.Cells[row, 9].Text))
                    db.TransactionParties.Add(new TransactionParty { ContractTransactionId = trans.Id, PartyType = 1, PartyName = sheet.Cells[row, 9].Text, PartyIDNumber = sheet.Cells[row, 10].Text, PartyRoleId = p1Role.Id, ProvinceId = p1Prov.Id });

                var p2Prov = provs.FirstOrDefault(p => p.Name == sheet.Cells[row, 16].Text) ?? provs.First();
                var p2Role = roles.FirstOrDefault(r => r.Name == sheet.Cells[row, 15].Text) ?? roles.First();
                if (!string.IsNullOrEmpty(sheet.Cells[row, 13].Text))
                    db.TransactionParties.Add(new TransactionParty { ContractTransactionId = trans.Id, PartyType = 2, PartyName = sheet.Cells[row, 13].Text, PartyIDNumber = sheet.Cells[row, 14].Text, PartyRoleId = p2Role.Id, ProvinceId = p2Prov.Id });

                db.SaveChanges();
            }
        }

        private void ImportContractorsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var existingContractors = db.StampContractors.Select(c => c.Name).ToList();

            for (int row = 2; row <= rowCount; row++)
            {
                string name = sheet.Cells[row, 1].Text.Trim();
                if (string.IsNullOrEmpty(name) || existingContractors.Contains(name)) continue;

                db.StampContractors.Add(new StampContractor
                {
                    Name = name,
                    Phone = sheet.Cells[row, 2].Text.Trim(),
                    NationalId = sheet.Cells[row, 3].Text.Trim(),
                    Governorate = sheet.Cells[row, 4].Text.Trim(),
                    Location = sheet.Cells[row, 5].Text.Trim(),
                    IsActive = true
                });
                existingContractors.Add(name);
            }
            db.SaveChanges();
        }

        private void ImportStampBooksLogic(ExcelWorksheet sheet, int rowCount)
        {
            // تحميل المتعهدين للربط (إذا كان الدفتر مصروفاً)
            var contractors = db.StampContractors.ToList();
            // تحميل المستخدم الافتراضي (المدخل)
            var admin = db.Users.FirstOrDefault();

            for (int row = 2; row <= rowCount; row++)
            {
                long.TryParse(sheet.Cells[row, 1].Text, out long startSerial);
                long.TryParse(sheet.Cells[row, 2].Text, out long endSerial);
                decimal.TryParse(sheet.Cells[row, 3].Text, out decimal value);
                string status = sheet.Cells[row, 4].Text.Trim(); // (متاح/مصروف/منتهي)
                string contractorName = sheet.Cells[row, 5].Text.Trim(); // اسم المتعهد إذا مصروف

                if (startSerial == 0 || endSerial == 0 || endSerial < startSerial) continue;

                // التحقق من عدم وجود تداخل في الأرقام التسلسلية
                if (db.Stamps.Any(s => s.SerialNumber >= startSerial && s.SerialNumber <= endSerial)) continue;

                int quantity = (int)(endSerial - startSerial + 1);

                // 1. إنشاء الدفتر
                var book = new StampBook
                {
                    StartSerial = startSerial,
                    EndSerial = endSerial,
                    Quantity = quantity,
                    ValuePerStamp = value,
                    DateAdded = DateTime.Now,
                    CouncilDecisionRef = "أرشيف مستورد",
                    Status = status
                };
                db.StampBooks.Add(book);
                db.SaveChanges(); // نحفظ الدفتر للحصول على ID

                // 2. إنشاء الطوابع الفردية (Batch Insert)
                var stamps = new List<Stamp>();
                var contractor = contractors.FirstOrDefault(c => c.Name == contractorName);

                for (long serial = startSerial; serial <= endSerial; serial++)
                {
                    stamps.Add(new Stamp
                    {
                        StampBookId = book.Id,
                        SerialNumber = serial,
                        Value = value,
                        Status = (status == "متاح") ? "في المخزن" : (status == "مصروف" ? "مع المتعهد" : "مباع"),
                        ContractorId = contractor?.Id, // إذا كان الدفتر مصروفاً لمتعهد
                        IsPaidToLawyer = false // الافتراضي
                    });
                }
                db.Stamps.AddRange(stamps);

                // إذا كان مصروفاً، نسجل حركة الصرف (Issuance)
                if (contractor != null)
                {
                    db.StampBookIssuances.Add(new StampBookIssuance
                    {
                        ContractorId = contractor.Id,
                        StampBookId = book.Id,
                        IssuanceDate = DateTime.Now,
                        PaymentVoucherId = 0 // (وهمي للأرشيف، أو يمكن ربطه بسند قديم)
                    });
                }

                db.SaveChanges();
            }
        }

        private void ImportStampSalesLogic(ExcelWorksheet sheet, int rowCount)
        {
            var lawyers = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber, g.MembershipId, g.ArabicName, g.BankName, g.AccountNumber }).ToList();
            var contractors = db.StampContractors.ToList();
            var admin = db.Users.FirstOrDefault();

            for (int row = 2; row <= rowCount; row++)
            {
                long.TryParse(sheet.Cells[row, 1].Text, out long serial); // رقم الطابع
                string contractorName = sheet.Cells[row, 2].Text.Trim();
                string lawyerIdNum = sheet.Cells[row, 3].Text.Trim(); // هوية المحامي
                string saleDateTxt = sheet.Cells[row, 4].Text.Trim();
                bool isPaid = (sheet.Cells[row, 5].Text.Trim() == "نعم"); // هل تم صرف الحصة؟

                // 1. البحث عن الطابع
                var stamp = db.Stamps.FirstOrDefault(s => s.SerialNumber == serial);
                if (stamp == null) continue; // لا يمكن بيع طابع غير موجود

                // 2. البحث عن المحامي والمتعهد
                var lawyer = lawyers.FirstOrDefault(l => l.NationalIdNumber == lawyerIdNum);
                var contractor = contractors.FirstOrDefault(c => c.Name == contractorName);
                if (contractor == null) continue;

                DateTime.TryParse(saleDateTxt, out DateTime date);
                if (date == DateTime.MinValue) date = DateTime.Now;

                // 3. تحديث الطابع الأصلي
                stamp.Status = "مباع";
                stamp.ContractorId = contractor.Id;
                stamp.SoldToLawyerId = lawyer?.Id;
                stamp.DateSold = date;
                stamp.IsPaidToLawyer = isPaid;

                // 4. إنشاء سجل البيع (StampSale)
                var sale = new StampSale
                {
                    StampId = stamp.Id,
                    ContractorId = contractor.Id,
                    SaleDate = date,
                    GraduateApplicationId = lawyer?.Id,
                    LawyerMembershipId = lawyer?.MembershipId ?? "غير معروف",
                    LawyerName = lawyer?.ArabicName ?? "غير معروف",
                    StampValue = stamp.Value,
                    // حصة المحامي (افتراضياً 40% مثلاً، أو يمكن قراءتها من الإكسل)
                    AmountToLawyer = stamp.Value * 0.40m,
                    AmountToBar = stamp.Value * 0.60m,
                    IsPaidToLawyer = isPaid,
                    BankSendDate = isPaid ? (DateTime?)DateTime.Now : null,
                    RecordedByUserId = admin.Id,
                    RecordedByUserName = "System",
                    // بيانات بنك المحامي
                    LawyerBankName = lawyer?.BankName,
                    LawyerAccountNumber = lawyer?.AccountNumber
                };

                db.StampSales.Add(sale);
            }
            db.SaveChanges();
        }

        private void ImportOralExamResultsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var trainees = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();

            // للتبسيط، إذا لم توجد لجان، ننشئ لجنة افتراضية للأرشيف
            var defaultCommittee = db.OralExamCommittees.FirstOrDefault(c => c.CommitteeName == "لجنة الأرشيف المستورد");
            if (defaultCommittee == null)
            {
                defaultCommittee = new OralExamCommittee { CommitteeName = "لجنة الأرشيف المستورد", FormationDate = DateTime.Now, IsActive = false };
                db.OralExamCommittees.Add(defaultCommittee);
                db.SaveChanges();
            }

            for (int row = 2; row <= rowCount; row++)
            {
                string nationalId = sheet.Cells[row, 1].Text.Trim();
                string dateTxt = sheet.Cells[row, 2].Text.Trim();
                string result = sheet.Cells[row, 3].Text.Trim(); // ناجح/راسب
                string scoreTxt = sheet.Cells[row, 4].Text.Trim();
                string notes = sheet.Cells[row, 5].Text.Trim();

                var trainee = trainees.FirstOrDefault(t => t.NationalIdNumber == nationalId);
                if (trainee == null) continue;

                // منع التكرار
                if (db.OralExamEnrollments.Any(e => e.GraduateApplicationId == trainee.Id && e.OralExamCommitteeId == defaultCommittee.Id)) continue;

                DateTime.TryParse(dateTxt, out DateTime examDate);
                if (examDate == DateTime.MinValue) examDate = DateTime.Now;

                double? score = null;
                if (double.TryParse(scoreTxt, out double s)) score = s;

                var enrollment = new OralExamEnrollment
                {
                    GraduateApplicationId = trainee.Id,
                    OralExamCommitteeId = defaultCommittee.Id,
                    ExamDate = examDate,
                    Result = !string.IsNullOrEmpty(result) ? result : "ناجح",
                    Score = score,
                    Notes = notes + " (استيراد)"
                };

                db.OralExamEnrollments.Add(enrollment);
            }
            db.SaveChanges();
        }

        private void ImportLegalResearchLogic(ExcelWorksheet sheet, int rowCount)
        {
            var trainees = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();

            for (int row = 2; row <= rowCount; row++)
            {
                string nationalId = sheet.Cells[row, 1].Text.Trim();
                string title = sheet.Cells[row, 2].Text.Trim();
                string dateTxt = sheet.Cells[row, 3].Text.Trim();
                string status = sheet.Cells[row, 4].Text.Trim(); // مقبول/مرفوض

                var trainee = trainees.FirstOrDefault(t => t.NationalIdNumber == nationalId);
                if (trainee == null) continue;

                // منع التكرار (للمتدرب الواحد)
                if (db.LegalResearches.Any(r => r.GraduateApplicationId == trainee.Id && r.Title == title)) continue;

                DateTime.TryParse(dateTxt, out DateTime subDate);
                if (subDate == DateTime.MinValue) subDate = DateTime.Now;

                var research = new LegalResearch
                {
                    GraduateApplicationId = trainee.Id,
                    Title = !string.IsNullOrEmpty(title) ? title : "بحث قانوني (أرشيف)",
                    SubmissionDate = subDate,
                    Status = !string.IsNullOrEmpty(status) ? status : "مقبول",
                    FinalDocumentPath = null, // لا يمكن استيراد الملفات
                    DiscussionCommitteeId = null // يمكن تطويره لربط لجنة
                };

                db.LegalResearches.Add(research);
            }
            db.SaveChanges();
        }

        private void ImportLoansLogic(ExcelWorksheet sheet, int rowCount)
        {
            var lawyers = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();
            var loanTypes = db.LoanTypes.ToList();
            // إنشاء نوع قرض افتراضي إذا لم يوجد
            var defaultLoanType = loanTypes.FirstOrDefault();
            if (defaultLoanType == null)
            {
                var bank = db.BankAccounts.FirstOrDefault();
                if (bank == null) throw new Exception("يجب تعريف حساب بنكي واحد على الأقل.");
                defaultLoanType = new LoanType { Name = "قرض أرشيف", BankAccountForRepaymentId = bank.Id, MaxAmount = 10000, MaxInstallments = 24 };
                db.LoanTypes.Add(defaultLoanType);
                db.SaveChanges();
            }

            for (int row = 2; row <= rowCount; row++)
            {
                string nationalId = sheet.Cells[row, 1].Text.Trim();
                string amountTxt = sheet.Cells[row, 2].Text.Trim();
                string installmentsCountTxt = sheet.Cells[row, 3].Text.Trim();
                string startDateTxt = sheet.Cells[row, 4].Text.Trim();
                string status = sheet.Cells[row, 5].Text.Trim(); // مسدد/قائم
                string notes = sheet.Cells[row, 6].Text.Trim();

                var lawyer = lawyers.FirstOrDefault(l => l.NationalIdNumber == nationalId);
                if (lawyer == null) continue;

                decimal.TryParse(amountTxt, out decimal amount);
                int.TryParse(installmentsCountTxt, out int count);
                if (count == 0) count = 10; // افتراضي

                DateTime.TryParse(startDateTxt, out DateTime start);
                if (start == DateTime.MinValue) start = DateTime.Now;

                decimal installmentAmount = amount / count;

                // إنشاء طلب القرض
                var loan = new LoanApplication
                {
                    LawyerId = lawyer.Id,
                    LoanTypeId = defaultLoanType.Id,
                    Amount = amount,
                    InstallmentCount = count,
                    InstallmentAmount = installmentAmount,
                    StartDate = start,
                    ApplicationDate = start,
                    Status = status,
                    IsDisbursed = true, // نفترض أنه مصروف لأنه أرشيف
                    DisbursementDate = start,
                    Notes = notes + " (استيراد)"
                };
                db.LoanApplications.Add(loan);
                db.SaveChanges(); // للحصول على ID

                // إنشاء الأقساط (Installments)
                var installments = new List<LoanInstallment>();
                for (int i = 1; i <= count; i++)
                {
                    installments.Add(new LoanInstallment
                    {
                        LoanApplicationId = loan.Id,
                        InstallmentNumber = i,
                        DueDate = start.AddMonths(i - 1),
                        Amount = installmentAmount,
                        Status = (status == "مكتمل" || status == "مسدد") ? "مدفوع" : "مستحق" // تبسيط للحالة
                    });
                }
                db.LoanInstallments.AddRange(installments);
                db.SaveChanges();
            }
        }

        // 1. دالة عامة للجداول البسيطة (Lookups)
        // 1. دالة عامة للجداول البسيطة (Lookups)
        private void ExportLookupSheet(ExcelPackage package, string type, string[] headers)
        {
            var ws = package.Workbook.Worksheets.Add(type);
            for (int i = 0; i < headers.Length; i++) ws.Cells[1, i + 1].Value = headers[i];
            ws.Cells[1, 1, 1, headers.Length].Style.Font.Bold = true;

            // --- التصحيح: تعريف r مرة واحدة هنا ---
            int r = 2;

            if (type == "UserTypes")
            {
                foreach (var x in db.UserTypes) { ws.Cells[r, 1].Value = x.NameArabic; ws.Cells[r, 2].Value = x.NameEnglish; r++; }
            }
            else if (type == "Currencies")
            {
                foreach (var x in db.Currencies) { ws.Cells[r, 1].Value = x.Name; ws.Cells[r, 2].Value = x.Symbol; r++; }
            }
            else if (type == "QualificationTypes")
            {
                foreach (var x in db.QualificationTypes) { ws.Cells[r, 1].Value = x.Name; ws.Cells[r, 2].Value = x.MinimumAcceptancePercentage; r++; }
            }
            // الجداول ذات الاسم الواحد
            else if (type == "ApplicationStatuses")
            {
                foreach (var x in db.ApplicationStatuses) { ws.Cells[r, 1].Value = x.Name; r++; }
            }
            else if (type == "Genders")
            {
                foreach (var x in db.Genders) { ws.Cells[r, 1].Value = x.Name; r++; }
            }
            else if (type == "NationalIdTypes")
            {
                foreach (var x in db.NationalIdTypes) { ws.Cells[r, 1].Value = x.Name; r++; }
            }
            else if (type == "ExamTypes")
            {
                foreach (var x in db.ExamTypes) { ws.Cells[r, 1].Value = x.Name; r++; }
            }
            else if (type == "AttachmentTypes")
            {
                foreach (var x in db.AttachmentTypes) { ws.Cells[r, 1].Value = x.Name; r++; }
            }
            else if (type == "Provinces")
            {
                foreach (var x in db.Provinces) { ws.Cells[r, 1].Value = x.Name; r++; }
            }
            else if (type == "PartyRoles")
            {
                foreach (var x in db.PartyRoles) { ws.Cells[r, 1].Value = x.Name; r++; }
            }
            else if (type == "ContractExemptionReasons")
            {
                foreach (var x in db.ContractExemptionReasons) { ws.Cells[r, 1].Value = x.Reason; r++; }
            }
            else if (type == "ContractTypes")
            {
                foreach (var x in db.ContractTypes) { ws.Cells[r, 1].Value = x.Name; r++; }
            }

            ws.Cells.AutoFitColumns();
        }

        // 2. المالية
        private void ExportBankAccounts(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("BankAccounts");
            ws.Cells[1, 1].Value = "اسم البنك"; ws.Cells[1, 2].Value = "اسم الحساب"; ws.Cells[1, 3].Value = "رقم الحساب"; ws.Cells[1, 4].Value = "الآيبان"; ws.Cells[1, 5].Value = "العملة";
            int r = 2; foreach (var b in db.BankAccounts.Include(b => b.Currency)) { ws.Cells[r, 1].Value = b.BankName; ws.Cells[r, 2].Value = b.AccountName; ws.Cells[r, 3].Value = b.AccountNumber; ws.Cells[r, 4].Value = b.Iban; ws.Cells[r, 5].Value = b.Currency?.Name; r++; }
            ws.Cells.AutoFitColumns();
        }
        private void ExportFeeTypes(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("FeeTypes");
            ws.Cells[1, 1].Value = "اسم الرسم"; ws.Cells[1, 2].Value = "القيمة"; ws.Cells[1, 3].Value = "العملة"; ws.Cells[1, 4].Value = "رقم حساب البنك"; ws.Cells[1, 5].Value = "نسبة المحامي"; ws.Cells[1, 6].Value = "نسبة النقابة";
            int r = 2; foreach (var f in db.FeeTypes.Include(f => f.Currency).Include(f => f.BankAccount)) { ws.Cells[r, 1].Value = f.Name; ws.Cells[r, 2].Value = f.DefaultAmount; ws.Cells[r, 3].Value = f.Currency?.Name; ws.Cells[r, 4].Value = f.BankAccount?.AccountNumber; ws.Cells[r, 5].Value = f.LawyerPercentage; ws.Cells[r, 6].Value = f.BarSharePercentage; r++; }
            ws.Cells.AutoFitColumns();
        }

        // 3. الامتحانات
        private void ExportExams(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("Exams");
            ws.Cells[1, 1].Value = "عنوان الامتحان"; ws.Cells[1, 2].Value = "نوع الامتحان"; ws.Cells[1, 3].Value = "وقت البدء"; ws.Cells[1, 4].Value = "وقت الانتهاء"; ws.Cells[1, 5].Value = "المدة"; ws.Cells[1, 6].Value = "نسبة النجاح"; ws.Cells[1, 7].Value = "الحالة المطلوبة";
            int r = 2; foreach (var e in db.Exams.Include(x => x.ExamType).Include(x => x.RequiredApplicationStatus)) { ws.Cells[r, 1].Value = e.Title; ws.Cells[r, 2].Value = e.ExamType?.Name; ws.Cells[r, 3].Value = e.StartTime.ToString("yyyy-MM-dd HH:mm"); ws.Cells[r, 4].Value = e.EndTime.ToString("yyyy-MM-dd HH:mm"); ws.Cells[r, 5].Value = e.DurationInMinutes; ws.Cells[r, 6].Value = e.PassingPercentage; ws.Cells[r, 7].Value = e.RequiredApplicationStatus?.Name; r++; }
            ws.Cells.AutoFitColumns();
        }
        private void ExportExamResults(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("ExamResults");
            ws.Cells[1, 1].Value = "عنوان الامتحان"; ws.Cells[1, 2].Value = "الرقم الوطني للمتقدم"; ws.Cells[1, 3].Value = "العلامة"; ws.Cells[1, 4].Value = "النتيجة";
            int r = 2; foreach (var en in db.ExamEnrollments.Include(x => x.Exam).Include(x => x.GraduateApplication)) { ws.Cells[r, 1].Value = en.Exam?.Title; ws.Cells[r, 2].Value = en.GraduateApplication?.NationalIdNumber; ws.Cells[r, 3].Value = en.Score; ws.Cells[r, 4].Value = en.Result; r++; }
            ws.Cells.AutoFitColumns();
        }

        // 4. الدفعات
        private void ExportPayments(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("Payments");
            ws.Cells[1, 1].Value = "الرقم الوطني";
            ws.Cells[1, 2].Value = "نوع الرسم";
            ws.Cells[1, 3].Value = "المبلغ";
            ws.Cells[1, 4].Value = "تاريخ الدفع";
            ws.Cells[1, 5].Value = "رقم وصل البنك";
            ws.Cells[1, 6].Value = "ملاحظات";

            // نأخذ البيانات من Receipt المربوطة بـ Voucher
            // (r => ...) هنا لا بأس بها، لأننا غيرنا اسم المتغير بالأسفل
            var receipts = db.Receipts
                .Include(r => r.PaymentVoucher.GraduateApplication)
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType))
                .ToList();

            // === التصحيح: تغيير اسم المتغير من r إلى row ===
            int row = 2;

            foreach (var rec in receipts)
            {
                var voucher = rec.PaymentVoucher;
                if (voucher == null) continue;

                var detail = voucher.VoucherDetails.FirstOrDefault();

                // استخدام row بدلاً من r
                ws.Cells[row, 1].Value = voucher.GraduateApplication?.NationalIdNumber;
                ws.Cells[row, 2].Value = detail?.FeeType?.Name ?? detail?.Description;
                ws.Cells[row, 3].Value = voucher.TotalAmount;
                ws.Cells[row, 4].Value = rec.BankPaymentDate.ToString("yyyy-MM-dd");
                ws.Cells[row, 5].Value = rec.BankReceiptNumber;
                ws.Cells[row, 6].Value = rec.Notes;

                row++; // زيادة العداد
            }
            ws.Cells.AutoFitColumns();
        }
        // 5. العقود
        private void ExportContracts(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("Contracts");
            // العناوين (نفس الاستيراد)
            string[] h = { "رقم هوية المحامي", "نوع العقد", "تاريخ المعاملة", "الرسوم", "الحالة", "ملاحظات", "معفى؟", "سبب الإعفاء", "الطرف الأول", "هوية الأول", "صفة الأول", "محافظة الأول", "الطرف الثاني", "هوية الثاني", "صفة الثاني", "محافظة الثاني" };
            for (int i = 0; i < h.Length; i++) ws.Cells[1, i + 1].Value = h[i];

            var trans = db.ContractTransactions.Include(t => t.Lawyer).Include(t => t.ContractType).Include(t => t.ExemptionReason).Include(t => t.Parties.Select(pp => pp.PartyRole)).Include(t => t.Parties.Select(pp => pp.Province)).ToList();
            int r = 2;
            foreach (var t in trans)
            {
                ws.Cells[r, 1].Value = t.Lawyer?.NationalIdNumber;
                ws.Cells[r, 2].Value = t.ContractType?.Name;
                ws.Cells[r, 3].Value = t.TransactionDate.ToString("yyyy-MM-dd");
                ws.Cells[r, 4].Value = t.FinalFee;
                ws.Cells[r, 5].Value = t.Status;
                ws.Cells[r, 6].Value = t.Notes;
                ws.Cells[r, 7].Value = t.IsExempt ? "نعم" : "لا";
                ws.Cells[r, 8].Value = t.ExemptionReason?.Reason;

                var p1 = t.Parties.FirstOrDefault(x => x.PartyType == 1);
                if (p1 != null) { ws.Cells[r, 9].Value = p1.PartyName; ws.Cells[r, 10].Value = p1.PartyIDNumber; ws.Cells[r, 11].Value = p1.PartyRole?.Name; ws.Cells[r, 12].Value = p1.Province?.Name; }

                var p2 = t.Parties.FirstOrDefault(x => x.PartyType == 2);
                if (p2 != null) { ws.Cells[r, 13].Value = p2.PartyName; ws.Cells[r, 14].Value = p2.PartyIDNumber; ws.Cells[r, 15].Value = p2.PartyRole?.Name; ws.Cells[r, 16].Value = p2.Province?.Name; }
                r++;
            }
            ws.Cells.AutoFitColumns();
        }

        // 6. الطوابع
        private void ExportStampContractors(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("Contractors");
            ws.Cells[1, 1].Value = "الاسم"; ws.Cells[1, 2].Value = "الجوال"; ws.Cells[1, 3].Value = "الهوية"; ws.Cells[1, 4].Value = "المحافظة"; ws.Cells[1, 5].Value = "الموقع";
            int r = 2; foreach (var c in db.StampContractors) { ws.Cells[r, 1].Value = c.Name; ws.Cells[r, 2].Value = c.Phone; ws.Cells[r, 3].Value = c.NationalId; ws.Cells[r, 4].Value = c.Governorate; ws.Cells[r, 5].Value = c.Location; r++; }
            ws.Cells.AutoFitColumns();
        }
        private void ExportStampBooks(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("Books");
            ws.Cells[1, 1].Value = "من"; ws.Cells[1, 2].Value = "إلى"; ws.Cells[1, 3].Value = "القيمة"; ws.Cells[1, 4].Value = "الحالة";
            int r = 2; foreach (var b in db.StampBooks) { ws.Cells[r, 1].Value = b.StartSerial; ws.Cells[r, 2].Value = b.EndSerial; ws.Cells[r, 3].Value = b.ValuePerStamp; ws.Cells[r, 4].Value = b.Status; r++; }
            ws.Cells.AutoFitColumns();
        }
        private void ExportStampSales(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("Sales");
            ws.Cells[1, 1].Value = "رقم الطابع"; ws.Cells[1, 2].Value = "المتعهد"; ws.Cells[1, 3].Value = "هوية المحامي"; ws.Cells[1, 4].Value = "التاريخ"; ws.Cells[1, 5].Value = "تم الصرف";
            // نستخدم StampSales مباشرة أو Stamps المباعة
            int r = 2; foreach (var s in db.StampSales.Include(x => x.Stamp).Include(x => x.Contractor).Include(x => x.Lawyer))
            {
                ws.Cells[r, 1].Value = s.Stamp?.SerialNumber; ws.Cells[r, 2].Value = s.Contractor?.Name;
                ws.Cells[r, 3].Value = s.Lawyer?.NationalIdNumber; ws.Cells[r, 4].Value = s.SaleDate.ToString("yyyy-MM-dd");
                ws.Cells[r, 5].Value = s.IsPaidToLawyer ? "نعم" : "لا"; r++;
            }
            ws.Cells.AutoFitColumns();
        }

        // 7. أخرى
        private void ExportOralResults(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("OralResults");
            ws.Cells[1, 1].Value = "الرقم الوطني"; ws.Cells[1, 2].Value = "التاريخ"; ws.Cells[1, 3].Value = "النتيجة"; ws.Cells[1, 4].Value = "الدرجة"; ws.Cells[1, 5].Value = "ملاحظات";
            int r = 2; foreach (var x in db.OralExamEnrollments.Include(t => t.Trainee)) { ws.Cells[r, 1].Value = x.Trainee?.NationalIdNumber; ws.Cells[r, 2].Value = x.ExamDate.ToString("yyyy-MM-dd"); ws.Cells[r, 3].Value = x.Result; ws.Cells[r, 4].Value = x.Score; ws.Cells[r, 5].Value = x.Notes; r++; }
            ws.Cells.AutoFitColumns();
        }
        private void ExportResearch(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("Research");
            ws.Cells[1, 1].Value = "الرقم الوطني"; ws.Cells[1, 2].Value = "العنوان"; ws.Cells[1, 3].Value = "التاريخ"; ws.Cells[1, 4].Value = "الحالة";
            int r = 2; foreach (var x in db.LegalResearches.Include(t => t.Trainee)) { ws.Cells[r, 1].Value = x.Trainee?.NationalIdNumber; ws.Cells[r, 2].Value = x.Title; ws.Cells[r, 3].Value = x.SubmissionDate.ToString("yyyy-MM-dd"); ws.Cells[r, 4].Value = x.Status; r++; }
            ws.Cells.AutoFitColumns();
        }
        private void ExportLoans(ExcelPackage p)
        {
            var ws = p.Workbook.Worksheets.Add("Loans");
            ws.Cells[1, 1].Value = "الرقم الوطني"; ws.Cells[1, 2].Value = "المبلغ"; ws.Cells[1, 3].Value = "الأقساط"; ws.Cells[1, 4].Value = "تاريخ البدء"; ws.Cells[1, 5].Value = "الحالة"; ws.Cells[1, 6].Value = "ملاحظات";
            int r = 2; foreach (var x in db.LoanApplications.Include(t => t.Lawyer)) { ws.Cells[r, 1].Value = x.Lawyer?.NationalIdNumber; ws.Cells[r, 2].Value = x.Amount; ws.Cells[r, 3].Value = x.InstallmentCount; ws.Cells[r, 4].Value = x.StartDate.ToString("yyyy-MM-dd"); ws.Cells[r, 5].Value = x.Status; ws.Cells[r, 6].Value = x.Notes; r++; }
            ws.Cells.AutoFitColumns();
        }
















    }
}