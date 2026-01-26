using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Services;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Globalization;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class SystemSettingsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Index()
        {
            // 1. جلب الإعدادات النصية القديمة
            var startDateSetting = db.SystemSettings.Find("ExamRegistrationStartDate");
            var endDateSetting = db.SystemSettings.Find("ExamRegistrationEndDate");
            var highSchoolSetting = db.SystemSettings.Find("MinHighSchoolScore");
            var bachelorSetting = db.SystemSettings.Find("MinBachelorScore");
            var gracePeriodSetting = db.SystemSettings.Find("RenewalGracePeriodEndDate");
            var trainingHoursSetting = db.SystemSettings.Find("RequiredTrainingHours");
            var annualIncSetting = db.SystemSettings.Find("AnnualIncrementPercent");
            var empPensionSetting = db.SystemSettings.Find("EmployeePensionPercent");
            var employerPensionSetting = db.SystemSettings.Find("EmployerPensionPercent");
            var examFeeSetting = db.SystemSettings.Find("IsExamFeeEnabled");

            // 2. جلب الإعدادات المالية (ValueInt)
            // أ. حسابات الطوابع والبنوك
            var stampPrepaid = db.SystemSettings.Find("Stamp_PrepaidAccount");
            var stampLawyer = db.SystemSettings.Find("Stamp_LawyerShareAccount");
            var stampRevenue = db.SystemSettings.Find("Stamp_RevenueAccount");
            var defaultBank = db.SystemSettings.Find("Default_Bank_Payment_Account");

            var stampFee = db.SystemSettings.Find("Stamp_Contractor_FeeTypeId");


            // ب. إعدادات رسوم الامتحانات والعقود (الجديدة)
            var examRegFee = db.SystemSettings.Find("Exam_Registration_FeeTypeId");
            var contractFee = db.SystemSettings.Find("Contract_FeeTypeId");
            var passportType = db.SystemSettings.Find("Contract_PassportAgencyTypeId");


            // ✅ جلب إعدادات الحضور
            var latSetting = db.SystemSettings.Find("Office_Latitude");
            var lngSetting = db.SystemSettings.Find("Office_Longitude");
            var radiusSetting = db.SystemSettings.Find("Allowed_Radius_Meters");

            // 3. تعبئة القوائم المنسدلة (Dropdowns)
            ReloadLists();

            var viewModel = new SystemSettingsViewModel
            {
                // القيم النصية
                ExamRegistrationStartDate = startDateSetting != null ? DateTime.Parse(startDateSetting.SettingValue) : DateTime.Now,
                ExamRegistrationEndDate = endDateSetting != null ? DateTime.Parse(endDateSetting.SettingValue) : DateTime.Now.AddDays(30),
                MinHighSchoolScore = highSchoolSetting != null ? double.Parse(highSchoolSetting.SettingValue, CultureInfo.InvariantCulture) : 50,
                MinBachelorScore = bachelorSetting != null ? double.Parse(bachelorSetting.SettingValue, CultureInfo.InvariantCulture) : 60,
                RenewalGracePeriodEndDate = gracePeriodSetting != null ? DateTime.Parse(gracePeriodSetting.SettingValue) : new DateTime(DateTime.Now.Year, 3, 31),
                RequiredTrainingHours = trainingHoursSetting != null ? int.Parse(trainingHoursSetting.SettingValue) : 100,
                AnnualIncrementPercent = annualIncSetting != null ? decimal.Parse(annualIncSetting.SettingValue, CultureInfo.InvariantCulture) : 5m,
                EmployeePensionPercent = empPensionSetting != null ? decimal.Parse(empPensionSetting.SettingValue, CultureInfo.InvariantCulture) : 7m,
                EmployerPensionPercent = employerPensionSetting != null ? decimal.Parse(employerPensionSetting.SettingValue, CultureInfo.InvariantCulture) : 9m,
                IsExamFeeEnabled = examFeeSetting != null ? bool.Parse(examFeeSetting.SettingValue) : true,


                // ✅ تعيين قيم الحضور
                OfficeLatitude = latSetting?.SettingValue,
                OfficeLongitude = lngSetting?.SettingValue,
                AllowedRadiusMeters = radiusSetting != null ? int.Parse(radiusSetting.SettingValue) : 100,






                // القيم المالية (الحسابات)
                StampPrepaidAccountId = stampPrepaid?.ValueInt,
                StampLawyerShareAccountId = stampLawyer?.ValueInt,
                StampRevenueAccountId = stampRevenue?.ValueInt,
                DefaultBankPaymentAccountId = defaultBank?.ValueInt,

              StampContractorFeeTypeId = stampFee?.ValueInt ,

            // القيم التشغيلية (الرسوم وأنواع العقود)
            ExamRegistrationFeeTypeId = examRegFee?.ValueInt,
                ContractFeeTypeId = contractFee?.ValueInt,
                PassportAgencyContractTypeId = passportType?.ValueInt
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Index(SystemSettingsViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. حفظ الإعدادات النصية
                    UpdateSetting("ExamRegistrationStartDate", viewModel.ExamRegistrationStartDate.ToString("yyyy-MM-dd"), "بداية تسجيل الامتحانات");
                    UpdateSetting("ExamRegistrationEndDate", viewModel.ExamRegistrationEndDate.ToString("yyyy-MM-dd"), "نهاية تسجيل الامتحانات");
                    UpdateSetting("MinHighSchoolScore", viewModel.MinHighSchoolScore.ToString(CultureInfo.InvariantCulture), "معدل القبول ثانوية");
                    UpdateSetting("MinBachelorScore", viewModel.MinBachelorScore.ToString(CultureInfo.InvariantCulture), "معدل القبول جامعة");
                    UpdateSetting("RenewalGracePeriodEndDate", viewModel.RenewalGracePeriodEndDate.ToString("yyyy-MM-dd"), "فترة سماح التجديد");
                    UpdateSetting("RequiredTrainingHours", viewModel.RequiredTrainingHours.ToString(), "ساعات التدريب المطلوبة");
                    UpdateSetting("AnnualIncrementPercent", viewModel.AnnualIncrementPercent.ToString(CultureInfo.InvariantCulture), "نسبة الزيادة السنوية");
                    UpdateSetting("EmployeePensionPercent", viewModel.EmployeePensionPercent.ToString(CultureInfo.InvariantCulture), "نسبة التقاعد موظف");
                    UpdateSetting("EmployerPensionPercent", viewModel.EmployerPensionPercent.ToString(CultureInfo.InvariantCulture), "نسبة التقاعد نقابة");
                    UpdateSetting("IsExamFeeEnabled", viewModel.IsExamFeeEnabled.ToString(), "تفعيل رسوم الامتحان");

                    // 2. حفظ الإعدادات المحاسبية (في ValueInt)
                    UpdateIntSetting("Stamp_PrepaidAccount", viewModel.StampPrepaidAccountId, "حساب إيراد طوابع مؤجل");
                    UpdateIntSetting("Stamp_LawyerShareAccount", viewModel.StampLawyerShareAccountId, "حساب أمانات المحامين");
                    UpdateIntSetting("Stamp_RevenueAccount", viewModel.StampRevenueAccountId, "حساب إيراد الطوابع");
                    UpdateIntSetting("Default_Bank_Payment_Account", viewModel.DefaultBankPaymentAccountId, "حساب البنك الافتراضي للصرف");
                    UpdateIntSetting("Stamp_Contractor_FeeTypeId", viewModel.StampContractorFeeTypeId, "رسم بيع الطوابع");
                    // 3. حفظ إعدادات الرسوم والعقود (في ValueInt)
                    UpdateIntSetting("Exam_Registration_FeeTypeId", viewModel.ExamRegistrationFeeTypeId, "نوع رسم امتحان القبول");
                    UpdateIntSetting("Contract_FeeTypeId", viewModel.ContractFeeTypeId, "نوع رسم تصديق العقود");
                    UpdateIntSetting("Contract_PassportAgencyTypeId", viewModel.PassportAgencyContractTypeId, "نوع عقد وكالة الجوازات");


                    // ✅ حفظ إعدادات الحضور
                    UpdateSetting("Office_Latitude", viewModel.OfficeLatitude, "إحداثيات المقر - خط العرض");
                    UpdateSetting("Office_Longitude", viewModel.OfficeLongitude, "إحداثيات المقر - خط الطول");
                    UpdateSetting("Allowed_Radius_Meters", viewModel.AllowedRadiusMeters.ToString(), "نطاق الحضور المسموح بالمتر");






                    db.SaveChanges();
                    AuditService.LogAction("Update Settings", "SystemSettings", "Updated system settings.");
                    TempData["SuccessMessage"] = "تم حفظ إعدادات النظام بنجاح.";

                    return RedirectToAction("Index");
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException e)
                {
                    var errorMessages = e.EntityValidationErrors
                            .SelectMany(x => x.ValidationErrors)
                            .Select(x => x.ErrorMessage);
                    ModelState.AddModelError("", "فشل الحفظ بسبب: " + string.Join("; ", errorMessages));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ غير متوقع: " + ex.Message);
                }
            }

            // إعادة تحميل القوائم في حال وجود خطأ
            ReloadLists();
            return View(viewModel);
        }

        // --- دوال مساعدة ---

        private void ReloadLists()
        {
            // قائمة الحسابات (الفرعية فقط)
            var accountsList = db.Accounts.Where(a => !a.ChildAccounts.Any())
                .Select(a => new { a.Id, Name = a.Code + " - " + a.Name })
                .OrderBy(a => a.Name).ToList();
            ViewBag.AccountsList = new SelectList(accountsList, "Id", "Name");

            // قائمة أنواع الرسوم (الفعالة)
            var feeTypesList = db.FeeTypes.Where(f => f.IsActive)
                .Select(f => new { f.Id, Name = f.Name + " (" + f.DefaultAmount + ")" })
                .ToList();
            ViewBag.FeeTypesList = new SelectList(feeTypesList, "Id", "Name");

            // قائمة أنواع العقود (الفعالة)
            var contractTypesList = db.ContractTypes
                .Select(c => new { c.Id, Name = c.Name })
                .OrderBy(c => c.Name).ToList();
            ViewBag.ContractTypesList = new SelectList(contractTypesList, "Id", "Name");
        }

        private void UpdateSetting(string key, string value, string description = "")
        {
            var setting = db.SystemSettings.Find(key);
            if (setting != null)
            {
                setting.SettingValue = value;
                if (string.IsNullOrEmpty(setting.Description)) setting.Description = description;
            }
            else
            {
                db.SystemSettings.Add(new SystemSetting { SettingKey = key, SettingValue = value, Description = description });
            }
        }

        private void UpdateIntSetting(string key, int? value, string description = "System Link Setting")
        {
            if (value == null) return;

            var setting = db.SystemSettings.Find(key);
            if (setting != null)
            {
                setting.ValueInt = value;
                setting.SettingValue = value.ToString(); // لضمان عدم ترك الحقل فارغاً
            }
            else
            {
                db.SystemSettings.Add(new SystemSetting
                {
                    SettingKey = key,
                    ValueInt = value,
                    SettingValue = value.ToString(),
                    Description = description
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}