using BarManegment.Models;
using BarManegment.Helpers;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [Authorize]
    [CustomAuthorize(Permission = "CanManageShifts")] // تأكد من إضافة الصلاحية لاحقاً
    public class WorkShiftsController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // عرض قائمة الورديات
        public ActionResult Index()
        {
            return View(db.WorkShifts.ToList());
        }

        // إضافة وردية جديدة
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(WorkShift shift)
        {
            if (ModelState.IsValid)
            {
                // إذا تم تحديد هذا الدوام كافتراضي، نلغي الافتراضي عن البقية
                if (shift.IsDefault)
                {
                    var others = db.WorkShifts.Where(s => s.IsDefault).ToList();
                    foreach (var s in others) s.IsDefault = false;
                }

                db.WorkShifts.Add(shift);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حفظ إعدادات الدوام بنجاح.";
                return RedirectToAction("Index");
            }
            return View(shift);
        }

        // تعديل وردية
        public ActionResult Edit(int id)
        {
            var shift = db.WorkShifts.Find(id);
            if (shift == null) return HttpNotFound();
            return View(shift);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(WorkShift shift)
        {
            if (ModelState.IsValid)
            {
                if (shift.IsDefault)
                {
                    var others = db.WorkShifts.Where(s => s.IsDefault && s.Id != shift.Id).ToList();
                    foreach (var s in others) s.IsDefault = false;
                }

                db.Entry(shift).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل إعدادات الدوام بنجاح.";
                return RedirectToAction("Index");
            }
            return View(shift);
        }

        // حذف وردية
        [HttpPost]
        public ActionResult Delete(int id)
        {
            var shift = db.WorkShifts.Find(id);
            if (shift != null)
            {
                db.WorkShifts.Remove(shift);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حذف الوردية.";
            }
            return RedirectToAction("Index");
        }
    }
}