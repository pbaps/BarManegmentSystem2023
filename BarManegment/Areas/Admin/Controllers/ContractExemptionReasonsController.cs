using BarManegment.Models;
using BarManegment.Helpers;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    // (نفترض أننا سنستخدم نفس صلاحية "LookupManagement" أو "Provinces" كصلاحية عامة)
    [CustomAuthorize(Permission = "CanView")]
    public class ContractExemptionReasonsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/ContractExemptionReasons
        public ActionResult Index()
        {
            return View(db.ContractExemptionReasons.ToList());
        }

        // GET: Admin/ContractExemptionReasons/Create
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/ContractExemptionReasons/Create
        // 💡💡 === بداية التعديل الكامل === 💡💡
        // POST: Admin/ContractExemptionReasons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]

        // 1. التعديل: لن نستقبل النموذج (Model) بالكامل، سنستقبل الحقل "Reason" كنص (string)
        public ActionResult Create(string Reason)
        {
            // 2. نقوم بالتحقق اليدوي
            if (string.IsNullOrWhiteSpace(Reason))
            {
                ModelState.AddModelError("Reason", "حقل سبب الإعفاء مطلوب.");
                // 3. إرجاع نموذج فارغ (لأن النموذج الأصلي لم يكن صالحاً)
                return View(new ContractExemptionReason { Reason = Reason });
            }

            // 4. (الآن ModelState.IsValid يجب أن يكون true)
            if (ModelState.IsValid)
            {
                // 5. إنشاء الكائن يدوياً
                var reason = new ContractExemptionReason();
                reason.Reason = Reason; // 6. تعيين القيمة النصية يدوياً

                db.ContractExemptionReasons.Add(reason);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم إضافة سبب الإعفاء بنجاح.";
                return RedirectToAction("Index");
            }

            // 7. في حال فشل ModelState لسبب آخر (نادر)
            return View(new ContractExemptionReason { Reason = Reason });
        }
        // 💡💡 === نهاية التعديل الكامل === 💡💡
        // GET: Admin/ContractExemptionReasons/Edit/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ContractExemptionReason reason = db.ContractExemptionReasons.Find(id);
            if (reason == null)
            {
                return HttpNotFound();
            }
            return View(reason);
        }

        // POST: Admin/ContractExemptionReasons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,Reason")] ContractExemptionReason reason)
        {
            if (ModelState.IsValid)
            {
                db.Entry(reason).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل سبب الإعفاء بنجاح.";
                return RedirectToAction("Index");
            }
            return View(reason);
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