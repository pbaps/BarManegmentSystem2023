using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using BarManegment.Helpers;
using BarManegment.Services;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class CoordinatorInboxController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var pendingItems = db.AgendaItems
                .Include(i => i.Attachments)
                .Where(i => i.CouncilSessionId == null)
                .OrderByDescending(i => i.Id)
                .ToList();

            var openSessions = db.CouncilSessions
                .Where(s => !s.IsFinalized)
                .OrderByDescending(s => s.SessionDate)
                .ToList();

            var viewModel = new CoordinatorDashboardViewModel
            {
                PendingItems = pendingItems
            };

            var sessionList = openSessions.Select(s => new
            {
                Id = s.Id,
                Text = $"جلسة رقم {s.SessionNumber} لسنة {s.Year} بتاريخ {s.SessionDate.ToShortDateString()}"
            }).ToList();

            viewModel.OpenSessions = new SelectList(sessionList, "Id", "Text");

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignItemsToSession(int? selectedSessionId, List<int> selectedItemIds)
        {
            // 1. التحقق من البيانات
            if (selectedSessionId == null || selectedSessionId == 0)
            {
                TempData["Error"] = "يرجى اختيار جلسة من القائمة.";
                return RedirectToAction("Index", "CoordinatorInbox", new { area = "Admin" });
            }

            if (selectedItemIds == null || !selectedItemIds.Any())
            {
                TempData["Error"] = "يرجى تحديد بند واحد على الأقل للترحيل.";
                return RedirectToAction("Index", "CoordinatorInbox", new { area = "Admin" });
            }

            // 2. جلب وتحديث البيانات
            var itemsToUpdate = db.AgendaItems
                .Where(i => selectedItemIds.Contains(i.Id))
                .ToList();

            if (itemsToUpdate.Count == 0)
            {
                TempData["Error"] = "لم يتم العثور على البنود المحددة.";
                return RedirectToAction("Index", "CoordinatorInbox", new { area = "Admin" });
            }

            foreach (var item in itemsToUpdate)
            {
                item.CouncilSessionId = selectedSessionId;
                item.CouncilDecisionType = "Pending"; // تصفير القرار ليظهر كبند جديد

                // تحديث نص الحالة ليظهر بوضوح في الجدول الجديد
                if (item.ExecutionStatus != null && (item.ExecutionStatus.Contains("مؤجل") || item.ExecutionStatus.Contains("دراسة")))
                {
                    item.ExecutionStatus = "معاد للعرض - قيد المناقشة";
                }
                else
                {
                    item.ExecutionStatus = "مدرج في الجدول";
                }

                db.Entry(item).State = EntityState.Modified;
            }

            db.SaveChanges();

            AuditService.LogAction("Assign Items", "CoordinatorInbox", $"Assigned {itemsToUpdate.Count} items to Session {selectedSessionId}");

            TempData["Success"] = $"تم ترحيل {itemsToUpdate.Count} بند بنجاح إلى الجلسة المختارة.";

            // 3. التوجيه الصريح (يحل مشكلة الذهاب للرئيسية)
            return RedirectToAction("Index", "CoordinatorInbox", new { area = "Admin" });
        }

        // دالة الحذف (لإكمال الكود)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAgendaItem(int itemId)
        {
            var item = db.AgendaItems.Find(itemId);
            if (item != null && item.CouncilSessionId == null)
            {
                db.AgendaItems.Remove(item);
                db.SaveChanges();
                TempData["Success"] = "تم حذف البند.";
            }
            return RedirectToAction("Index", "CoordinatorInbox", new { area = "Admin" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}