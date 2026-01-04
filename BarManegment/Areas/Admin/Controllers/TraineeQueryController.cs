using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System;
using BarManegment.Helpers;

namespace BarManegment.Areas.Admin.Controllers
{
    // (تأكد من الصلاحيات المناسبة)
    public class TraineeQueryController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // --- 1. صفحة البحث الرئيسية (GET) ---
        // GET: Admin/TraineeQuery
        public ActionResult Index(TraineeQueryViewModel viewModel)
        {
            IQueryable<GraduateApplication> query = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Include(g => g.Supervisor)
                .Include(g => g.ContactInfo);

            // --- تطبيق الفلاتر ---
            if (!string.IsNullOrWhiteSpace(viewModel.SearchTerm))
            {
                query = query.Where(g => g.ArabicName.Contains(viewModel.SearchTerm) ||
                                         g.NationalIdNumber.Contains(viewModel.SearchTerm) ||
                                         g.TraineeSerialNo.Contains(viewModel.SearchTerm));
            }
            if (viewModel.StatusId.HasValue)
            {
                query = query.Where(g => g.ApplicationStatusId == viewModel.StatusId.Value);
            }
            if (viewModel.SupervisorId.HasValue)
            {
                query = query.Where(g => g.SupervisorId == viewModel.SupervisorId.Value);
            }
            if (viewModel.StartDate.HasValue)
            {
                query = query.Where(g => g.TrainingStartDate >= viewModel.StartDate.Value);
            }
            if (viewModel.EndDate.HasValue)
            {
                query = query.Where(g => g.TrainingStartDate <= viewModel.EndDate.Value);
            }

            // === 
            // === بداية الإضافة: فلترة المحافظة
            // ===
            if (!string.IsNullOrWhiteSpace(viewModel.Governorate))
            {
                query = query.Where(g => g.ContactInfo.Governorate == viewModel.Governorate);
            }
            // === نهاية الإضافة ===

            viewModel.Results = query.OrderByDescending(g => g.SubmissionDate).ToList();

            // --- إعداد القوائم المنسدلة ---
            viewModel.Statuses = new SelectList(db.ApplicationStatuses, "Id", "Name", viewModel.StatusId);
            viewModel.Supervisors = new SelectList(db.Users.Where(u => u.UserType.NameEnglish == "Practicing"), "Id", "FullNameArabic", viewModel.SupervisorId);

            // === 
            // === بداية الإضافة: تعبئة قائمة المحافظات
            // ===
            // (جلب قائمة فريدة بالمحافظات الموجودة في قاعدة البيانات)
            // (جلب قائمة فريدة بالمحافظات الموجودة في قاعدة البيانات)
            var governorateList = db.ContactInfos
                                    .Select(c => c.Governorate)
                                    .Where(g => g != null && g != "")
                                    .Distinct()
                                    .OrderBy(g => g)
                                    .ToList();
            viewModel.Governorates = new SelectList(governorateList, viewModel.Governorate);
            // === نهاية الإضافة ===
            // === نهاية الإضافة ===

            viewModel.AvailableColumns = TraineeQueryViewModel.GetAvailableColumns();
            if (viewModel.SelectedColumns == null || !viewModel.SelectedColumns.Any())
            {
                viewModel.SelectedColumns = new List<string> { "TraineeSerialNo", "ArabicName", "NationalIdNumber", "Status", "Supervisor" };
            }

            return View(viewModel);
        }

