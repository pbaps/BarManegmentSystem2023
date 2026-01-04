using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace BarManegment
{
    public class HandleAntiForgeryErrorAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.Exception is HttpAntiForgeryException)
            {
                filterContext.ExceptionHandled = true;

                // إضافة رسالة توضيحية للمستخدم
                filterContext.Controller.TempData["ErrorMessage"] = "انتهت صلاحية جلستك أو أن الطلب غير صالح. الرجاء تسجيل الدخول مرة أخرى.";

                // === بداية التعديل: آلية التوجيه الذكية ===

                // الحصول على اسم المنطقة الحالية من بيانات المسار
                var area = filterContext.RouteData.DataTokens["area"]?.ToString();

                RouteValueDictionary routeValues;

                switch (area)
                {
                    case "Admin":
                        // توجيه إلى صفحة تسجيل دخول المسؤولين
                        routeValues = new RouteValueDictionary
                        {
                            { "area", "Admin" },
                            { "controller", "AdminLogin" },
                            { "action", "Index" }
                        };
                        break;

                    case "Members":
                        // توجيه إلى صفحة تسجيل دخول الأعضاء
                        routeValues = new RouteValueDictionary
                        {
                            { "area", "Members" },
                            { "controller", "Account" },
                            { "action", "Login" }
                        };
                        break;

                    case "ExamPortal":
                        // توجيه إلى صفحة تسجيل دخول بوابة الامتحان
                        routeValues = new RouteValueDictionary
                        { 
                            { "area", "ExamPortal" },
                            { "controller", "ExamLogin" },
                            { "action", "Index" }
 
                        };
                        break;

                    default:
                        // في أي حالة أخرى، توجيه إلى الصفحة الرئيسية للموقع
                        routeValues = new RouteValueDictionary
                        {
                            { "area", "" },
                            { "controller", "Home" },
                            { "action", "Index" }
                        };
                        break;
                }

                filterContext.Result = new RedirectToRouteResult(routeValues);

                // === نهاية التعديل ===
            }
            else
            {
                base.OnException(filterContext);
            }
        }
    }
}

