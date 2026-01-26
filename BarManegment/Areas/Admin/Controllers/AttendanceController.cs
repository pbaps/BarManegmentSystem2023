using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Data.Entity; // ضروري جداً للعلاقات
using System.Linq;
using System.Web.Mvc;
using BarManegment.Helpers;

namespace BarManegment.Areas.Admin.Controllers
{
    [Authorize]
    public class AttendanceController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. واجهة الموظف الذكية (MyAttendance)
        // ============================================================
        public ActionResult MyAttendance()
        {
            int empId = GetCurrentEmployeeId();
            if (empId == 0)
            {
                return View("AccessDenied");
            }

            var today = DateTime.Today;
            // جلب سجل اليوم للموظف
            var log = db.AttendanceLogs.FirstOrDefault(a => a.EmployeeId == empId && a.Date == today);

            return View(log);
        }

        // ============================================================
        // 2. تسجيل الدخول (AJAX Check-In) - يدعم تعدد الفروع
        // ============================================================
 
        [HttpPost]
        public ActionResult ClockIn(double lat, double lng)
        {
            try
            {
                // 1. التحقق من هوية الموظف
                int empId = GetCurrentEmployeeId();
                if (empId == 0)
                {
                    return Json(new { success = false, message = "عفواً، المستخدم الحالي غير مرتبط بملف موظف." });
                }

                // 2. التحقق من الموقع الجغرافي (Geofencing)
                // هل الموظف موجود داخل نطاق أي فرع من فروع النقابة؟
                var currentBranch = GetCurrentBranch(lat, lng);

                if (currentBranch == null)
                {
                    return Json(new { success = false, message = "فشل التسجيل: أنت خارج النطاق الجغرافي لجميع مقرات النقابة." });
                }

                // 3. التحقق من عدم التكرار (هل سجل اليوم؟)
                var today = DateTime.Today;
                var log = db.AttendanceLogs.FirstOrDefault(a => a.EmployeeId == empId && a.Date == today);

                if (log != null)
                {
                    return Json(new { success = false, message = "تم تسجيل الحضور مسبقاً لهذا اليوم." });
                }

                // 4. تطبيق منطق الدوام الذكي (Smart Shift Logic)
                // نستخدم الخدمة لحساب الحالة (حاضر / متأخر / عمل في عطلة)
                AttendanceService attService = new AttendanceService();
                var currentTime = DateTime.Now.TimeOfDay;
                string status = "حاضر"; // الحالة الافتراضية

                // أ) فحص هل اليوم عطلة رسمية أو أسبوعية؟
                if (!attService.IsWorkingDay(today))
                {
                    // الموظف حضر في يوم إجازة
                    status = "عمل في عطلة";
                }
                else
                {
                    // ب) فحص التأخير (بناءً على إعدادات الوردية وفترة السماح)
                    status = attService.CalculateStatus(currentTime);
                }

                // 5. إنشاء وحفظ السجل
                log = new AttendanceLog
                {
                    EmployeeId = empId,
                    Date = today,
                    CheckInTime = currentTime,
                    Status = status, // الحالة المحسوبة آلياً
                    Method = "GPS-Smart",
                    // توثيق الفرع والإحداثيات لغايات التدقيق
                    SystemNotes = $"تسجيل دخول: {currentBranch.Name}"
                };

                db.AttendanceLogs.Add(log);
                db.SaveChanges();

                // 6. سجل التدقيق
                AuditService.LogAction("Check-In", "Attendance", $"Emp #{empId} Clocked In at {currentBranch.Name}. Status: {status}");

                return Json(new { success = true, time = DateTime.Now.ToString("hh:mm tt"), status = status });
            }
            catch (Exception ex)
            {
                // التعامل مع أي خطأ غير متوقع
                return Json(new { success = false, message = "حدث خطأ أثناء المعالجة: " + ex.Message });
            }
        }

        // ============================================================
        // 3. تسجيل الخروج (AJAX Check-Out)
        // ============================================================
        [HttpPost]
        public ActionResult ClockOut(double lat, double lng)
        {
            int empId = GetCurrentEmployeeId();
            var today = DateTime.Today;
            var log = db.AttendanceLogs.FirstOrDefault(a => a.EmployeeId == empId && a.Date == today);

            if (log != null)
            {
                // التحقق من الموقع عند الخروج أيضاً
                var currentBranch = GetCurrentBranch(lat, lng);
                if (currentBranch == null)
                {
                    return Json(new { success = false, message = "يجب أن تكون داخل أحد المقرات لتسجيل الانصراف." });
                }

                log.CheckOutTime = DateTime.Now.TimeOfDay;

                // تحديث الملاحظات لإثبات مكان الخروج
                log.SystemNotes += $" | انصراف: {currentBranch.Name}";

                db.SaveChanges();

                AuditService.LogAction("Check-Out", "Attendance", $"Employee {empId} checked out at {currentBranch.Name}.");
                return Json(new { success = true, time = DateTime.Now.ToString("hh:mm tt") });
            }

            return Json(new { success = false, message = "لم تقم بتسجيل الدخول اليوم لتتمكن من الخروج." });
        }

