using BarManegment.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Collections.Generic;

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // داخل ContractsController.cs

        // داخل BarManegment.Areas.Members.Controllers.ContractsController

        public ActionResult Index(string searchTerm, DateTime? fromDate, DateTime? toDate)
        {
            var userId = (int)Session["UserId"];
            var lawyer = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);
            if (lawyer == null) return RedirectToAction("Login", "Account");

            var query = db.FeeDistributions
                .Include(d => d.ContractTransaction)
                .Include(d => d.ContractTransaction.ContractType)
                .Include(d => d.ContractTransaction.Parties) // تأكد أن Parties موجودة في الموديل
                .Where(d => d.LawyerId == lawyer.Id);

            // البحث
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(d =>
                    d.ContractTransaction.Id.ToString().Contains(searchTerm)
                // ملاحظة: إذا كان جدول Parties لا يحتوي على Name، استبدلها بـ ArabicName أو PartyName
                // || d.ContractTransaction.Parties.Any(p => p.Name.Contains(searchTerm)) 
                );
            }

            // فلترة التاريخ
            if (fromDate.HasValue)
                query = query.Where(d => d.ContractTransaction.TransactionDate >= fromDate.Value);

            if (toDate.HasValue)
            {
                var finalToDate = toDate.Value.AddDays(1);
                query = query.Where(d => d.ContractTransaction.TransactionDate < finalToDate);
            }

            // جلب البيانات (To List) ثم التحويل لتفادي مشاكل Linq-to-Entities
            var transactionsData = query
                .OrderByDescending(d => d.ContractTransaction.TransactionDate)
                .ToList();

            // التحويل إلى ViewModel
            var viewModel = transactionsData.Select(d => new MemberContractViewModel
            {
                TransactionId = d.ContractTransactionId,
                Date = d.ContractTransaction.TransactionDate,
                ContractType = d.ContractTransaction.ContractType != null ? d.ContractTransaction.ContractType.Name : "غير محدد",

                // ✅ التصحيح هنا: استخدام FinalFee بدلاً من TotalAmount
                TotalAmount = d.ContractTransaction.FinalFee,

                LawyerShare = d.Amount,

                // ✅ معالجة الأطراف: تأكد من اسم الحقل في جدول TransactionParty
                // سنفترض هنا أن الحقل اسمه "Name" أو "FullName" أو "ArabicName".
                // بما أني لا أرى موديل TransactionParty، سأستخدمToString() كحل مؤقت آمن.
                // يفضل تغيير x.ToString() إلى x.Name أو x.ArabicName إذا عرفت الاسم الصحيح.
                PartiesNames = d.ContractTransaction.Parties != null
                               ? string.Join(" - ", d.ContractTransaction.Parties.Select(p => p.Id.ToString())) // مؤقتاً نعرض الـ ID
                               : "لا يوجد أطراف"
            }).ToList();

            return View(viewModel);
        }
    }
}