using System.Web.Mvc;
using System.Web.Routing;

namespace BarManegment
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // ملاحظة: تسجيل مسارات الـ Areas يتم تلقائيًا عبر ملف Global.asax.cs

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                // === بداية التعديل: تغيير الصفحة الافتراضية للمشروع ===
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                // === نهاية التعديل ===

                // إضافة مهمة: تحديد مسار وحدات التحكم الرئيسية لتجنب التعارض مع الـ Areas
                namespaces: new[] { "BarManegment.Controllers" }
            );
        }
    }
}
