using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services; // للتأكد من وجود AuditService
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [Authorize]
    [CustomAuthorize(Permission = "CanManageBranches")] // تأكد من إضافة هذا الإذن في جدول الصلاحيات
    public class BranchesController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // 1. عرض قائمة الفروع
        public ActionResult Index()
        {
            return View(db.Branches.ToList());
        }

        // 2. إنشاء فرع جديد (GET)
        public ActionResult Create()
        {
            return View();
        }

        // 3. حفظ الفرع الجديد (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Branch branch)
        {
            if (ModelState.IsValid)
            {
                db.Branches.Add(branch);
                db.SaveChanges();

                AuditService.LogAction("Add Branch", "Branches", $"Added branch: {branch.Name}");
                TempData["SuccessMessage"] = "تم إضافة الفرع الجديد بنجاح.";
                return RedirectToAction("Index");
            }

            return View(branch);
        }

        // 4. تعديل فرع (GET)
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var branch = db.Branches.Find(id);
            if (branch == null) return HttpNotFound();
            return View(branch);
        }

        // 5. حفظ التعديل (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Branch branch)
        {
            if (ModelState.IsValid)
            {
                db.Entry(branch).State = EntityState.Modified;
                db.SaveChanges();

                AuditService.LogAction("Edit Branch", "Branches", $"Edited branch: {branch.Name}");
                TempData["SuccessMessage"] = "تم تحديث بيانات الفرع بنجاح.";
                return RedirectToAction("Index");
            }
            return View(branch);
        }

        // 6. حذف فرع (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var branch = db.Branches.Find(id);
            if (branch != null)
            {
                // يمكن إضافة تحقق هنا إذا كان هناك سجلات حضور مرتبطة بهذا الفرع
                // لكن للحفاظ على السجلات التاريخية، يفضل عمل Soft Delete (إلغاء تفعيل)
                // هنا سنقوم بالحذف المباشر حسب الطلب، أو يمكنك استخدام IsActive = false

                db.Branches.Remove(branch);
                db.SaveChanges();

                AuditService.LogAction("Delete Branch", "Branches", $"Deleted branch: {branch.Name}");
                TempData["SuccessMessage"] = "تم حذف الفرع بنجاح.";
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}