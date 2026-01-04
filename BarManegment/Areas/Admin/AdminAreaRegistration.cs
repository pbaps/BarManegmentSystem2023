using System.Web.Mvc;

namespace BarManegment.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Admin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Admin_default",
                "Admin/{controller}/{action}/{id}", // <-- ✅ السطر الأهم
                new { action = "Index", id = UrlParameter.Optional } // <-- إخباره أن الـ id اختياري
            );
        }
    }
}