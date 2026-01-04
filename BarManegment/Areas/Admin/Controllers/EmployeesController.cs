using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO; // ضروري للتعامل مع الملفات والمجلدات
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BarManegment.Models;
using BarManegment.Helpers;
using BarManegment.Services;

namespace BarManegment.Areas.Admin.Controllers
{
    [Authorize]
    [CustomAuthorize(Permission = "CanView")]
    public class EmployeesController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. عرض القائمة (Index)
        // ============================================================
        public ActionResult Index()
        {
            var employees = db.Employees
                .Include(e => e.Department)
                .Include(e => e.JobTitle)
                .Include(e => e.User);
            return View(employees.ToList());
        }

        // ============================================================
        // 2. التفاصيل (Details) + السجل المالي
        // ============================================================
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var employee = db.Employees
                             .Include(e => e.Department)
                             .Include(e => e.JobTitle)
                             .Include(e => e.User)
                             .FirstOrDefault(e => e.Id == id);

            if (employee == null) return HttpNotFound();

            // جلب السجل التاريخي للتغييرات المالية
            ViewBag.FinancialHistory = db.EmployeeFinancialHistories
                                         .Where(h => h.EmployeeId == id)
                                         .OrderByDescending(h => h.ChangeDate)
                                         .ToList();

            return View(employee);
        }

        // ============================================================
        // 3. الإضافة (Create)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name");
            ViewBag.JobTitleId = new SelectList(db.JobTitles, "Id", "Name");

            // جلب المستخدمين غير المرتبطين بموظفين سابقاً لتجنب التكرار
            var linkedUserIds = db.Employees.Where(e => e.UserId != null).Select(e => e.UserId).ToList();
            ViewBag.UserId = new SelectList(db.Users.Where(u => u.IsActive && !linkedUserIds.Contains(u.Id)), "Id", "FullNameArabic");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Employee employee)
        {
            if (ModelState.IsValid)
            {
                // --- معالجة رفع الصورة ---
                if (employee.ImageFile != null && employee.ImageFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(employee.ImageFile.FileName);
                    string extension = Path.GetExtension(employee.ImageFile.FileName);
                    fileName = fileName + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                    string uploadDir = Server.MapPath("~/Uploads/Employees/");

                    // التأكد من وجود المجلد وإنشائه إن لم يوجد
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    string path = Path.Combine(uploadDir, fileName);
                    employee.ImageFile.SaveAs(path);
                    employee.ProfilePicturePath = "~/Uploads/Employees/" + fileName;
                }

                employee.IsActive = true;
                db.Employees.Add(employee);
                db.SaveChanges();

                // تسجيل العملية
                AuditService.LogAction("Add Employee", "Employees", $"Name: {employee.FullName}, Salary: {employee.BasicSalary}");

                TempData["SuccessMessage"] = "تم إضافة ملف الموظف بنجاح.";
                return RedirectToAction("Index");
            }

            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name", employee.DepartmentId);
            ViewBag.JobTitleId = new SelectList(db.JobTitles, "Id", "Name", employee.JobTitleId);
            ViewBag.UserId = new SelectList(db.Users.Where(u => u.IsActive), "Id", "FullNameArabic", employee.UserId);
            return View(employee);
        }

        // ============================================================
        // 4. التعديل (Edit) + الأرشفة المالية
        // ============================================================
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var employee = db.Employees.Find(id);
            if (employee == null) return HttpNotFound();

            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name", employee.DepartmentId);
            ViewBag.JobTitleId = new SelectList(db.JobTitles, "Id", "Name", employee.JobTitleId);
            ViewBag.UserId = new SelectList(db.Users.Where(u => u.IsActive), "Id", "FullNameArabic", employee.UserId);
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Employee employee, string ChangeReason)
        {
            if (ModelState.IsValid)
            {
                // 1. جلب البيانات القديمة (AsNoTracking مهم جداً هنا)
                var oldData = db.Employees.AsNoTracking().FirstOrDefault(e => e.Id == employee.Id);

                bool isFinancialChange = false;

                if (oldData != null)
                {
                    // 2. التحقق من التغييرات المالية
                    isFinancialChange =
                        oldData.BasicSalary != employee.BasicSalary ||
                        oldData.ManagerAllowance != employee.ManagerAllowance ||
                        oldData.HeadOfDeptAllowance != employee.HeadOfDeptAllowance ||
                        oldData.MasterDegreeAllowance != employee.MasterDegreeAllowance ||
                        oldData.PhdDegreeAllowance != employee.PhdDegreeAllowance ||
                        oldData.SpecializationAllowance != employee.SpecializationAllowance ||
                        oldData.TransportAllowance != employee.TransportAllowance ||
                        oldData.EmployeePensionPercent != employee.EmployeePensionPercent ||
                        oldData.EmployerPensionPercent != employee.EmployerPensionPercent ||
                        oldData.OtherMonthlyDeduction != employee.OtherMonthlyDeduction;

                    // 3. الأرشفة إذا وجد تغيير
                    if (isFinancialChange)
                    {
                        var historyRecord = new EmployeeFinancialHistory
                        {
                            EmployeeId = oldData.Id,
                            ChangeDate = DateTime.Now,
                            ChangedBy = Session["FullName"]?.ToString() ?? "System",
                            ChangeReason = string.IsNullOrEmpty(ChangeReason) ? "تحديث بيانات مالية" : ChangeReason,

                            BasicSalary = oldData.BasicSalary,
                            ManagerAllowance = oldData.ManagerAllowance,
                            HeadOfDeptAllowance = oldData.HeadOfDeptAllowance,
                            MasterDegreeAllowance = oldData.MasterDegreeAllowance,
                            PhdDegreeAllowance = oldData.PhdDegreeAllowance,
                            SpecializationAllowance = oldData.SpecializationAllowance,
                            TransportAllowance = oldData.TransportAllowance,
                            EmployeePensionPercent = oldData.EmployeePensionPercent,
                            EmployerPensionPercent = oldData.EmployerPensionPercent,
                            OtherMonthlyDeduction = oldData.OtherMonthlyDeduction
                        };
                        db.EmployeeFinancialHistories.Add(historyRecord);
                    }

                    // 4. معالجة الصورة في التعديل
                    if (employee.ImageFile != null && employee.ImageFile.ContentLength > 0)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(employee.ImageFile.FileName);
                        string extension = Path.GetExtension(employee.ImageFile.FileName);
                        fileName = fileName + "_" + DateTime.Now.ToString("yymmssfff") + extension;

                        string uploadDir = Server.MapPath("~/Uploads/Employees/");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        string path = Path.Combine(uploadDir, fileName);
                        employee.ImageFile.SaveAs(path);
                        employee.ProfilePicturePath = "~/Uploads/Employees/" + fileName;
                    }
                    else
                    {
                        // الحفاظ على الصورة القديمة
                        employee.ProfilePicturePath = oldData.ProfilePicturePath;
                    }
                }

                // 5. حفظ التعديلات
                db.Entry(employee).State = EntityState.Modified;
                db.SaveChanges();

                // 6. التدقيق
                if (isFinancialChange)
                {
                    AuditService.LogAction("Update Salary", "Employees", $"Salary updated for {employee.FullName}. Old Snapshot archived.");
                    TempData["SuccessMessage"] = "تم تحديث البيانات وأرشفة السجل المالي السابق.";
                }
                else
                {
                    AuditService.LogAction("Edit Employee", "Employees", $"Updated ID: {employee.Id}");
                    TempData["SuccessMessage"] = "تم تحديث البيانات بنجاح.";
                }

                return RedirectToAction("Index");
            }

            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name", employee.DepartmentId);
            ViewBag.JobTitleId = new SelectList(db.JobTitles, "Id", "Name", employee.JobTitleId);
            ViewBag.UserId = new SelectList(db.Users.Where(u => u.IsActive), "Id", "FullNameArabic", employee.UserId);
            return View(employee);
        }
        // أمر طباعة ملف الموظف
        public ActionResult Print(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var employee = db.Employees
                             .Include(e => e.Department)
                             .Include(e => e.JobTitle)
                             .Include(e => e.User)
                             .FirstOrDefault(e => e.Id == id);

            if (employee == null) return HttpNotFound();

            return View(employee);
        }
        // ============================================================
        // 5. التنظيف (Dispose)
        // ============================================================
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}