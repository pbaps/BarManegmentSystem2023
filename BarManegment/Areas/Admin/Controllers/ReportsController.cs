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
    [CustomAuthorize(Permission = "CanViewReports")]
    public class ReportsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: صفحة التقارير الرئيسية
        public ActionResult Index(string type = "Graduates")
        {
            var model = new ReportViewModel { ReportType = type };

            // تجهيز القوائم المنسدلة للفلاتر (ViewBag)
            PrepareViewBags(type);

            // تجهيز الأعمدة المتاحة للاختيار بناءً على النوع
            model.AvailableColumns = GetColumnsForType(type);

            // تحديد كل الأعمدة كافتراضي في البداية
            model.SelectedColumns = model.AvailableColumns.Keys.ToList();

            return View(model);
        }

        // POST: عرض النتائج (Search)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(ReportViewModel model)
        {
            PrepareViewBags(model.ReportType);
            model.AvailableColumns = GetColumnsForType(model.ReportType); // إعادة تعبئة الأعمدة

            // المنطق الرئيسي لجلب البيانات
            switch (model.ReportType)
            {
                case "Graduates":
                    model.Results = GetGraduatesData(model);
                    break;
                case "Contracts":
                    model.Results = GetContractsData(model);
                    break;

                case "FamilyHealth":
                    model.Results = GetFamilyHealthData(model); // في Index
                                                                // data = GetFamilyHealthData(model); // في Export
                    break;
                    // أضف باقي الحالات (Financial, Exams...)
            }

            return View(model);
        }

        // POST: تصدير إلى إكسل (Export)
        [HttpPost]
        public ActionResult Export(ReportViewModel model)
        {
            // إعادة جلب البيانات (لأننا في Request جديد)
            // ملاحظة: لتحسين الأداء يمكن استخدام TempData أو Session لتخزين النتائج مؤقتاً، لكن إعادة الاستعلام أكثر أماناً للبيانات الكبيرة
            List<dynamic> data = new List<dynamic>();

            switch (model.ReportType)
            {
                case "Graduates": data = GetGraduatesData(model); break;
                case "Contracts": data = GetContractsData(model); break;
                case "FamilyHealth":   data = GetFamilyHealthData(model); break;
  
            }

          //  ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Report");
                var colMapping = GetColumnsForType(model.ReportType);

                // 1. رسم العناوين المختارة فقط
                int colIndex = 1;
                foreach (var key in model.SelectedColumns)
                {
                    if (colMapping.ContainsKey(key))
                    {
                        ws.Cells[1, colIndex].Value = colMapping[key];
                        colIndex++;
                    }
                }

                // تنسيق العناوين
                using (var range = ws.Cells[1, 1, 1, colIndex - 1])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // 2. ملء البيانات
                int row = 2;
                foreach (var item in data)
                {
                    colIndex = 1;
                    // نستخدم Reflection لقراءة الخصائص ديناميكياً
                    var props = item.GetType().GetProperties();

                    foreach (var key in model.SelectedColumns)
                    {
                        // البحث عن الخاصية في الكائن الديناميكي
                        var prop = ((System.Reflection.PropertyInfo[])props).FirstOrDefault(p => p.Name == key);
                        if (prop != null)
                        {
                            var val = prop.GetValue(item, null);

                            // تنسيق التواريخ
                            if (val is DateTime dt)
                                ws.Cells[row, colIndex].Value = dt.ToString("yyyy-MM-dd");
                            else
                                ws.Cells[row, colIndex].Value = val;
                        }
                        colIndex++;
                    }
                    row++;
                }

                ws.Cells.AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Report_{model.ReportType}_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }

        // ==========================================
        // دوال المساعدة (Helpers)
        // ==========================================

        private Dictionary<string, string> GetColumnsForType(string type)
        {
            var cols = new Dictionary<string, string>();

            if (type == "Graduates")
            {
                cols.Add("NationalIdNumber", "الرقم الوطني");
                cols.Add("ArabicName", "الاسم العربي");
                cols.Add("Gender", "الجنس");
                cols.Add("Status", "الحالة النقابية");
                cols.Add("Governorate", "المحافظة");
                cols.Add("City", "المدينة");
                cols.Add("TraineeSerialNo", "رقم المتدرب");
                cols.Add("MembershipId", "رقم العضوية");
                cols.Add("Mobile", "الجوال");
                cols.Add("TrainingStartDate", "تاريخ بدء التدريب");
                cols.Add("Qualifications", "المؤهلات العلمية");
                cols.Add("SupervisorName", "المحامي المشرف");
            } // <--- نهاية الـ if الأولى (كان القوس مفقوداً أو في مكان خاطئ)
            else if (type == "Contracts") // <--- الـ else if الآن داخل الدالة بشكل صحيح
            {
                cols.Add("TransactionDate", "تاريخ المعاملة");
                cols.Add("LawyerName", "اسم المحامي");
                cols.Add("ContractType", "نوع العقد");
                cols.Add("FinalFee", "الرسوم");
                cols.Add("Parties", "الأطراف");
                cols.Add("Status", "الحالة");
                cols.Add("Notes", "ملاحظات");
            }
            // === 💡 الإضافة الجديدة ===
            if (type == "FamilyHealth")
            {
                cols.Add("NationalId", "رقم الهوية");
                cols.Add("Name", "الاسم");
                cols.Add("MaritalStatus", "الحالة الاجتماعية");
                cols.Add("WivesCount", "عدد الزوجات");
                cols.Add("ChildrenCount", "عدد الأبناء");
                cols.Add("Displacement", "محافظة النزوح");
                cols.Add("HealthStatus", "الحالة الصحية");
                cols.Add("Diseases", "تفاصيل المرض");
                cols.Add("Medications", "الأدوية");
                cols.Add("HasInsurance", "تأمين صحي؟");
                cols.Add("WasDetained", "تعرض للاعتقال؟");
                cols.Add("DetentionPeriod", "فترة الاعتقال");
            }

            return cols; // <--- الـ return الآن داخل الدالة وقبل إغلاقها
        }

        private void PrepareViewBags(string type)
        {
            ViewBag.StatusList = new SelectList(db.ApplicationStatuses, "Id", "Name");


            if (type == "Graduates")
            {
                ViewBag.StatusList = new SelectList(db.ApplicationStatuses, "Id", "Name");
                ViewBag.GenderList = new SelectList(db.Genders, "Id", "Name");

                // 1. جلب قائمة المحافظات المستخدمة فعلياً (بدون تكرار)
                var governorates = db.ContactInfos
                                     .Where(c => c.Governorate != null && c.Governorate != "")
                                     .Select(c => c.Governorate)
                                     .Distinct()
                                     .OrderBy(g => g)
                                     .ToList();
                ViewBag.GovernorateList = new SelectList(governorates);

                // 2. جلب أنواع المؤهلات (إذا أردت الفلترة بالدرجة العلمية)
                ViewBag.QualificationTypeList = new SelectList(db.QualificationTypes, "Id", "Name");

                // 3. قائمة سنوات (للانتساب) - آخر 20 سنة
                var years = Enumerable.Range(DateTime.Now.Year - 20, 21).OrderByDescending(y => y).ToList();
                ViewBag.YearsList = new SelectList(years);
            }




            else if (type == "Contracts")
            {
                ViewBag.TypeList = new SelectList(db.ContractTypes, "Id", "Name");
            }

            // === 💡 الإضافة الجديدة ===
            if (type == "FamilyHealth")
            {
                ViewBag.MaritalStatusList = new SelectList(new[] { "أعزب", "متزوج", "مطلق", "أرمل" });
                ViewBag.HealthStatusList = new SelectList(new[] { "سليم", "مصاب", "مريض مزمن" });

                // جلب محافظات النزوح الفعلية من الداتا
                var dispLocs = db.LawyerPersonalDatas
                                 .Where(x => x.DisplacementGovernorate != null)
                                 .Select(x => x.DisplacementGovernorate)
                                 .Distinct().ToList();
                ViewBag.DisplacementList = new SelectList(dispLocs);
            }
        }

        // --- منطق جلب بيانات الخريجين ---
        private List<dynamic> GetGraduatesData(ReportViewModel model)
        {
            var query = db.GraduateApplications.AsQueryable();

            // --- الفلاتر الأساسية السابقة ---
            if (!string.IsNullOrEmpty(model.SearchKeyword))
                query = query.Where(x => x.ArabicName.Contains(model.SearchKeyword) || x.NationalIdNumber.Contains(model.SearchKeyword) || x.TraineeSerialNo.Contains(model.SearchKeyword));

            if (model.StatusId.HasValue)
                query = query.Where(x => x.ApplicationStatusId == model.StatusId);

            if (model.GenderId.HasValue)
                query = query.Where(x => x.GenderId == model.GenderId);

            if (model.DateFrom.HasValue)
                query = query.Where(x => x.SubmissionDate >= model.DateFrom);

            if (model.DateTo.HasValue)
                query = query.Where(x => x.SubmissionDate <= model.DateTo);

            // === 💡 الفلاتر الجديدة ===

            // 1. فلترة المحافظة (من خلال ContactInfo)
            if (!string.IsNullOrEmpty(model.SelectedGovernorate))
            {
                query = query.Where(x => x.ContactInfo.Governorate == model.SelectedGovernorate);
            }

            // 2. فلترة المدينة (اختياري)
            if (!string.IsNullOrEmpty(model.SelectedCity))
            {
                query = query.Where(x => x.ContactInfo.City.Contains(model.SelectedCity));
            }

            // 3. فلترة المؤهل العلمي (يجلب من لديه هذا المؤهل على الأقل)
            if (model.QualificationTypeId.HasValue)
            {
                query = query.Where(x => x.Qualifications.Any(q => q.QualificationTypeId == model.QualificationTypeId));
            }

            // 4. فلترة سنة الانتساب (بناءً على تاريخ التقديم أو تاريخ بدء التدريب)
            if (model.RegistrationYear.HasValue)
            {
                query = query.Where(x => x.SubmissionDate.Year == model.RegistrationYear.Value);
            }

            // ----------------------------

            // جلب البيانات (مع التأكد من تضمين الجداول المرتبطة)
            var result = query
                .Include(g => g.ContactInfo)
                .Include(g => g.Supervisor)
                .Include(g => g.Qualifications.Select(q => q.QualificationType)) // لتضمين اسم المؤهل
                .ToList()
                .Select(x => new
                {
                    x.NationalIdNumber,
                    x.ArabicName,
                    Gender = x.Gender?.Name,
                    Status = x.ApplicationStatus?.Name,
                    x.TraineeSerialNo,
                    x.MembershipId,

            // بيانات الاتصال
            Governorate = x.ContactInfo?.Governorate ?? "-",
                    City = x.ContactInfo?.City ?? "-",
                    Mobile = x.ContactInfo?.MobileNumber,

            // التواريخ
            x.TrainingStartDate,
                    SubmissionYear = x.SubmissionDate.Year,

                    SupervisorName = x.Supervisor != null ? x.Supervisor.ArabicName : "لا يوجد",

            // تجميع المؤهلات في نص واحد (مثلاً: بكالوريوس، ماجستير)
            Qualifications = string.Join(", ", x.Qualifications.Select(q => q.QualificationType?.Name))
                }).ToList<dynamic>();

            return result;
        }
        // --- منطق جلب بيانات العقود ---
        private List<dynamic> GetContractsData(ReportViewModel model)
        {
            var query = db.ContractTransactions.AsQueryable();

            if (!string.IsNullOrEmpty(model.SearchKeyword))
                query = query.Where(x => x.Lawyer.ArabicName.Contains(model.SearchKeyword) || x.Lawyer.NationalIdNumber.Contains(model.SearchKeyword));

            if (model.TypeId.HasValue)
                query = query.Where(x => x.ContractTypeId == model.TypeId);

            if (model.DateFrom.HasValue)
                query = query.Where(x => x.TransactionDate >= model.DateFrom);

            if (model.DateTo.HasValue)
                query = query.Where(x => x.TransactionDate <= model.DateTo);

            // جلب البيانات مع الأطراف
            var rawData = query
                .Include(c => c.Lawyer)
                .Include(c => c.ContractType)
                .Include(c => c.Parties)
                .ToList();

            // التحويل في الذاكرة (لتجميع أسماء الأطراف)
            var result = rawData.Select(x => new
            {
                x.TransactionDate,
                LawyerName = x.Lawyer.ArabicName,
                ContractType = x.ContractType.Name,
                x.FinalFee,
                Parties = string.Join(" - ", x.Parties.Select(p => p.PartyName)), // دمج الأطراف
                x.Status,
                x.Notes
            }).ToList<dynamic>();

            return result;
        }

        private List<dynamic> GetFamilyHealthData(ReportViewModel model)
        {
            // نبدأ من جدول المحامين (GraduateApplications) ونربط البيانات الشخصية والصحية
            var query = db.GraduateApplications
                          .Include(g => g.LawyerPersonalData)
                          .Include(g => g.LawyerPersonalData.Spouses)
                          .Include(g => g.LawyerPersonalData.Children)
                          .Include(g => g.LawyerPersonalData.HealthRecord)
                          .AsQueryable();

            // 1. الفلاتر العامة
            if (!string.IsNullOrEmpty(model.SearchKeyword))
                query = query.Where(x => x.ArabicName.Contains(model.SearchKeyword) || x.NationalIdNumber.Contains(model.SearchKeyword));

            // 2. فلاتر العائلة
            if (!string.IsNullOrEmpty(model.SelectedMaritalStatus))
                query = query.Where(x => x.LawyerPersonalData.MaritalStatus == model.SelectedMaritalStatus);

            if (!string.IsNullOrEmpty(model.SelectedDisplacement))
                query = query.Where(x => x.LawyerPersonalData.DisplacementGovernorate == model.SelectedDisplacement);

            // 3. فلاتر الصحة والأمن
            if (!string.IsNullOrEmpty(model.SelectedHealthStatus))
                query = query.Where(x => x.LawyerPersonalData.HealthRecord.GeneralHealthStatus == model.SelectedHealthStatus);

            if (model.IsDetainedFilter.HasValue)
                query = query.Where(x => x.LawyerPersonalData.HealthRecord.WasDetained == model.IsDetainedFilter.Value);

            if (model.HasInsuranceFilter.HasValue)
                query = query.Where(x => x.LawyerPersonalData.HealthRecord.HasHealthInsurance == model.HasInsuranceFilter.Value);

            // تنفيذ الاستعلام
            var data = query.ToList();

            // تشكيل النتائج
            var result = data.Select(x => new
            {
                NationalId = x.NationalIdNumber,
                Name = x.ArabicName,

                // بيانات شخصية
                MaritalStatus = x.LawyerPersonalData?.MaritalStatus ?? "-",
                WivesCount = x.LawyerPersonalData?.Spouses?.Count ?? 0,
                ChildrenCount = x.LawyerPersonalData?.Children?.Count ?? 0,
                Displacement = x.LawyerPersonalData?.DisplacementGovernorate ?? "-",

                // بيانات صحية
                HealthStatus = x.LawyerPersonalData?.HealthRecord?.GeneralHealthStatus ?? "-",
                Diseases = string.IsNullOrEmpty(x.LawyerPersonalData?.HealthRecord?.GeneralHealthStatus) ? "-" : x.LawyerPersonalData?.HealthRecord?.GeneralHealthStatus, // أو حقل تفاصيل المرض إن وجد
                Medications = x.LawyerPersonalData?.HealthRecord?.MedicationsList ?? "-",
                HasInsurance = (x.LawyerPersonalData?.HealthRecord?.HasHealthInsurance ?? false) ? "نعم" : "لا",

                // بيانات أمنية
                WasDetained = (x.LawyerPersonalData?.HealthRecord?.WasDetained ?? false) ? "نعم" : "لا",
                DetentionPeriod = (x.LawyerPersonalData?.HealthRecord?.WasDetained ?? false)
                                  ? $"{x.LawyerPersonalData.HealthRecord.DetentionStartDate:yyyy/MM/dd} - {x.LawyerPersonalData.HealthRecord.DetentionEndDate:yyyy/MM/dd}"
                                  : "-"
            }).ToList<dynamic>();

            return result;
        }
    }
}