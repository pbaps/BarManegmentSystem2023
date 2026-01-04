using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using System.Collections.Generic; // 💡 إضافة ضرورية لاستخدام List
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    // هذا المتحكم بالكامل خاص بصلاحية "متابعة تنفيذ القرارات"
    [CustomAuthorize(Permission = "CanView")]
    public class DecisionFollowUpController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/DecisionFollowUp
        // GET: Admin/DecisionFollowUp
        public ActionResult Index(int? sessionId)
        {
            // جلب كل القرارات التي تحتاج إجراء
            var decisionsQuery = db.AgendaItems
                .Include(i => i.CouncilSession)
               
            .Where(i => i.ExecutionStatus == "بانتظار التعيين" || i.ExecutionStatus.Contains("أعيد للسكرتير"));

            if (sessionId != null)
            {
                decisionsQuery = decisionsQuery.Where(i => i.CouncilSessionId == sessionId);
                ViewBag.SelectedSession = "جلسة رقم " + db.CouncilSessions.Find(sessionId)?.SessionNumber;
            }
            else
            {
                ViewBag.SelectedSession = "كل الجلسات";
            }

            // --- جلب قائمة الموظفين ---
            var excludedRoles = new List<string> { "Graduate", "Advocate" };
            var employeesList = db.Users
                .Include(u => u.UserType)
                .Where(u => u.UserType != null && !excludedRoles.Contains(u.UserType.NameEnglish))
                .OrderBy(u => u.FullNameArabic)
                .ToList();

            // --- تعبئة الـ ViewModel ---
            var viewModel = new DecisionFollowUpViewModel
            {
                // 1. تمرير قائمة القرارات
                Decisions = decisionsQuery.OrderByDescending(i => i.CouncilSession.SessionDate).ToList(),

                // 2. تمرير القائمة المنسدلة
                EmployeesList = new SelectList(employeesList, "Username", "FullNameArabic"),

                // 3. تمرير خريطة الأسماء (للعرض في الجدول)
                EmployeeNameMap = employeesList.ToDictionary(emp => emp.Username, emp => emp.FullNameArabic)
            };

            return View(viewModel);
        }
        // POST: Admin/DecisionFollowUp/AssignForExecution
        [HttpPost]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignForExecution(int itemId, string employeeUsername)
        {
            var item = db.AgendaItems.Find(itemId);
            if (item != null)
            {
                item.AssignedForExecutionUserId = employeeUsername;
                item.ExecutionStatus = "بانتظار التنفيذ";
                db.SaveChanges();
            }
            return RedirectToAction("Index", new { sessionId = item.CouncilSessionId });
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