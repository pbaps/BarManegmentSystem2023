// File Path: BarManegment/Global.asax.cs
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
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
                // 👇 التعديل هنا: استخدام المسار الكامل BarManegment.Migrations.Configuration
                var configuration = new BarManegment.Migrations.Configuration();

                var migrator = new DbMigrator(configuration);
                migrator.Update(); // هذا السطر ينفذ أي Migrations ناقصة ويشغل دالة Seed
            }
            catch (Exception ex)
            {
                // في حال فشل الاتصال أو وجود خطأ في قاعدة البيانات
                throw new Exception("فشل تحديث قاعدة البيانات تلقائياً: " + ex.Message);
            }

            var domain = "http://pbaps.ps"; // قم بتغيير هذا عند النشر
            var webhookUrl = $"{domain}/TelegramBot/Update";

            try
            {
                TelegramService.SetWebhookAsync(webhookUrl).Wait();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            } 
        } 
       
    }
}