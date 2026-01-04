using System.Web.Mvc;

namespace BarManegment.Areas.Members
{
    public class MembersAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Members";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Members_default",
                "Members/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional },
                // === بداية الإضافة: تحديد مسار وحدات التحكم الخاصة بهذه المنطقة ===
                namespaces: new[] { "BarManegment.Areas.Members.Controllers" }
                // === نهاية الإضافة ===
            );
        }
    }
}