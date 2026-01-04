using BarManegment.Models;
using BarManegment.Helpers;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class DepartmentsController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View(db.Departments.ToList());
        }

        // GET: Admin/Departments/Create
        public ActionResult Create()
        {
            // جلب القيم الافتراضية من إعدادات النظام
            // نستخدم ?? "5" كقيمة احتياطية في حال لم تكن الإعدادات محفوظة
            var incrementSetting = db.SystemSettings.Find("AnnualIncrementPercent")?.SettingValue ?? "5";
            var empPensionSetting = db.SystemSettings.Find("EmployeePensionPercent")?.SettingValue ?? "7";
            var employerPensionSetting = db.SystemSettings.Find("EmployerPensionPercent")?.SettingValue ?? "9";

            var model = new Department
            {
                // تعيين القيم الافتراضية
                AnnualIncrementPercent = decimal.Parse(incrementSetting),
                EmployeePensionPercent = decimal.Parse(empPensionSetting),
                EmployerPensionPercent = decimal.Parse(employerPensionSetting)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Department department)
        {
            if (ModelState.IsValid)
            {
                db.Departments.Add(department);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(department);
        }
        // --- تعديل (Edit) ---
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var department = db.Departments.Find(id);
            if (department == null) return HttpNotFound();
            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Department department)
        {
            if (ModelState.IsValid)
            {
                db.Entry(department).State = EntityState.Modified;
                db.SaveChanges();
                BarManegment.Services.AuditService.LogAction("Edit Department", "Departments", $"Updated ID: {department.Id}");
                return RedirectToAction("Index");
            }
            return View(department);
        }

        // --- حذف (Delete) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int id)
        {
            var department = db.Departments.Find(id);
            if (department != null)
            {
                // حماية: منع الحذف إذا كان هناك موظفين في هذا القسم
                if (db.Employees.Any(e => e.DepartmentId == id))
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف هذا القسم لأنه مرتبط بموظفين حاليين.";
                    return RedirectToAction("Index");
                }

                db.Departments.Remove(department);
                db.SaveChanges();
                BarManegment.Services.AuditService.LogAction("Delete Department", "Departments", $"Deleted ID: {id}");
                TempData["SuccessMessage"] = "تم حذف القسم بنجاح.";
            }
            return RedirectToAction("Index");
        }
    }
}