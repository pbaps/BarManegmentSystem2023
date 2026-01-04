using System.Web;
using System.Web.Mvc;

namespace BarManegment
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            // === بداية الإضافة: تسجيل معالج الأخطاء الجديد ===
            filters.Add(new HandleAntiForgeryErrorAttribute());
            // === نهاية الإضافة ===
        }
    }
}
