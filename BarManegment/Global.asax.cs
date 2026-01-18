// File Path: BarManegment/Global.asax.cs
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
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
            // 💡 إضافة كود تفعيل الـ Migrations والـ Seed تلقائياً
            // =========================================================
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

            var domain = "http://maj.pbaps.ps";
            var webhookUrl = $"{domain}/TelegramBot/Update";

            Task.Run(async () =>
            {
                try
                {
                    await TelegramService.SetWebhookAsync(webhookUrl);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Webhook Error: {ex.Message}");
                }
            });
        } 
       
    }
}