using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services; // تأكد من استدعاء خدمة التدقيق
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "FinancialSetup")] // صلاحية الوصول للمتحكم بالكامل
    public class FinancialSetupController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. إدارة دليل الحسابات (Chart of Accounts)
        // ============================================================

        // عرض الدليل
        public ActionResult AccountsIndex()
        {
            var accounts = db.Accounts.OrderBy(a => a.Code).ToList();
            return View(accounts);
        }

        // إنشاء حساب (GET)
        public ActionResult CreateAccount()
        {
            // نرسل قائمة بالحسابات "الرئيسية" فقط لتكون أباً للحساب الجديد
            var parents = db.Accounts
                .Where(a => !a.IsTransactional)
                .OrderBy(a => a.Code)
                .Select(a => new { a.Id, Name = a.Code + " - " + a.Name })
                .ToList();

            ViewBag.ParentId = new SelectList(parents, "Id", "Name");
            return View();
        }

        // حفظ الحساب (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAccount(Account account)
        {
            if (ModelState.IsValid)
            {
                // 1. التحقق من تكرار الكود
                if (db.Accounts.Any(a => a.Code == account.Code))
                {
                    ModelState.AddModelError("Code", "رمز الحساب موجود مسبقاً.");
                }
                else
                {
                    // 2. ضبط المستوى (Level) بناءً على الأب
                    if (account.ParentId.HasValue)
                    {
                        var parent = db.Accounts.Find(account.ParentId);
                        if (parent != null)
                        {
                            account.Level = parent.Level + 1;

                            // التحقق من أن الكود يبدأ بكود الأب
                            if (!account.Code.StartsWith(parent.Code))
                            {
                                ModelState.AddModelError("Code", $"يجب أن يبدأ الرمز بـ {parent.Code} ليتبع له.");
                                ReloadAccountViewBag(account.ParentId);
                                return View(account);
                            }
                        }
                    }
                    else
                    {
                        account.Level = 1; // حساب رئيسي
                    }

                    db.Accounts.Add(account);
                    db.SaveChanges();

                    // تسجيل في سجل التدقيق
                    AuditService.LogAction("Create Account", "FinancialSetup", $"Added Account: {account.Code} - {account.Name}");

                    TempData["SuccessMessage"] = "تم إضافة الحساب بنجاح.";
                    return RedirectToAction("AccountsIndex");
                }
            }

            ReloadAccountViewBag(account.ParentId);
            return View(account);
        }

        // تعديل حساب (GET)
        public ActionResult EditAccount(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var account = db.Accounts.Find(id);
            if (account == null) return HttpNotFound();

            ReloadAccountViewBag(account.ParentId, account.Id);
            return View(account);
        }

        // حفظ التعديل (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAccount(Account account)
        {
            if (ModelState.IsValid)
            {
                var dbAccount = db.Accounts.Find(account.Id);
                if (dbAccount == null) return HttpNotFound();

                // حفظ القيم القديمة للمقارنة في التدقيق (اختياري)
                string oldName = dbAccount.Name;

                dbAccount.Name = account.Name;
                dbAccount.Code = account.Code;
                dbAccount.ParentId = account.ParentId;
                // ملاحظة: يفضل عدم تغيير AccountType أو IsTransactional إذا كان عليه حركات

                db.Entry(dbAccount).State = EntityState.Modified;
                db.SaveChanges();

                // تسجيل في سجل التدقيق
                AuditService.LogAction("Edit Account", "FinancialSetup", $"Updated Account ID {account.Id} from {oldName} to {account.Name}");

                TempData["SuccessMessage"] = "تم تعديل الحساب بنجاح.";
                return RedirectToAction("AccountsIndex");
            }
            ReloadAccountViewBag(account.ParentId, account.Id);
            return View(account);
        }

        // حذف حساب (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")] // صلاحية خاصة للحذف
        public ActionResult DeleteAccount(int id)
        {
            var account = db.Accounts.Find(id);
            if (account == null) return HttpNotFound();

            // 1. منع الحذف إذا كان له أبناء
            if (db.Accounts.Any(a => a.ParentId == id))
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذا الحساب لأنه يحتوي على حسابات فرعية.";
                return RedirectToAction("AccountsIndex");
            }

            // 2. منع الحذف إذا كان مرتبط بقيود
            if (db.JournalEntryDetails.Any(d => d.AccountId == id))
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذا الحساب لوجود حركات مالية مسجلة عليه.";
                return RedirectToAction("AccountsIndex");
            }

            db.Accounts.Remove(account);
            db.SaveChanges();

            // تسجيل التدقيق
            AuditService.LogAction("Delete Account", "FinancialSetup", $"Deleted Account: {account.Code} - {account.Name}");

            TempData["SuccessMessage"] = "تم حذف الحساب بنجاح.";
            return RedirectToAction("AccountsIndex");
        }

        // دالة مساعدة لتعبئة القوائم
        private void ReloadAccountViewBag(int? selectedId = null, int? excludeId = null)
        {
            var query = db.Accounts.Where(a => !a.IsTransactional);
            if (excludeId.HasValue)
            {
                query = query.Where(a => a.Id != excludeId.Value);
            }

            var parents = query.OrderBy(a => a.Code)
                               .Select(a => new { a.Id, Name = a.Code + " - " + a.Name })
                               .ToList();

            ViewBag.ParentId = new SelectList(parents, "Id", "Name", selectedId);
        }


        // ============================================================
        // 2. مراكز التكلفة (Cost Centers)
        // ============================================================

        // عرض المراكز
        public ActionResult CostCentersIndex()
        {
            return View(db.CostCenters.OrderBy(c => c.Code).ToList());
        }

        // إنشاء مركز (GET)
        public ActionResult CreateCostCenter()
        {
            // ✅ تحميل القائمة المنسدلة للأب (لتجنب خطأ null)
            ViewBag.ParentId = new SelectList(db.CostCenters, "Id", "Name");
            return View();
        }

        // حفظ المركز (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCostCenter(CostCenter model)
        {
            if (ModelState.IsValid)
            {
                if (db.CostCenters.Any(c => c.Code == model.Code))
                {
                    ModelState.AddModelError("Code", "رمز المركز موجود مسبقاً");
                    ViewBag.ParentId = new SelectList(db.CostCenters, "Id", "Name", model.ParentId);
                    return View(model);
                }

                db.CostCenters.Add(model);
                db.SaveChanges();

                AuditService.LogAction("Create CostCenter", "FinancialSetup", $"Added CostCenter: {model.Code} - {model.Name}");

                TempData["SuccessMessage"] = "تم إضافة مركز التكلفة.";
                return RedirectToAction("CostCentersIndex");
            }

            // إعادة التعبئة عند الخطأ
            ViewBag.ParentId = new SelectList(db.CostCenters, "Id", "Name", model.ParentId);
            return View(model);
        }

        // تعديل مركز (GET)
        public ActionResult EditCostCenter(int id)
        {
            var cc = db.CostCenters.Find(id);
            if (cc == null) return HttpNotFound();

            // ✅ استثناء المركز نفسه من القائمة
            ViewBag.ParentId = new SelectList(db.CostCenters.Where(c => c.Id != id), "Id", "Name", cc.ParentId);
            return View(cc);
        }

        // حفظ التعديل (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCostCenter(CostCenter model)
        {
            if (ModelState.IsValid)
            {
                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();

                AuditService.LogAction("Edit CostCenter", "FinancialSetup", $"Updated CostCenter ID: {model.Id}");

                TempData["SuccessMessage"] = "تم التعديل بنجاح.";
                return RedirectToAction("CostCentersIndex");
            }

            // إعادة التعبئة عند الخطأ
            ViewBag.ParentId = new SelectList(db.CostCenters.Where(c => c.Id != model.Id), "Id", "Name", model.ParentId);
            return View(model);
        }

        // حذف مركز (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteCostCenter(int id)
        {
            var cc = db.CostCenters.Find(id);
            if (cc == null) return HttpNotFound();

            // منع الحذف إذا كان أباً لمراكز أخرى
            if (db.CostCenters.Any(c => c.ParentId == id))
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذا المركز لأنه مرتبط بمراكز فرعية.";
                return RedirectToAction("CostCentersIndex");
            }

            // (اختياري) منع الحذف إذا كان مستخدماً في قيود أو موازنات
            /*
            if (db.JournalEntryDetails.Any(j => j.CostCenterId == id)) { ... }
            */

            db.CostCenters.Remove(cc);
            db.SaveChanges();

            AuditService.LogAction("Delete CostCenter", "FinancialSetup", $"Deleted CostCenter: {cc.Name}");

            TempData["SuccessMessage"] = "تم حذف مركز التكلفة.";
            return RedirectToAction("CostCentersIndex");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}