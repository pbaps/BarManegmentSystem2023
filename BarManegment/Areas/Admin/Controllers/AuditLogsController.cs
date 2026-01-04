using BarManegment.Helpers;
using BarManegment.Models;
using PagedList; // تأكد من تثبيت مكتبة PagedList.Mvc
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [Authorize]
    [CustomAuthorize(Permission = "CanView")]
    public class AuditLogsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // تم تحديث الدالة لاستقبال معاملات البحث والفلترة
        public ActionResult Index(string searchString, string actionFilter, DateTime? dateFrom, DateTime? dateTo, int? page)
        {
            // 1. تجهيز الاستعلام الأساسي (AsQueryable)
            var logs = db.AuditLogs.Include(a => a.User).AsQueryable();

            // 2. تطبيق البحث النصي (اسم المستخدم، التفاصيل، اسم المتحكم)
            if (!string.IsNullOrEmpty(searchString))
            {
                logs = logs.Where(l => l.User.FullNameArabic.Contains(searchString) ||
                                       l.Details.Contains(searchString) ||
                                       l.Controller.Contains(searchString));
            }

            // 3. فلترة حسب نوع الإجراء (Create, Edit, Delete...)
            if (!string.IsNullOrEmpty(actionFilter))
            {
                logs = logs.Where(l => l.Action == actionFilter);
            }

            // 4. فلترة حسب التاريخ (من)
            if (dateFrom.HasValue)
            {
                logs = logs.Where(l => l.Timestamp >= dateFrom.Value);
            }

            // 5. فلترة حسب التاريخ (إلى) - نضيف يوماً كاملاً ليشمل نهاية اليوم
            if (dateTo.HasValue)
            {
                var toDate = dateTo.Value.AddDays(1);
                logs = logs.Where(l => l.Timestamp < toDate);
            }

            // 6. الترتيب (الأحدث أولاً)
            logs = logs.OrderByDescending(l => l.Timestamp);

            // 7. إعدادات تقسيم الصفحات
            int pageSize = 20; // عدد السجلات في الصفحة
            int pageNumber = (page ?? 1);

            // 8. حفظ قيم البحث لملء الحقول في الواجهة (View)
            ViewBag.CurrentFilter = searchString;
            ViewBag.ActionFilter = actionFilter;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");

            // إرجاع القائمة مقسمة للصفحة
            return View(logs.ToPagedList(pageNumber, pageSize));
        }

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