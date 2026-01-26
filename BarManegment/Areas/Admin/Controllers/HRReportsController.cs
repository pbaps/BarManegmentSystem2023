using BarManegment.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BarManegment.Helpers; // للصلاحيات

namespace BarManegment.Areas.Admin.Controllers
{
    [Authorize]
    [CustomAuthorize(Permission = "CanViewReports")]
    public class HRReportsController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // الشاشة الرئيسية لاختيار التقارير
        public ActionResult Index()
        {
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name");
            ViewBag.EmployeeId = new SelectList(db.Employees, "Id", "FullName");
            return View();
        }

        // 1. تقرير الحضور الشهري
        public ActionResult MonthlyAttendance(int? empId, int? month, int? year)
        {
            // قيم افتراضية للشهر الحالي
            int selectedMonth = month ?? DateTime.Now.Month;
            int selectedYear = year ?? DateTime.Now.Year;

            var query = db.AttendanceLogs.Include(a => a.Employee.Department).AsQueryable();

            query = query.Where(a => a.Date.Month == selectedMonth && a.Date.Year == selectedYear);

            if (empId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == empId);
            }

            var result = query.OrderBy(a => a.Date).ThenBy(a => a.Employee.FullName).ToList();

            // تمرير معلومات للطباعة
            ViewBag.ReportTitle = $"كشف الحضور والانصراف لشهر {selectedMonth}/{selectedYear}";
            ViewBag.Date = DateTime.Now;

            // نستخدم Layout خاص بالطباعة (سأنشئه لك بالأسفل)
            return View("PrintTemplate", result);
        }

        // 2. طباعة طلب إجازة (للتوقيع)
        public ActionResult PrintLeaveRequest(int id)
        {
            var request = db.LeaveRequests.Include(l => l.Employee.Department).FirstOrDefault(l => l.Id == id);
            if (request == null) return HttpNotFound();

            return View("LeaveRequestPaper", request);
        }

        // 3. طباعة إذن مغادرة
        public ActionResult PrintPermissionRequest(int id)
        {
            var perm = db.HourlyPermissions.Include(p => p.Employee.Department).FirstOrDefault(p => p.Id == id);
            if (perm == null) return HttpNotFound();

            return View("PermissionRequestPaper", perm);
        }
    }
}