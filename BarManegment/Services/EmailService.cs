// File Path: BarManegment/Services/EmailService.cs
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Configuration;

namespace BarManegment.Services
{
    public static class EmailService
    {
        public static async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var fromEmail = ConfigurationManager.AppSettings["SmtpFromEmail"];
            var fromPassword = ConfigurationManager.AppSettings["SmtpPassword"];
            var smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
            var smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);

            var message = new MailMessage(fromEmail, toEmail, subject, body)
            {
                IsBodyHtml = true
            };

            using (var client = new SmtpClient(smtpHost, smtpPort))
            {
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(fromEmail, fromPassword);
                await client.SendMailAsync(message);
            }
        }
    }
}