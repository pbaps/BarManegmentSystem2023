using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Controllers
{
 
   public class HomeController : Controller
    {
        [AllowAnonymous] // <-- التأكد من أن هذه الصفحة متاحة للجميع
        public ActionResult Index()
        {
            // إذا كان المستخدم مسجلاً دخوله كموظف، قم بتوجيهه إلى لوحة التحكم مباشرة
            if (User.Identity.IsAuthenticated && Session["UserId"] != null)
            {
                return RedirectToAction("Index", "GraduateApplications", new { area = "Admin" });
            }

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}