        // ============================================================
        // 4. لوحة تحكم المدير (Index) - استعراض السجلات
        // ============================================================
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult Index(DateTime? date, int? departmentId)
        {
            var targetDate = date ?? DateTime.Today;
            ViewBag.SelectedDate = targetDate;
            ViewBag.DepartmentId = new SelectList(db.Departments, "Id", "Name", departmentId);

            // استخدام Include النصي لتجنب مشاكل الـ Lambda
            var query = db.AttendanceLogs.Include("Employee.Department").AsQueryable();

            // الفلترة بالتاريخ
            query = query.Where(a => a.Date == targetDate);

            // الفلترة بالقسم
            if (departmentId.HasValue)
            {
                query = query.Where(a => a.Employee.DepartmentId == departmentId);
            }

            return View(query.OrderBy(a => a.CheckInTime).ToList());
        }

        // ============================================================
        // 5. إدارة الإجازات (Request & Manage)
        // ============================================================

        // الموظف: عرض صفحة الطلب
        public ActionResult RequestLeave()
        {
            return View();
        }

        // الموظف: إرسال الطلب
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestLeave(LeaveRequest request)
        {
            int empId = GetCurrentEmployeeId();
            if (empId == 0) return RedirectToAction("MyAttendance");

            if (ModelState.IsValid)
            {
                // حساب عدد الأيام
                if (request.EndDate >= request.StartDate)
                {
                    request.TotalDays = (int)(request.EndDate - request.StartDate).TotalDays + 1;
                }
                else
                {
                    request.TotalDays = 1;
                }

                request.EmployeeId = empId;
                request.Status = "قيد الانتظار";

                db.LeaveRequests.Add(request);
                db.SaveChanges();

                TempData["SuccessMessage"] = "تم إرسال طلب الإجازة بنجاح.";
                return RedirectToAction("MyAttendance");
            }
            return View(request);
        }

        // المدير: قائمة الطلبات
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ManageLeaves()
        {
            var requests = db.LeaveRequests.Include("Employee.Department")
                             .OrderByDescending(l => l.StartDate)
                             .ToList();
            return View(requests);
        }

        // المدير: اتخاذ قرار
        [HttpPost]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ReviewLeave(int id, string status, string comment)
        {
            var request = db.LeaveRequests.Find(id);
            if (request != null)
            {
                request.Status = status; // مقبول / مرفوض
                request.ManagerComment = comment;
                db.SaveChanges();
                TempData["SuccessMessage"] = $"تم تحديث حالة الطلب إلى: {status}";

                // هنا يمكن إضافة كود لخصم الرصيد إذا كانت الحالة "مقبول"
            }
            return RedirectToAction("ManageLeaves");
        }

        // ============================================================
        // 6. إدارة الأذونات (Permissions)
        // ============================================================

        public ActionResult RequestPermission()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestPermission(HourlyPermission permission)
        {
            int empId = GetCurrentEmployeeId();
            if (empId == 0) return RedirectToAction("MyAttendance");

            if (ModelState.IsValid)
            {
                permission.EmployeeId = empId;
                permission.Date = DateTime.Today; // أو تاريخ محدد إذا أضفناه للواجهة
                permission.Status = "قيد الانتظار";

                db.HourlyPermissions.Add(permission);
                db.SaveChanges();

                TempData["SuccessMessage"] = "تم إرسال طلب الإذن بنجاح.";
                return RedirectToAction("MyAttendance");
            }
            return View(permission);
        }

        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ManagePermissions()
        {
            var permissions = db.HourlyPermissions.Include("Employee")
                                .OrderByDescending(p => p.Date)
                                .ToList();
            return View(permissions);
        }

        [HttpPost]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ReviewPermission(int id, string status)
        {
            var perm = db.HourlyPermissions.Find(id);
            if (perm != null)
            {
                perm.Status = status;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تحديث حالة الإذن.";
            }
            return RedirectToAction("ManagePermissions");
        }

        // ============================================================
        // 7. الدوال المساعدة (Helpers)
        // ============================================================

        private int GetCurrentEmployeeId()
        {
            if (Session["UserId"] == null) return 0;
            int userId = (int)Session["UserId"];
            var emp = db.Employees.FirstOrDefault(e => e.UserId == userId);
            return emp?.Id ?? 0;
        }

        // دالة تحديد الفرع الحالي بناءً على الموقع
        private Branch GetCurrentBranch(double userLat, double userLng)
        {
            // جلب الفروع النشطة فقط
            var branches = db.Branches.Where(b => b.IsActive).ToList();

            foreach (var branch in branches)
            {
                // التأكد من صحة البيانات المخزنة لتجنب الأخطاء
                if (double.TryParse(branch.Latitude, out double bLat) &&
                    double.TryParse(branch.Longitude, out double bLng))
                {
                    var distance = CalculateDistance(userLat, userLng, bLat, bLng);

                    // إذا كان المستخدم ضمن النطاق
                    if (distance <= branch.AllowedRadius)
                    {
                        return branch;
                    }
                }
            }
            return null; // لم يتم العثور على أي فرع قريب
        }

        // معادلة هافرسين لحساب المسافة بالمتر
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371e3; // نصف قطر الأرض بالمتر
            var φ1 = lat1 * Math.PI / 180;
            var φ2 = lat2 * Math.PI / 180;
            var Δφ = (lat2 - lat1) * Math.PI / 180;
            var Δλ = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}