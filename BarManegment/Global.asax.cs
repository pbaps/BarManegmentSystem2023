// File Path: BarManegment/Global.asax.cs
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations; // يمكن إزالة هذا الـ using إذا حذفت الكود
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace BarManegment
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // =========================================================
            // 🛑 تم إيقاف التحديث التلقائي لقاعدة البيانات (مهم للاستضافة)
            // في بيئة الإنتاج (Production)، يتم تحديث القاعدة يدوياً عبر SQL Script
            // =========================================================

            /* try
            {
                var configuration = new BarManegment.Migrations.Configuration();
                var migrator = new DbMigrator(configuration);
                migrator.Update();
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ بدلاً من إيقاف التطبيق
                // System.Diagnostics.Trace.TraceError("Migration Error: " + ex.Message);
            }
      

      try
      {
          var configuration = new BarManegment.Migrations.Configuration();
          var migrator = new DbMigrator(configuration);
          migrator.Update();
      }
      catch (Exception ex)
      {
          // في حال فشل الاتصال أو وجود خطأ في قاعدة البيانات
          throw new Exception("فشل تحديث قاعدة البيانات تلقائياً: " + ex.Message);
      }
        */
        }
    }
}