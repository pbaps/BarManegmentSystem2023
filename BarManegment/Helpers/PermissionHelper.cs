using BarManegment.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace BarManegment.Helpers
{
    // كلاس مساعد صغير لتخزين البيانات في السيشن بدلاً من كائنات القاعدة الثقيلة
    public class CachedPermissionDto
    {
        public string ControllerName { get; set; }
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanExport { get; set; }
        public bool CanImport { get; set; }
    }

    public static class PermissionHelper
    {
        // هذه الدالة للتحقق من صلاحية العرض فقط (للقائمة الجانبية)
        public static bool HasPermission(string controllerName)
        {
            return CheckPermission(controllerName, "CanView");
        }

        // ✅ التعديل هنا: تم إضافة المعامل الثالث الاختياري (passedUserTypeId)
        public static bool CheckPermission(string controllerName, string permissionType, int? passedUserTypeId = null)
        {
            int userTypeId;

            // 1. تحديد مصدر UserTypeId (الأولوية للقيمة الممررة من Claims)
            if (passedUserTypeId.HasValue)
            {
                userTypeId = passedUserTypeId.Value;
            }
            else
            {
                // إذا لم تمرر قيمة (مثل الاستدعاء من Views قديمة)، نحاول القراءة من السيشن كخيار أخير
                if (HttpContext.Current?.Session?["UserTypeId"] == null)
                {
                    return false;
                }
                userTypeId = (int)HttpContext.Current.Session["UserTypeId"];
            }

            // 2. محاولة جلب الصلاحيات من الذاكرة (Session Cache) أولاً لتقليل الاستعلامات
            // نستخدم مفتاح تخزين فريد لكل نوع مستخدم لتجنب التداخل
            string cacheKey = $"UserPermissionsList_{userTypeId}";
            var cachedPermissions = HttpContext.Current?.Session?[cacheKey] as List<CachedPermissionDto>;

            // 3. إذا لم تكن موجودة في الذاكرة (أول مرة فقط)، نجلبها من قاعدة البيانات
            if (cachedPermissions == null)
            {
                using (var db = new ApplicationDbContext())
                {
                    // جلب كل الصلاحيات دفعة واحدة لهذا النوع من المستخدمين
                    var dbPermissions = db.Permissions
                                            .Include(p => p.Module) // تضمين جدول الموديول
                                            .Where(p => p.UserTypeId == userTypeId)
                                            .ToList();

                    // تحويلها إلى قائمة خفيفة للتخزين
                    cachedPermissions = dbPermissions.Select(p => new CachedPermissionDto
                    {
                        ControllerName = p.Module.ControllerName,
                        CanView = p.CanView,
                        CanAdd = p.CanAdd,
                        CanEdit = p.CanEdit,
                        CanDelete = p.CanDelete,
                        CanExport = p.CanExport,
                        CanImport = p.CanImport
                    }).ToList();

                    // حفظها في السيشن لعدم تكرار الاستعلام
                    if (HttpContext.Current?.Session != null)
                    {
                        HttpContext.Current.Session[cacheKey] = cachedPermissions;
                    }
                }
            }

            // 4. البحث في القائمة المحفوظة (سريع جداً)
            var permissionRecord = cachedPermissions
                .FirstOrDefault(p => p.ControllerName == controllerName);

            if (permissionRecord == null)
            {
                return false;
            }

            // استخدام switch للتحقق من الصلاحية المطلوبة
            switch (permissionType)
            {
                case "CanAdd":
                    return permissionRecord.CanAdd;
                case "CanEdit":
                    return permissionRecord.CanEdit;
                case "CanDelete":
                    return permissionRecord.CanDelete;
                case "CanExport":
                    return permissionRecord.CanExport;
                case "CanImport":
                    return permissionRecord.CanImport;
                default:
                    return permissionRecord.CanView;
            }
        }

        // دالة لتنظيف الصلاحيات عند الخروج (يجب استدعاؤها في LogOff)
        public static void ClearPermissions()
        {
            if (HttpContext.Current.Session["UserPermissionsList"] != null)
            {
                HttpContext.Current.Session["UserPermissionsList"] = null;
            }
        }
    }
}