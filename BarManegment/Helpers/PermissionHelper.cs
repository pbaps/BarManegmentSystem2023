using BarManegment.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity; // ضروري لعمل Include

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

        // دالة عامة للتحقق من أي صلاحية محددة
        public static bool CheckPermission(string controllerName, string permissionType)
        {
            // 1. التحقق من وجود المستخدم
            if (HttpContext.Current.Session["UserTypeId"] == null)
            {
                return false;
            }

            int userTypeId = (int)HttpContext.Current.Session["UserTypeId"];

            // 2. محاولة جلب الصلاحيات من الذاكرة (Session) أولاً
            var cachedPermissions = HttpContext.Current.Session["UserPermissionsList"] as List<CachedPermissionDto>;

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
                    HttpContext.Current.Session["UserPermissionsList"] = cachedPermissions;
                }
            }

            // 4. البحث في الذاكرة (سريع جداً)
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