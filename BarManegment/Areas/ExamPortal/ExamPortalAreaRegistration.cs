using System.Web.Mvc;

namespace BarManegment.Areas.ExamPortal
{
    public class ExamPortalAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "ExamPortal";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "ExamPortal_default",
                "ExamPortal/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
