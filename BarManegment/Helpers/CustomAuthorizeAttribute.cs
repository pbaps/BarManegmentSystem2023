using BarManegment.Models;
using System;
using System.Linq;
using System.Security.Claims;
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
            // 1. التحقق من صحة السياق
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            // 2. التأكد من أن المستخدم مسجل دخوله (Authentication)
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return false;
            }

            // 3. جلب بيانات المستخدم من الـ Claims (بدلاً من Session)
            var identity = httpContext.User.Identity as ClaimsIdentity;
            if (identity == null) return false;

            // البحث عن Claim الخاص بنوع المستخدم (UserTypeId)
            var userTypeIdClaim = identity.FindFirst("UserTypeId");

            // إذا لم نجد الـ Claim، فهذا يعني أن الكوكيز قديمة أو البيانات ناقصة
            if (userTypeIdClaim == null)
            {
                return false;
            }

            // تحويل القيمة إلى رقم
            if (!int.TryParse(userTypeIdClaim.Value, out int userTypeId))
            {
                return false;
            }

            // 4. تحديد اسم المتحكم الحالي
            var routeData = httpContext.Request.RequestContext.RouteData;
            string controllerName = routeData.Values["controller"].ToString();

            // 5. التحقق من الصلاحيات باستخدام الدالة المساعدة
            // ملاحظة: سنحتاج لتعديل دالة PermissionHelper.CheckPermission لتقبل userTypeId كـ parameter
            // أو يمكنك تركها تقرأ من السيشن إذا كنت مصراً، لكن الأفضل تمرير القيمة هنا
            return PermissionHelper.CheckPermission(controllerName, Permission ?? "CanView", userTypeId);
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // مسجل دخول لكن ليس لديه صلاحية -> صفحة خطأ
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
                // غير مسجل دخول -> صفحة الدخول
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

