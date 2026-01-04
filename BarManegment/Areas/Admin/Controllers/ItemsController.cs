using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BarManegment.Models;
using BarManegment.Helpers;
using System.Net;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class ItemsController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View(db.Items.Include(i => i.ItemCategory).ToList());
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            ViewBag.ItemCategoryId = new SelectList(db.ItemCategories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Item item)
        {
            if (ModelState.IsValid)
            {
                // القيم الأولية يجب أن تكون صفر عند الإنشاء
                item.CurrentQuantity = 0;
                item.AverageCost = 0;

                db.Items.Add(item);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ItemCategoryId = new SelectList(db.ItemCategories, "Id", "Name", item.ItemCategoryId);
            return View(item);
        }


        // إضافة تصنيف جديد عبر Ajax
        [HttpPost]
        public ActionResult AddCategoryJson(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "الاسم مطلوب" });
            }

            // التحقق من التكرار
            if (db.ItemCategories.Any(c => c.Name == name))
            {
                return Json(new { success = false, message = "التصنيف موجود مسبقاً" });
            }

            var category = new ItemCategory { Name = name };
            db.ItemCategories.Add(category);
            db.SaveChanges();

            return Json(new { success = true, id = category.Id, name = category.Name });
        }



        // ... (الكود السابق) ...

        // GET: Edit
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var item = db.Items.Find(id);
            if (item == null) return HttpNotFound();

            ViewBag.ItemCategoryId = new SelectList(db.ItemCategories, "Id", "Name", item.ItemCategoryId);
            return View(item);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(Item item)
        {
            if (ModelState.IsValid)
            {
                // نمنع تعديل الكمية والتكلفة يدوياً لأنها تحسب آلياً
                var existingItem = db.Items.AsNoTracking().FirstOrDefault(i => i.Id == item.Id);
                if (existingItem != null)
                {
                    item.CurrentQuantity = existingItem.CurrentQuantity;
                    item.AverageCost = existingItem.AverageCost;
                }

                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ItemCategoryId = new SelectList(db.ItemCategories, "Id", "Name", item.ItemCategoryId);
            return View(item);
        }

        // POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int id)
        {
            var item = db.Items.Find(id);
            if (item != null)
            {
                // التحقق من الاستخدام
                bool isInUse = db.PurchaseInvoiceItems.Any(x => x.ItemId == id) || db.StockIssueItems.Any(x => x.ItemId == id);
                if (isInUse)
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف هذا الصنف لوجود حركات مخزنية مرتبطة به. يمكنك إلغاء تفعيله بدلاً من ذلك.";
                }
                else
                {
                    db.Items.Remove(item);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "تم حذف الصنف بنجاح.";
                }
            }
            return RedirectToAction("Index");
        }
    }
}