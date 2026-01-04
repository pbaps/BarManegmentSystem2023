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
            // جلب الإعدادات الحالية
            var startDateSetting = db.SystemSettings.Find("ExamRegistrationStartDate");
            var endDateSetting = db.SystemSettings.Find("ExamRegistrationEndDate");
            var highSchoolSetting = db.SystemSettings.Find("MinHighSchoolScore");
            var bachelorSetting = db.SystemSettings.Find("MinBachelorScore");
            var gracePeriodSetting = db.SystemSettings.Find("RenewalGracePeriodEndDate");
            var trainingHoursSetting = db.SystemSettings.Find("RequiredTrainingHours");

            // 👇 جلب إعدادات الرواتب الجديدة
            var annualIncSetting = db.SystemSettings.Find("AnnualIncrementPercent");
            var empPensionSetting = db.SystemSettings.Find("EmployeePensionPercent");
            var employerPensionSetting = db.SystemSettings.Find("EmployerPensionPercent");

            var viewModel = new SystemSettingsViewModel
            {
                ExamRegistrationStartDate = startDateSetting != null ? DateTime.Parse(startDateSetting.SettingValue) : DateTime.Now,
                ExamRegistrationEndDate = endDateSetting != null ? DateTime.Parse(endDateSetting.SettingValue) : DateTime.Now.AddDays(30),

                MinHighSchoolScore = highSchoolSetting != null ? double.Parse(highSchoolSetting.SettingValue, CultureInfo.InvariantCulture) : 50,
                MinBachelorScore = bachelorSetting != null ? double.Parse(bachelorSetting.SettingValue, CultureInfo.InvariantCulture) : 60,

                RenewalGracePeriodEndDate = gracePeriodSetting != null ? DateTime.Parse(gracePeriodSetting.SettingValue) : new DateTime(DateTime.Now.Year, 3, 31),
                RequiredTrainingHours = trainingHoursSetting != null ? int.Parse(trainingHoursSetting.SettingValue) : 100,

                // 👇 تعيين القيم الافتراضية للرواتب (5, 7, 9)
                AnnualIncrementPercent = annualIncSetting != null ? decimal.Parse(annualIncSetting.SettingValue, CultureInfo.InvariantCulture) : 5m,
                EmployeePensionPercent = empPensionSetting != null ? decimal.Parse(empPensionSetting.SettingValue, CultureInfo.InvariantCulture) : 7m,
                EmployerPensionPercent = employerPensionSetting != null ? decimal.Parse(employerPensionSetting.SettingValue, CultureInfo.InvariantCulture) : 9m
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
                // حفظ الإعدادات القديمة
                UpdateSetting("ExamRegistrationStartDate", viewModel.ExamRegistrationStartDate.ToString("yyyy-MM-dd"));
                UpdateSetting("ExamRegistrationEndDate", viewModel.ExamRegistrationEndDate.ToString("yyyy-MM-dd"));
                UpdateSetting("MinHighSchoolScore", viewModel.MinHighSchoolScore.ToString(CultureInfo.InvariantCulture));
                UpdateSetting("MinBachelorScore", viewModel.MinBachelorScore.ToString(CultureInfo.InvariantCulture));
                UpdateSetting("RenewalGracePeriodEndDate", viewModel.RenewalGracePeriodEndDate.ToString("yyyy-MM-dd"));
                UpdateSetting("RequiredTrainingHours", viewModel.RequiredTrainingHours.ToString());

                // 👇 حفظ إعدادات الرواتب الجديدة
                UpdateSetting("AnnualIncrementPercent", viewModel.AnnualIncrementPercent.ToString(CultureInfo.InvariantCulture));
                UpdateSetting("EmployeePensionPercent", viewModel.EmployeePensionPercent.ToString(CultureInfo.InvariantCulture));
                UpdateSetting("EmployerPensionPercent", viewModel.EmployerPensionPercent.ToString(CultureInfo.InvariantCulture));

                db.SaveChanges();

                AuditService.LogAction("Update Settings", "SystemSettings", "Updated system settings including Payroll configurations.");

                TempData["SuccessMessage"] = "تم حفظ إعدادات النظام بنجاح.";
                return RedirectToAction("Index");
            }
            return View(viewModel);
        }

        private void UpdateSetting(string key, string value)
        {
            var setting = db.SystemSettings.Find(key);
            if (setting != null)
            {
                setting.SettingValue = value;
            }
            else
            {
                db.SystemSettings.Add(new SystemSetting { SettingKey = key, SettingValue = value });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}