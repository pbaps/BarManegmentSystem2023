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

            // 3. محاولة جلب UserTypeId (نقبل المصدرين: Claims أو Session)
            int? userTypeId = null;

            // أ) المحاولة الأولى: من الـ Claims (للمستقبل)
            var identity = httpContext.User.Identity as ClaimsIdentity;
            if (identity != null)
            {
                var claim = identity.FindFirst("UserTypeId");
                if (claim != null && int.TryParse(claim.Value, out int id))
                {
                    userTypeId = id;
                }
            }

            // ب) المحاولة الثانية: من الـ Session (وهو المستخدم حالياً في AdminLoginController)
            if (userTypeId == null && httpContext.Session["UserTypeId"] != null)
            {
                userTypeId = (int)httpContext.Session["UserTypeId"];
            }

            // إذا لم نجد نوع المستخدم في أي مكان، نرفض الدخول
            if (userTypeId == null)
            {
                return false;
            }

            // 4. تحديد اسم المتحكم الحالي
            var routeData = httpContext.Request.RequestContext.RouteData;
            string controllerName = routeData.Values["controller"].ToString();

            // 5. التحقق من الصلاحيات باستخدام الدالة المساعدة
            // نمرر الـ userTypeId الذي عثرنا عليه لضمان الدقة
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