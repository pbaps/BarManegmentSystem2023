using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class MyExecutionTasksController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/MyExecutionTasks
        public ActionResult Index()
        {
            // ✅ (تصحيح) استخدام اسم المستخدم للمطابقة
            var currentUsername = User.Identity.Name;
            var tasks = db.AgendaItems
                .Include(i => i.CouncilSession)
                .Where(i => i.AssignedForExecutionUserId == currentUsername && i.ExecutionStatus == "بانتظار التنفيذ")
                .ToList();

            return View(tasks);
        }

        // =======================================================
        // ===           ✅ (جديد) الدالة المطورة للتنفيذ          ===
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")] // أو "CanView"
        public ActionResult SubmitExecutionAction(int itemId, string executionNotes, string selectedAction)
        {
            var item = db.AgendaItems.Find(itemId);
            if (item == null) return HttpNotFound();

            // الحصول على اسم الموظف الحالي لتوثيقه في سجل التدقيق
            var currentUsernameForLog = Session["FullName"]?.ToString() ?? User.Identity.Name;

            // 1. حفظ ملاحظات الموظف (سواء نفذ أو أعاد)
            item.EmployeeExecutionNotes = executionNotes;

            // 2. تحديد الإجراء بناءً على اختيار الموظف
            switch (selectedAction)
            {
                case "Execute":
                    item.ExecutionStatus = "تم التنفيذ";
                    AuditService.LogAction("ExecuteDecision", "MyExecutionTasks", $"قام الموظف '{currentUsernameForLog}' بتنفيذ القرار {item.Id} (العنوان: {item.Title}).");
                    break;

                case "ReturnToSecretary":
                    item.ExecutionStatus = "أعيد للسكرتير (للمراجعة)";
                    item.AssignedForExecutionUserId = null; // إزالة التكليف عن الموظف
                    AuditService.LogAction("ReturnDecision", "MyExecutionTasks", $"أعاد الموظف '{currentUsernameForLog}' القرار {item.Id} للمراجعة مع ملاحظات.");
                    break;

                    // (يمكن إضافة حالات مستقبلية هنا مثل "ReferToManager" لو تطلب الأمر)
            }

            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // ❌ (تأكد من حذف دالة MarkAsExecuted القديمة من هنا)


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