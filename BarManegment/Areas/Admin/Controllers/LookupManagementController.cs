using BarManegment.Helpers;
using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BarManegment.Areas.Admin.ViewModels;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class LookupManagementController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // قائمة الجداول "الثابتة" التي لها جداول خاصة في الداتابيز
        // (يجب أن تكون الأسماء هنا بالأحرف الصغيرة لتسهيل المقارنة)
        private readonly List<string> _hardTables = new List<string>
        {
            "usertypes", "genders", "nationalidtypes", "applicationstatuses",
            "qualificationtypes", "attachmenttypes", "currencies", "questiontypes"
        };

        // GET: Admin/LookupManagement
        public ActionResult Index()
        {
            // 1. الجداول الأساسية (الثابتة)
            var lookupTypes = new Dictionary<string, string>
            {
                { "UserTypes", "أدوار المستخدمين" },
                { "Genders", "الأجناس" },
                { "NationalIdTypes", "أنواع الهويات" },
                { "ApplicationStatuses", "حالات الطلبات/المتدربين" },
                { "QualificationTypes", "أنواع المؤهلات" },
                { "AttachmentTypes", "أنواع المرفقات" },
                { "Currencies", "العملات" },
                { "QuestionTypes", "أنواع أسئلة الامتحانات" },
            };

            // 2. الجداول المرنة (المالية والإدارية) - تذهب لجدول SystemLookups
            lookupTypes.Add("PaymentMethod", "طرق الدفع (مالي)");
            lookupTypes.Add("ExpenseType", "أنواع المصروفات (مالي)");
            lookupTypes.Add("BankName", "أسماء البنوك (عام)");

            // === 💡 الإضافات الجديدة ===
            lookupTypes.Add("FinancialAidType", "أنواع المساعدات المالية");
            lookupTypes.Add("WalletProvider", "مزودي المحافظ الإلكترونية");
            // ==========================

            ViewBag.LookupTypes = lookupTypes;
            return View();
        }

        // GET: Admin/LookupManagement/List?type=Genders
        public ActionResult List(string type)
        {
            if (string.IsNullOrEmpty(type)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            string typeLower = type.ToLower();
            object list;

            // التحقق هل هو جدول ثابت أم مرن
            if (_hardTables.Contains(typeLower))
            {
                // جداول ثابتة
                var dbSet = GetDbSetByType(type);
                if (dbSet == null) return HttpNotFound("نوع الجدول غير معروف.");
                list = dbSet.ToListAsync().Result;
            }
            else
            {
                // جداول مرنة (SystemLookup)
                list = db.SystemLookups
                    .Where(x => x.Category == type)
                    .OrderBy(x => x.Name)
                    .ToList();
            }

            ViewBag.LookupType = type;
            ViewBag.LookupTypeName = GetLookupTypeName(type);
            return View(list);
        }

        // GET: Admin/LookupManagement/Create
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(string type)
        {
            if (string.IsNullOrEmpty(type)) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var viewModel = new LookupItemViewModel { Type = type };
            ViewBag.LookupTypeName = GetLookupTypeName(type);
            PrepareViewBagForType(type);

            return View(viewModel);
        }

        // POST: Admin/LookupManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(LookupItemViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string typeLower = viewModel.Type.ToLower();

                    if (_hardTables.Contains(typeLower))
                    {
                        // إضافة لجدول ثابت
                        var dbSet = GetDbSetByType(viewModel.Type);
                        dynamic newItem = CreateAndPopulateItem(viewModel);
                        dbSet.Add(newItem);
                    }
                    else
                    {
                        // إضافة لجدول SystemLookup (مرن)
                        var newItem = new SystemLookup
                        {
                            Category = viewModel.Type,
                            Name = viewModel.Name,
                            IsActive = true
                        };
                        db.SystemLookups.Add(newItem);
                    }

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "تمت إضافة العنصر بنجاح.";
                    return RedirectToAction("List", new { type = viewModel.Type });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                }
            }

            ViewBag.LookupTypeName = GetLookupTypeName(viewModel.Type);
            PrepareViewBagForType(viewModel.Type);
            return View(viewModel);
        }

        // GET: Admin/LookupManagement/Edit
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(string type, int? id)
        {
            if (string.IsNullOrEmpty(type) || id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            LookupItemViewModel viewModel;
            string typeLower = type.ToLower();

            if (_hardTables.Contains(typeLower))
            {
                var dbSet = GetDbSetByType(type);
                dynamic item = dbSet.Find(id);
                if (item == null) return HttpNotFound();
                viewModel = MapToViewModel(item, type);
            }
            else
            {
                // التعامل مع SystemLookup
                var item = db.SystemLookups.Find(id);
                if (item == null) return HttpNotFound();
                viewModel = new LookupItemViewModel
                {
                    Id = item.Id,
                    Type = item.Category,
                    Name = item.Name
                };
            }

            ViewBag.LookupTypeName = GetLookupTypeName(type);
            PrepareViewBagForType(type);
            return View(viewModel);
        }

        // POST: Admin/LookupManagement/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(LookupItemViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string typeLower = viewModel.Type.ToLower();

                    if (_hardTables.Contains(typeLower))
                    {
                        // تعديل جدول ثابت
                        var dbSet = GetDbSetByType(viewModel.Type);
                        dynamic item = dbSet.Find(viewModel.Id);
                        PopulateItemFromViewModel(item, viewModel);
                        db.Entry(item).State = EntityState.Modified;
                    }
                    else
                    {
                        // تعديل SystemLookup
                        var item = db.SystemLookups.Find(viewModel.Id);
                        if (item == null) return HttpNotFound();

                        item.Name = viewModel.Name;
                        db.Entry(item).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "تم تعديل العنصر بنجاح.";
                    return RedirectToAction("List", new { type = viewModel.Type });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                }
            }

            ViewBag.LookupTypeName = GetLookupTypeName(viewModel.Type);
            PrepareViewBagForType(viewModel.Type);
            return View(viewModel);
        }

        // POST: Admin/LookupManagement/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteConfirmed(string type, int id)
        {
            try
            {
                string typeLower = type.ToLower();

                if (_hardTables.Contains(typeLower))
                {
                    var dbSet = GetDbSetByType(type);
                    dynamic item = dbSet.Find(id);
                    if (item != null) dbSet.Remove(item);
                }
                else
                {
                    var item = db.SystemLookups.Find(id);
                    if (item != null) db.SystemLookups.Remove(item);
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حذف العنصر بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف العنصر لأنه مرتبط ببيانات أخرى. (" + ex.Message + ")";
            }

            return RedirectToAction("List", new { type = type });
        }

        // دالة مساعدة لصفحة الحذف
        public ActionResult Delete(string type, int? id)
        {
            if (string.IsNullOrEmpty(type) || id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            string name = "";
            string typeLower = type.ToLower();

            if (_hardTables.Contains(typeLower))
            {
                var dbSet = GetDbSetByType(type);
                dynamic item = dbSet.Find(id);
                if (item == null) return HttpNotFound();
                try { name = item.Name; } catch { try { name = item.NameArabic; } catch { name = "عنصر غير معروف"; } }
            }
            else
            {
                var item = db.SystemLookups.Find(id);
                if (item == null) return HttpNotFound();
                name = item.Name;
            }

            ViewBag.ItemName = name;
            ViewBag.LookupType = type;
            ViewBag.LookupTypeName = GetLookupTypeName(type);
            ViewBag.Id = id;

            return View();
        }

        // ============================================================
        // Helper Methods
        // ============================================================

        private DbSet GetDbSetByType(string type)
        {
            switch (type?.ToLower())
            {
                case "usertypes": return db.UserTypes;
                case "genders": return db.Genders;
                case "nationalidtypes": return db.NationalIdTypes;
                case "applicationstatuses": return db.ApplicationStatuses;
                case "qualificationtypes": return db.QualificationTypes;
                case "attachmenttypes": return db.AttachmentTypes;
                case "currencies": return db.Currencies;
                case "questiontypes": return db.QuestionTypes;
                default: return null;
            }
        }

        private string GetLookupTypeName(string type)
        {
            var allTypes = new Dictionary<string, string>
            {
                { "UserTypes", "أدوار المستخدمين" },
                { "Genders", "الأجناس" },
                { "NationalIdTypes", "أنواع الهويات" },
                { "ApplicationStatuses", "حالات الطلبات" },
                { "QualificationTypes", "المؤهلات العلمية" },
                { "AttachmentTypes", "المرفقات" },
                { "Currencies", "العملات" },
                { "QuestionTypes", "أنواع الأسئلة" },
                { "PaymentMethod", "طرق الدفع" },
                { "ExpenseType", "أنواع المصروفات" },
                { "BankName", "أسماء البنوك" },
                
                // === 💡 الإضافات الجديدة ===
                { "FinancialAidType", "أنواع المساعدات المالية" },
                { "WalletProvider", "مزودي المحافظ الإلكترونية" }
                // ==========================
            };

            return allTypes.TryGetValue(type, out string name) ? name : type;
        }

        private dynamic CreateAndPopulateItem(LookupItemViewModel viewModel)
        {
            switch (viewModel.Type?.ToLower())
            {
                case "usertypes": return new UserTypeModel { NameArabic = viewModel.Name, NameEnglish = viewModel.Symbol };
                case "genders": return new Gender { Name = viewModel.Name };
                case "nationalidtypes": return new NationalIdType { Name = viewModel.Name };
                case "applicationstatuses": return new ApplicationStatus { Name = viewModel.Name };
                case "qualificationtypes": return new QualificationType { Name = viewModel.Name, MinimumAcceptancePercentage = viewModel.PercentageValue };
                case "attachmenttypes": return new AttachmentType { Name = viewModel.Name };
                case "currencies": return new Currency { Name = viewModel.Name, Symbol = viewModel.Symbol };
                case "questiontypes": return new QuestionType { Name = viewModel.Name };
                default: return null;
            }
        }

        private void PopulateItemFromViewModel(dynamic item, LookupItemViewModel viewModel)
        {
            string type = viewModel.Type?.ToLower();

            if (type == "usertypes")
            {
                item.NameArabic = viewModel.Name;
                item.NameEnglish = viewModel.Symbol;
            }
            else if (type == "qualificationtypes")
            {
                item.Name = viewModel.Name;
                item.MinimumAcceptancePercentage = viewModel.PercentageValue;
            }
            else if (type == "currencies")
            {
                item.Name = viewModel.Name;
                item.Symbol = viewModel.Symbol;
            }
            else
            {
                item.Name = viewModel.Name;
            }
        }

        private LookupItemViewModel MapToViewModel(dynamic item, string type)
        {
            var viewModel = new LookupItemViewModel { Id = item.Id, Type = type };
            string typeLower = type?.ToLower();

            if (typeLower == "usertypes")
            {
                viewModel.Name = item.NameArabic;
                viewModel.Symbol = item.NameEnglish;
            }
            else if (typeLower == "qualificationtypes")
            {
                viewModel.Name = item.Name;
                viewModel.PercentageValue = item.MinimumAcceptancePercentage;
            }
            else if (typeLower == "currencies")
            {
                viewModel.Name = item.Name;
                viewModel.Symbol = item.Symbol;
            }
            else
            {
                viewModel.Name = item.Name;
            }
            return viewModel;
        }

        private void PrepareViewBagForType(string type)
        {
            // يمكن إضافة منطق خاص هنا إذا لزم الأمر
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}