        // --- 2. دالة التصدير (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportToExcel(TraineeQueryViewModel viewModel)
        {
            // (إعادة تنفيذ نفس الاستعلام)
            IQueryable<GraduateApplication> query = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Include(g => g.Supervisor)
                .Include(g => g.ContactInfo);

            // (تطبيق نفس الفلاتر)
            if (!string.IsNullOrWhiteSpace(viewModel.SearchTerm))
            {
                query = query.Where(g => g.ArabicName.Contains(viewModel.SearchTerm) ||
                                         g.NationalIdNumber.Contains(viewModel.SearchTerm) ||
                                         g.TraineeSerialNo.Contains(viewModel.SearchTerm));
            }
            if (viewModel.StatusId.HasValue)
            {
                query = query.Where(g => g.ApplicationStatusId == viewModel.StatusId.Value);
            }
            if (viewModel.SupervisorId.HasValue)
            {
                query = query.Where(g => g.SupervisorId == viewModel.SupervisorId.Value);
            }
            if (viewModel.StartDate.HasValue)
            {
                query = query.Where(g => g.TrainingStartDate >= viewModel.StartDate.Value);
            }
            if (viewModel.EndDate.HasValue)
            {
                query = query.Where(g => g.TrainingStartDate <= viewModel.EndDate.Value);
            }

            // === 
            // === بداية الإضافة: فلترة المحافظة (في التصدير)
            // ===
            if (!string.IsNullOrWhiteSpace(viewModel.Governorate))
            {
                query = query.Where(g => g.ContactInfo.Governorate == viewModel.Governorate);
            }
            // === نهاية الإضافة ===

            var results = query.ToList();

            if (viewModel.SelectedColumns == null || !viewModel.SelectedColumns.Any())
            {
                TempData["ErrorMessage"] = "يرجى اختيار عمود واحد على الأقل لتصديره.";
                return RedirectToAction("Index", viewModel);
            }

            var allAvailableColumns = TraineeQueryViewModel.GetAvailableColumns().Items.Cast<KeyValuePair<string, string>>().ToDictionary(k => k.Key, v => v.Value);
            // 3. إنشاء ملف الإكسل
            //   ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("تقرير المتدربين");
                worksheet.View.RightToLeft = true; // جعل الشيت من اليمين لليسار

                // 4. كتابة الهيدر (الأعمدة المختارة فقط)
                int col = 1;
                foreach (var columnKey in viewModel.SelectedColumns)
                {
                    worksheet.Cells[1, col].Value = allAvailableColumns.ContainsKey(columnKey) ? allAvailableColumns[columnKey] : columnKey;
                    col++;
                }

                // 5. كتابة البيانات
                int row = 2;
                foreach (var trainee in results)
                {
                    col = 1;
                    foreach (var columnKey in viewModel.SelectedColumns)
                    {
                        // استخدام switch لجلب القيمة الصحيحة
                        switch (columnKey)
                        {
                            case "TraineeSerialNo": worksheet.Cells[row, col].Value = trainee.TraineeSerialNo; break;
                            case "ArabicName": worksheet.Cells[row, col].Value = trainee.ArabicName; break;
                            case "NationalIdNumber": worksheet.Cells[row, col].Value = trainee.NationalIdNumber; break;
                            case "Status": worksheet.Cells[row, col].Value = trainee.ApplicationStatus?.Name; break;
                            case "Supervisor": worksheet.Cells[row, col].Value = trainee.Supervisor?.ArabicName; break;
                            case "TrainingStartDate": worksheet.Cells[row, col].Value = trainee.TrainingStartDate?.ToString("yyyy-MM-dd"); break;
                            case "TrainingEndDate":
                                // (حساب التاريخ يدوياً)
                                DateTime? trainingEndDate = trainee.TrainingStartDate?.AddYears(2);
                                worksheet.Cells[row, col].Value = trainingEndDate?.ToString("yyyy-MM-dd");
                                break;
                            // === نهاية التصحيح ===
                            case "Gender": worksheet.Cells[row, col].Value = trainee.Gender?.Name; break;
                            case "BirthDate": worksheet.Cells[row, col].Value = trainee.BirthDate.ToString("yyyy-MM-dd"); break;
                            case "MobileNumber": worksheet.Cells[row, col].Value = trainee.ContactInfo?.MobileNumber; break;
                            case "Email": worksheet.Cells[row, col].Value = trainee.ContactInfo?.Email; break;
                            case "Governorate": worksheet.Cells[row, col].Value = trainee.ContactInfo?.Governorate; break;
                            case "Address": worksheet.Cells[row, col].Value = trainee.ContactInfo?.Street; break;
                        }
                        col++;
                    }
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"TraineeReport-{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }

        // --- 3. عرض صفحة الاستيراد (GET) ---
        // GET: Admin/TraineeQuery/Import
        public ActionResult Import()
        {
            return View();
        }

        // --- 4. معالجة ملف الاستيراد (POST) ---
        // --- 4. معالجة ملف الاستيراد (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(HttpPostedFileBase excelFile)
        {
            if (excelFile == null || excelFile.ContentLength == 0)
            {
                TempData["ErrorMessage"] = "الرجاء اختيار ملف إكسل.";
                return View();
            }
            if (Path.GetExtension(excelFile.FileName).ToLower() != ".xlsx")
            {
                TempData["ErrorMessage"] = "صيغة الملف غير مدعومة. يرجى استخدام .xlsx فقط.";
                return View();
            }

            var addedCount = 0;
            var errorList = new List<string>();

            // (جلب البيانات المساعدة مرة واحدة)
            var allStatuses = db.ApplicationStatuses.ToDictionary(s => s.Name, s => s.Id);
            var allSupervisors = db.GraduateApplications
                                 .Where(g => g.ApplicationStatus.Name == "محامي مزاول")
                                 .ToDictionary(g => g.NationalIdNumber, g => g.Id);
            var genderMaleId = db.Genders.FirstOrDefault(g => g.Name == "ذكر")?.Id;
            var genderFemaleId = db.Genders.FirstOrDefault(g => g.Name == "أنثى")?.Id;
            var defaultStatusId = allStatuses.ContainsKey("طلب جديد") ? allStatuses["طلب جديد"] : 0;

            // === 
            // === بداية الإضافة: جلب نوع المستخدم "خريج"
            // ===
            var graduateUserType = db.UserTypes.FirstOrDefault(ut => ut.NameEnglish == "Graduate");
            if (graduateUserType == null)
            {
                TempData["ErrorMessage"] = "خطأ فادح: لم يتم العثور على نوع المستخدم (Graduate).";
                return View();
            }
            // === نهاية الإضافة ===

            var defaultNationalIdTypeId = db.NationalIdTypes.FirstOrDefault(n => n.Name == "رقم الهوية")?.Id;

            if (defaultStatusId == 0 || !genderMaleId.HasValue || !genderFemaleId.HasValue || !defaultNationalIdTypeId.HasValue)
            {
                TempData["ErrorMessage"] = "خطأ في إعدادات النظام: لم يتم العثور على (طلب جديد) أو (ذكر/أنثى) أو (رقم الهوية).";
                return View();
            }

      ///      ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(excelFile.InputStream))
            {
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    TempData["ErrorMessage"] = "الملف فارغ.";
                    return View();
                }

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    try
                    {
                        string arabicName = worksheet.Cells[row, 1].GetValue<string>()?.Trim();
                        string nationalId = worksheet.Cells[row, 2].GetValue<string>()?.Trim();
                        DateTime? birthDateExcel = worksheet.Cells[row, 3].GetValue<DateTime?>();
                        string genderName = worksheet.Cells[row, 4].GetValue<string>()?.Trim();
                        string mobileNumber = worksheet.Cells[row, 5].GetValue<string>()?.Trim();
                        string email = worksheet.Cells[row, 6].GetValue<string>()?.Trim();
                        string governorate = worksheet.Cells[row, 7].GetValue<string>()?.Trim();
                        string statusName = worksheet.Cells[row, 8].GetValue<string>()?.Trim();
                        string supervisorNationalId = worksheet.Cells[row, 9].GetValue<string>()?.Trim();
                        DateTime? trainingStartDate = worksheet.Cells[row, 10].GetValue<DateTime?>();
                        string traineeSerialNo = worksheet.Cells[row, 11].GetValue<string>()?.Trim();

                        // --- 1. التحقق من البيانات الإلزامية ---
                        if (string.IsNullOrWhiteSpace(nationalId) || string.IsNullOrWhiteSpace(arabicName) || !birthDateExcel.HasValue || string.IsNullOrWhiteSpace(email))
                        {
                            errorList.Add($"السطر {row}: بيانات أساسية مفقودة (الاسم، الرقم الوطني، تاريخ الميلاد، أو الإيميل).");
                            continue;
                        }
                        if (birthDateExcel.Value < (DateTime)System.Data.SqlTypes.SqlDateTime.MinValue)
                        {
                            errorList.Add($"السطر {row}: تاريخ الميلاد {birthDateExcel.Value.ToShortDateString()} غير صالح.");
                            continue;
                        }

                        // --- 2. التحقق من التكرار (في كلا الجدولين) ---
                        if (db.GraduateApplications.Any(g => g.NationalIdNumber == nationalId) ||
                            db.GraduateApplications.Local.Any(g => g.NationalIdNumber == nationalId))
                        {
                            errorList.Add($"السطر {row}: الرقم الوطني {nationalId} موجود مسبقاً في (سجلات الخريجين).");
                            continue;
                        }
                        if (db.Users.Any(u => u.Username == nationalId || u.Email == email) ||
                            db.Users.Local.Any(u => u.Username == nationalId || u.Email == email))
                        {
                            errorList.Add($"السطر {row}: الرقم الوطني ({nationalId}) أو الإيميل ({email}) موجود مسبقاً في (جدول الحسابات).");
                            continue;
                        }

                        // --- 3. تجهيز البيانات (IDs) ---
                        int statusId = defaultStatusId;
                        if (!string.IsNullOrWhiteSpace(statusName) && allStatuses.ContainsKey(statusName))
                        {
                            statusId = allStatuses[statusName];
                        }

                        int? supervisorId = null;
                        if (!string.IsNullOrWhiteSpace(supervisorNationalId) && allSupervisors.ContainsKey(supervisorNationalId))
                        {
                            supervisorId = allSupervisors[supervisorNationalId];
                        }

                        // === 
                        // === 4. إنشاء حساب الدخول (UserModel) ===
                        // ===
                        string defaultPassword = nationalId; // (كلمة المرور الافتراضية هي الرقم الوطني)
                        var newUser = new UserModel
                        {
                            FullNameArabic = arabicName,
                            Username = nationalId, // (اسم المستخدم هو الرقم الوطني)
                            Email = email,
                            IdentificationNumber = nationalId,
                            IsActive = true,
                            UserTypeId = graduateUserType.Id, // (نوع المستخدم = خريج)
                            HashedPassword = PasswordHelper.HashPassword(defaultPassword)
                        };
                        db.Users.Add(newUser);
                        // (سيقوم EF بحفظ هذا أولاً والحصول على ID)

                        // === 
                        // === 5. إنشاء ملف المتدرب (GraduateApplication) وربطه
                        // ===
                        var newApp = new GraduateApplication
                        {
                            ArabicName = arabicName,
                            NationalIdNumber = nationalId,
                            BirthDate = birthDateExcel.Value,
                            GenderId = (genderName == "أنثى" ? genderFemaleId.Value : genderMaleId.Value),
                            NationalIdTypeId = defaultNationalIdTypeId.Value,
                            ApplicationStatusId = statusId,
                            SubmissionDate = DateTime.Now,
                            SupervisorId = supervisorId,
                            TrainingStartDate = trainingStartDate,
                            TraineeSerialNo = traineeSerialNo,

                            // (الربط بالحساب الجديد)
                            User = newUser, // <-- هذا هو الربط الأهم

                            ContactInfo = new ContactInfo
                            {
                                MobileNumber = mobileNumber,
                                Email = email,
                                Governorate = governorate
                            }
                        };
                        db.GraduateApplications.Add(newApp);
                        addedCount++;
                    }
                    catch (Exception ex)
                    {
                        errorList.Add($"السطر {row}: خطأ في قراءة البيانات. ({ex.Message})");
                    }
                }
            }

            // --- 6. حفظ التغييرات (كما في الكود السابق) ---
            if (addedCount > 0)
            {
                try
                {
                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"تم استيراد وإنشاء حسابات لـ {addedCount} سجل بنجاح. كلمة المرور الافتراضية هي الرقم الوطني.";
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            errorList.Add($"خطأ في التحقق: الحقل '{validationError.PropertyName}' - {validationError.ErrorMessage}");
                        }
                    }
                    TempData["ErrorMessage"] = "فشل الحفظ بسبب أخطاء في البيانات (مثل حقل إلزامي فارغ):<br/>" + string.Join("<br/>", errorList);
                }
                catch (Exception ex)
                {
                    string innerMessage = ex.InnerException?.InnerException?.Message ?? ex.Message;
                    if (innerMessage.Contains("UNIQUE KEY constraint"))
                    {
                        errorList.Add("خطأ حفظ: واحد أو أكثر من السجلات يحتوي على قيمة مكررة (مثل البريد الإلكتروني أو رقم المتدرب).");
                    }
                    else
                    {
                        errorList.Add("خطأ عام أثناء الحفظ: " + innerMessage);
                    }
                    TempData["ErrorMessage"] = string.Join("<br/>", errorList);
                }
            }

            if (errorList.Any() && TempData["SuccessMessage"] == null)
            {
                if (TempData["ErrorMessage"] == null)
                {
                    TempData["ErrorMessage"] = "تمت معالجة الملف مع الأخطاء التالية (ولم يتم حفظ شيء):<br/>" + string.Join("<br/>", errorList);
                }
            }

            return RedirectToAction("Index");
        }
        // === نهاية التعديل ===


        // --- 5. تحميل نموذج الاستيراد (GET) ---
        // GET: Admin/TraineeQuery/DownloadImportTemplate
        // === 
        // === بداية التعديل: دالة تحميل النموذج المحدث
        // ===
        // --- 5. تحميل نموذج الاستيراد (GET) ---
        // GET: Admin/TraineeQuery/DownloadImportTemplate
        public ActionResult DownloadImportTemplate()
        {
          ///  ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("نموذج استيراد");
                worksheet.View.RightToLeft = true;

                // (تحديد الأعمدة المطلوبة)
                worksheet.Cells["A1"].Value = "الاسم الكامل (مطلوب)";
                worksheet.Cells["B1"].Value = "الرقم الوطني (مطلوب - فريد)";
                worksheet.Cells["C1"].Value = "تاريخ الميلاد (مطلوب - YYYY-MM-DD)";
                worksheet.Cells["D1"].Value = "الجنس (مطلوب - 'ذكر' أو 'أنثى')";
                worksheet.Cells["E1"].Value = "رقم الجوال (مطلوب)";
                worksheet.Cells["F1"].Value = "البريد الإلكتروني (مطلوب - فريد)";
                worksheet.Cells["G1"].Value = "المحافظة (مطلوب)";
                worksheet.Cells["H1"].Value = "الحالة (اختياري - الافتراضي 'طلب جديد')";
                worksheet.Cells["I1"].Value = "الرقم الوطني للمشرف (اختياري)";
                worksheet.Cells["J1"].Value = "تاريخ بدء التدريب (اختياري - YYYY-MM-DD)";
                worksheet.Cells["K1"].Value = "رقم المتدرب (اختياري - مثال 00123/2025)";

                // (تنسيق الهيدر)
                using (var range = worksheet.Cells["A1:K1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }

                // (تنسيق الأعمدة)
                worksheet.Column(3).Style.Numberformat.Format = "yyyy-mm-dd";
                worksheet.Column(10).Style.Numberformat.Format = "yyyy-mm-dd";

                // (إضافة قائمة منسدلة للجنس)
                var genderValidation = worksheet.DataValidations.AddListValidation("D2:D1000");
                genderValidation.Formula.Values.Add("ذكر");
                genderValidation.Formula.Values.Add("أنثى");
                genderValidation.ShowErrorMessage = true;
                genderValidation.Error = "الرجاء الاختيار من القائمة (ذكر / أنثى).";

                // === 
                // === بداية التعديل: حل مشكلة 255 حرف
                // ===

                // (إضافة قائمة منسدلة للحالات الأولية المسموحة فقط)
                var statusValidation = worksheet.DataValidations.AddListValidation("H2:H1000");

                // (تحديد الحالات الأولية المسموحة عند الاستيراد فقط)
                var importableStatuses = new List<string>
                { "متدرب مقيد",
                 "متدرب موقوف",
                    "طلب جديد",
                    "قيد المراجعة",
                    "معفى (مؤهل للتسجيل)",
                    "بانتظار استكمال النواقص"
                    // (هذه القائمة القصيرة لن تتجاوز 255 حرفاً)
                };

                foreach (var status in importableStatuses)
                {
                    statusValidation.Formula.Values.Add(status); // (السطر 390 سابقاً)
                }
                statusValidation.ShowErrorMessage = true;
                statusValidation.Error = "الرجاء الاختيار من قائمة الحالات المعتمدة.";
                // === نهاية التعديل ===


                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = "TraineeImportTemplate_Advanced.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
        // === نهاية التعديل ===

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }

 
    }
}