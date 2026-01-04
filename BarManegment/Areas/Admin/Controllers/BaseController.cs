using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Data.Entity; // ✅ ضروري جداً لعمل Include

namespace BarManegment.Areas.Admin.Controllers
{
    public class BaseController : Controller
    {
        // استخدام Context واحد مشترك أفضل (اختياري)
        // private readonly ApplicationDbContext _db = new ApplicationDbContext();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // ... (كود التحقق من الجلسة كما هو) ...
            var allowAnonymous = filterContext.ActionDescriptor.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any()
                                 || filterContext.ActionDescriptor.ControllerDescriptor.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();

            if (allowAnonymous)
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            if (Session["UserId"] == null)
            {
                FormsAuthentication.SignOut();
                Session.Clear();
                Session.Abandon();

                filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                    {
                        { "controller", "AdminLogin" },
                        { "action", "Login" },
                        { "area", "Admin" },
                        { "returnUrl", filterContext.HttpContext.Request.RawUrl }
                    });

                base.OnActionExecuting(filterContext);
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        // 1. دالة إنشاء قسيمة واحدة
        protected PaymentVoucher CreatePaymentVoucher(int traineeId, int feeTypeId, string description)
        {
            using (var db = new ApplicationDbContext()) // استخدام Using لضمان الإغلاق
            {
                try
                {
                    var feeType = db.FeeTypes.Find(feeTypeId);
                    if (feeType == null) return null;

                    var voucher = new PaymentVoucher
                    {
                        GraduateApplicationId = traineeId,
                        IssueDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddDays(14),
                        Status = "صادر",
                        TotalAmount = feeType.DefaultAmount,
                        IssuedByUserId = (int)Session["UserId"],
                        IssuedByUserName = Session["FullName"] as string,
                        VoucherDetails = new List<VoucherDetail>
                        {
                            new VoucherDetail
                            {
                                FeeTypeId = feeTypeId,
                                Amount = feeType.DefaultAmount,
                                BankAccountId = feeType.BankAccountId,
                                Description = description
                            }
                        }
                    };
                    return voucher;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    return null;
                }
            }
        }

        // 2. دالة إنشاء قسيمة مجمعة (Batch)
        // ✅ تم تعديلها لتكون public أو protected لكي يراها الأبناء
        // ✅ تم إضافة include
        protected PaymentVoucher CreateBatchPaymentVoucher(int traineeId, List<FeeSelectionViewModel> selectedFees, string description, DateTime ExpiryDate)
        {
            using (var db = new ApplicationDbContext())
            {
                try
                {
                    if (!selectedFees.Any()) return null;

                    var selectedFeeIds = selectedFees.Select(f => f.FeeTypeId).ToList();

                    // ✅ هنا التصحيح: استخدام Include بشكل صحيح
                    var feeTypes = db.FeeTypes
                                     .Include(f => f.Currency)
                                     .Include(f => f.BankAccount)
                                     .Where(f => selectedFeeIds.Contains(f.Id))
                                     .ToList(); // جلب البيانات للذاكرة أولاً

                    decimal totalAmount = 0;
                    var voucherDetails = new List<VoucherDetail>();

                    foreach (var selectedFee in selectedFees)
                    {
                        // البحث في القائمة المحملة في الذاكرة (Memory)
                        var dbFeeType = feeTypes.FirstOrDefault(f => f.Id == selectedFee.FeeTypeId);

                        if (dbFeeType != null)
                        {
                            voucherDetails.Add(new VoucherDetail
                            {
                                FeeTypeId = selectedFee.FeeTypeId,
                                Amount = selectedFee.Amount,
                                BankAccountId = dbFeeType.BankAccountId, // أخذ رقم الحساب من نوع الرسم
                                Description = description
                            });
                            totalAmount += selectedFee.Amount;
                        }
                    }

                    if (!voucherDetails.Any()) return null;

                    var voucher = new PaymentVoucher
                    {
                        GraduateApplicationId = traineeId,
                        IssueDate = DateTime.Now,
                        ExpiryDate = ExpiryDate,
                        Status = "صادر",
                        TotalAmount = totalAmount,
                        IssuedByUserId = (int)Session["UserId"],
                        IssuedByUserName = Session["FullName"] as string,
                        PaymentMethod = "نقدي", // قيمة افتراضية
                        VoucherDetails = voucherDetails
                    };

                    return voucher;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating batch payment voucher: {ex.Message}");
                    return null;
                }
            }
        }
    }
}