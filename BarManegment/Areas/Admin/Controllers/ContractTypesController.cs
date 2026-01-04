using BarManegment.Models;
using BarManegment.Helpers;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    // افترض أننا سنضيف صلاحية "ContractTypes" لاحقاً
    [CustomAuthorize(Permission = "CanView")]
    public class ContractTypesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/ContractTypes
        public ActionResult Index()
        {
            // نستخدم Include لجلب اسم العملة مع النوع
            var contractTypes = db.ContractTypes.Include(c => c.Currency).ToList();
            return View(contractTypes);
        }

        // GET: Admin/ContractTypes/Create
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            // إرسال قائمة العملات إلى الواجهة
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name");
            return View();
        }

        // POST: Admin/ContractTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create([Bind(Include = "Id,Name,DefaultFee,CurrencyId,LawyerPercentage,BarSharePercentage")] ContractType contractType)
        {
            // التحقق من أن مجموع النسب يساوي 1 (100%)
            if (contractType.LawyerPercentage + contractType.BarSharePercentage != 1.00m)
            {
                ModelState.AddModelError("LawyerPercentage", "مجموع حصة المحامي وحصة النقابة يجب أن يساوي 1 (مثال: 0.60 و 0.40).");
            }

            if (ModelState.IsValid)
            {
                db.ContractTypes.Add(contractType);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم إضافة نوع العقد بنجاح.";
                return RedirectToAction("Index");
            }

            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name", contractType.CurrencyId);
            return View(contractType);
        }

        // GET: Admin/ContractTypes/Edit/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ContractType contractType = db.ContractTypes.Find(id);
            if (contractType == null)
            {
                return HttpNotFound();
            }
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name", contractType.CurrencyId);
            return View(contractType);
        }

        // POST: Admin/ContractTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,Name,DefaultFee,CurrencyId,LawyerPercentage,BarSharePercentage")] ContractType contractType)
        {
            if (contractType.LawyerPercentage + contractType.BarSharePercentage != 1.00m)
            {
                ModelState.AddModelError("LawyerPercentage", "مجموع حصة المحامي وحصة النقابة يجب أن يساوي 1 (مثال: 0.60 و 0.40).");
            }

            if (ModelState.IsValid)
            {
                db.Entry(contractType).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل نوع العقد بنجاح.";
                return RedirectToAction("Index");
            }
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name", contractType.CurrencyId);
            return View(contractType);
        }

        // (يمكن إضافة دالة الحذف (Delete) لاحقاً إذا احتجت إليها)

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