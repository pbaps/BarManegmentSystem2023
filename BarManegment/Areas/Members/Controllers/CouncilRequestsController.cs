using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Members.Controllers
{
    // افترض أن المتحكم الأساسي للأعضاء يوفر صلاحيات الدخول
    // [AuthorizeMember] أو ما شابه
    public class CouncilRequestsController : Controller // أو : MemberBaseController إذا كان موجوداً
    {
        private ApplicationDbContext db = new ApplicationDbContext();


        private int GetCurrentMemberId()
        {
            // 1. (تصحيح) ابحث عن "UserId" الذي يتم تعيينه عند تسجيل الدخول
            var userId = Session["UserId"];
            if (userId == null)
            {
                // هذا يعني أن المستخدم لم يسجل دخوله أصلاً
                throw new Exception("User is not authenticated.");
            }

            int currentUserId = (int)userId;

            // 2. (تصحيح) استخدم "UserId" للعثور على "GraduateApplicationId"
            // نحن نفترض أن كل مستخدم (UserModel) لديه ملف واحد فقط (GraduateApplication)
            var graduateProfileId = db.GraduateApplications
                                      .Where(g => g.UserId == currentUserId)
                                      .Select(g => g.Id)
                                      .FirstOrDefault();

            if (graduateProfileId == 0)
            {
                // هذا يعني أن المستخدم سجل دخوله، لكن ليس لديه ملف محامي/متدرب
                throw new Exception("User is authenticated but not linked to a GraduateApplication profile.");
            }

            // 3. (تصحيح) إرجاع الـ ID الصحيح
            return graduateProfileId;
        }
 


        // 1. GET: /Members/CouncilRequests/Index
        // (صفحة "متابعة طلباتي")
        public ActionResult Index()
        {
            int memberId = GetCurrentMemberId();

            var myRequests = db.AgendaItems
                .Where(a => a.RequesterLawyerId == memberId)
                .OrderByDescending(a => a.Id)
                .ToList();

            return View(myRequests);
        }

        // 2. GET: /Members/CouncilRequests/Create
        // (صفحة "تقديم طلب جديد")
        public ActionResult Create()
        {
            return View();
        }

        // 3. POST: /Members/CouncilRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string RequestType, string Title, string Description, IEnumerable<HttpPostedFileBase> attachments)
        {
            if (string.IsNullOrEmpty(Title) || string.IsNullOrEmpty(Description))
            {
                ModelState.AddModelError("", "يجب ملء حقل العنوان والتفاصيل.");
                return View();
            }

            int memberId = GetCurrentMemberId();

            var item = new AgendaItem
            {
                // بيانات الطلب
                RequestType = RequestType,
                Title = Title,
                Description = Description,

                // بيانات الربط
                RequesterLawyerId = memberId,

                // بيانات الحالة (للمنسق)
                CouncilSessionId = null, // <-- أهم حقل (ليظهر في صندوق الوارد)
                Source = "LawyerPortal", // <-- لتحديد المصدر
                IsApprovedForAgenda = false,
                CouncilDecisionType = "Pending",
                ExecutionStatus = "قيد مراجعة المنسق", // حالة أولية يراها المحامي

                Attachments = new List<AgendaAttachment>()
            };

            // معالجة المرفقات (نفس الكود الذي استخدمناه في البند اليدوي)
            if (attachments != null)
            {
                string uploadPath = Server.MapPath("~/Uploads/AgendaAttachments/");
                Directory.CreateDirectory(uploadPath);

                foreach (var file in attachments)
                {
                    if (file != null && file.ContentLength > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var physicalPath = Path.Combine(uploadPath, fileName);
                        file.SaveAs(physicalPath);

                        item.Attachments.Add(new AgendaAttachment
                        {
                            FileName = file.FileName,
                            FilePath = "/Uploads/AgendaAttachments/" + fileName,
                            UploadedBy = $"MemberID: {memberId}"
                        });
                    }
                }
            }

            db.AgendaItems.Add(item);
            db.SaveChanges();

            TempData["SuccessMessage"] = "تم إرسال طلبك بنجاح للمتابعة.";
            return RedirectToAction("Index");
        }
    }
}