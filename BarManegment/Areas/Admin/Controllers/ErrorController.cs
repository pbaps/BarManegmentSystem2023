using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Admin/Error/Unauthorized
        [AllowAnonymous] // للسماح بعرض هذه الصفحة حتى لو لم يكن المستخدم مسجلاً دخوله
        public ActionResult Unauthorized()
        {
            return View();
        }
    }
}
