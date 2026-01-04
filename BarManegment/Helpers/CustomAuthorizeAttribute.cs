using BarManegment.Models;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace BarManegment.Helpers
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        public string Permission { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            // 1. التأكد من أن المستخدم مسجل دخوله
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            // 2. التحقق من وجود بيانات المستخدم في الجلسة
            if (httpContext.Session["UserTypeId"] == null)
            {
                return false;
            }

            var userTypeId = (int)httpContext.Session["UserTypeId"];
            var controllerName = httpContext.Request.RequestContext.RouteData.Values["controller"].ToString();

            using (var db = new ApplicationDbContext())
            {
                // 3. البحث عن سجل الصلاحية المطابق
                var permissionRecord = db.Permissions
                    .FirstOrDefault(p => p.UserTypeId == userTypeId && p.Module.ControllerName == controllerName);

                if (permissionRecord == null)
                {
                    return false; // لا يوجد سجل صلاحية لهذا الدور على هذه الوحدة
                }

                // 4. إذا لم يتم تحديد صلاحية معينة (مثل القائمة الجانبية)، يكفي التحقق من صلاحية العرض
                if (string.IsNullOrEmpty(Permission))
                {
                    return permissionRecord.CanView;
                }

                // 5. التحقق من الصلاحية المحددة بدقة
                switch (Permission)
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
                        return permissionRecord.CanView; // الافتراضي هو صلاحية العرض
                }
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // الحالة الأولى: المستخدم مسجل دخوله ولكنه لا يملك صلاحية
                // نوجهه إلى صفحة "وصول غير مصرح به"
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "area", "Admin" },
                        { "controller", "Error" },
                        { "action", "Unauthorized" }
                    });
            }
            else
            {
                // الحالة الثانية: المستخدم ليس مسجلاً دخوله أصلاً
                // نوجهه إلى صفحة تسجيل دخول الموظفين
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "area", "Admin" },
                        { "controller", "AdminLogin" },
                        { "action", "Login" },
                        { "returnUrl", filterContext.HttpContext.Request.RawUrl }
                    });
            }
        }
    }
}

