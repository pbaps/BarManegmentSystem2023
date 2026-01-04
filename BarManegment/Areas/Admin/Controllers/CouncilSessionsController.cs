using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BarManegment.Helpers;
using System.Web;
using BarManegment.Services;
using System.IO;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class CouncilSessionsController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // عرض قائمة الجلسات
        public ActionResult Index()
        {
            var sessions = db.CouncilSessions.OrderByDescending(s => s.SessionDate).ToList();
            return View(sessions);
        }

        // إنشاء جلسة جديدة
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            var currentYear = DateTime.Now.Year;
            var lastSession = db.CouncilSessions
                .Where(s => s.Year == currentYear)
                .OrderByDescending(s => s.SessionNumber)
                .FirstOrDefault();

            var nextNum = (lastSession != null) ? lastSession.SessionNumber + 1 : 1;

            var model = new CouncilSession
            {
                Year = currentYear,
                SessionDate = DateTime.Now,
                SessionNumber = nextNum,
                Location = "مقر النقابة الرئيسي"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(CouncilSession councilSession)
        {
            if (ModelState.IsValid)
            {
                db.CouncilSessions.Add(councilSession);
                db.SaveChanges();

                // 📝 تسجيل الحدث
                AuditService.LogAction("Create Session", "CouncilSessions", $"Created Session #{councilSession.SessionNumber}/{councilSession.Year} (ID: {councilSession.Id})");

                return RedirectToAction("Details", new { id = councilSession.Id });
            }
            return View(councilSession);
        }

        // تفاصيل الجلسة وإدارتها
        // تفاصيل الجلسة وإدارتها
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            var session = db.CouncilSessions
                .Include(s => s.AgendaItems.Select(ai => ai.Attachments))
                .Include(s => s.Attendees)
                .FirstOrDefault(s => s.Id == id);

            if (session == null) return HttpNotFound();

            var existingMemberNames = session.Attendees.Select(a => a.MemberName).ToList();
            var councilMembers = db.CouncilMembers
                .Where(m => m.IsActive && !existingMemberNames.Contains(m.Name))
                .ToList();

            ViewBag.CouncilMembersList = new SelectList(councilMembers, "Name", "Name");

            return View(session);
        }

        // إضافة بند يدوي
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult AddManualItem(int sessionId, string title, string description, string requestType, int? RequesterLawyerId, IEnumerable<HttpPostedFileBase> attachments)
        {
            var item = new AgendaItem
            {
                CouncilSessionId = sessionId,
                Title = title,
                Description = description,
                RequestType = requestType,
                Source = "Manual",
                CreatedByUserId = Session["UserId"]?.ToString(),
                IsApprovedForAgenda = true,
                CouncilDecisionType = "Pending",
                RequesterLawyerId = RequesterLawyerId,
                Attachments = new List<AgendaAttachment>()
            };

            if (attachments != null)
            {
                string uploadPath = Server.MapPath("~/Uploads/AgendaAttachments/");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

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
                            UploadedBy = Session["FullName"]?.ToString()
                        });
                    }
                }
            }

            db.AgendaItems.Add(item);
            db.SaveChanges();

            // 📝 تسجيل الحدث
            AuditService.LogAction("Add Agenda Item", "CouncilSessions", $"Added item '{title}' to Session ID {sessionId}");

            return RedirectToAction("Details", new { id = sessionId });
        }

        // تسجيل قرار المجلس وتحديث حالة الطلب الأصلي
        // =========================================================================
        // ✅ الدالة المصححة بالكامل لتسجيل القرار وتحديث الحالة والعودة للصفحة
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult SaveDecision(int itemId, string decisionType, string decisionText, bool isVisibleToRequester)
        {
            var item = db.AgendaItems.Find(itemId);
            if (item == null) return HttpNotFound();

            // 1. تحديث بيانات البند في جدول الأعمال
            item.CouncilDecisionType = decisionType;
            item.DecisionText = decisionText;
            item.IsVisibleToRequester = isVisibleToRequester;

            // تحديث حالة التنفيذ بناءً على القرار
            if (decisionType == "Approved" || decisionType == "Modified")
            {
                item.ExecutionStatus = "بانتظار التعيين";
            }
            else if (decisionType == "Rejected")
            {
                item.ExecutionStatus = "مرفوض - لا يتطلب إجراء";
            }
            else if (decisionType == "Postponed")
            {
                item.ExecutionStatus = "مؤجل";
            }
            else
            {
                item.ExecutionStatus = "قيد الدراسة";
            }

            // 2. تحديث المصدر الأصلي للطلب (لجان)
            if (item.Source == "Committee")
            {
                // محاولة البحث عن القضية في جدول اللجان
                // نبحث عن الملف الذي تم رفعه للمجلس والذي يحتوي عنوان البند على رقم ملفه
                var caseFile = db.CommitteeCases.FirstOrDefault(c => item.Title.Contains(c.CaseNumber));

                if (caseFile != null)
                {
                    // تحديث الملاحظات (تمت إضافتها للموديل)
                    caseFile.CouncilDecisionNotes = decisionText;

                    if (decisionType == "Approved") caseFile.Status = "تم اعتماد التوصية";
                    else if (decisionType == "Rejected") caseFile.Status = "تم رفض التوصية";
                    else if (decisionType == "Modified") caseFile.Status = "تم الاعتماد بتعديل";
                    else if (decisionType == "Study") caseFile.Status = "مسترجع للدراسة";

                    db.Entry(caseFile).State = EntityState.Modified;
                }
            }

            db.SaveChanges();
            AuditService.LogAction("Make Decision", "CouncilSessions", $"Decision on Item {itemId}: {decisionType}");

            // العودة لنفس الجلسة
            return RedirectToAction("Details", new { id = item.CouncilSessionId });
        }
        // عرض ملف القرار الموقع
        public ActionResult ViewSignedDecision(int id)
        {
            var item = db.AgendaItems.Find(id);
            if (item == null || string.IsNullOrEmpty(item.DecisionFilePath))
            {
                return HttpNotFound("ملف القرار الموقع غير موجود.");
            }

            string physicalPath = Server.MapPath(item.DecisionFilePath);
            if (!System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound("الملف المحدد غير موجود على الخادم.");
            }

            string mimeType = MimeMapping.GetMimeMapping(physicalPath);

            // 📝 تسجيل الحدث (اختياري للخصوصية)
            // AuditService.LogAction("View Decision File", "CouncilSessions", $"Viewed signed decision for Item {id}");

            return File(physicalPath, mimeType);
        }

        // رفع ملف القرار الموقع
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UploadSignedDecisionFile(int itemId, HttpPostedFileBase signedFile)
        {
            var item = db.AgendaItems.Find(itemId);
            if (item == null) return HttpNotFound();

            if (signedFile != null && signedFile.ContentLength > 0)
            {
                string uploadPath = Server.MapPath("~/Uploads/SignedDecisions/");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var fileName = $"Decision_{item.Id}_{Guid.NewGuid()}{Path.GetExtension(signedFile.FileName)}";
                var physicalPath = Path.Combine(uploadPath, fileName);
                signedFile.SaveAs(physicalPath);

                item.DecisionFilePath = "/Uploads/SignedDecisions/" + fileName;
                db.SaveChanges();

                // 📝 تسجيل الحدث
                AuditService.LogAction("Upload Signed Decision", "CouncilSessions", $"Uploaded file for Item {itemId}");
            }

            return RedirectToAction("Details", new { id = item.CouncilSessionId });
        }

        // تسجيل الحضور
        [HttpPost]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AddAttendee(int sessionId, string memberName, bool isPresent, string notes)
        {
            if (!string.IsNullOrEmpty(memberName))
            {
                var attendance = new SessionAttendance
                {
                    CouncilSessionId = sessionId,
                    MemberName = memberName,
                    IsPresent = isPresent,
                    Notes = notes
                };
                db.SessionAttendances.Add(attendance);
                db.SaveChanges();

                // 📝 تسجيل الحدث
                AuditService.LogAction("Add Attendee", "CouncilSessions", $"Added {memberName} (Present: {isPresent}) to Session {sessionId}");
            }
            return RedirectToAction("Details", new { id = sessionId });
        }

        // طباعة المحضر
        public ActionResult PrintAgenda(int id)
        {
            var session = db.CouncilSessions
                .Include(s => s.AgendaItems)
                .Include(s => s.Attendees)
                .FirstOrDefault(s => s.Id == id);

            if (session == null) return HttpNotFound();

            // 📝 تسجيل الحدث
            AuditService.LogAction("Print Agenda", "CouncilSessions", $"Printed minutes for Session {id}");

            return View(session);
        }

        // إغلاق الجلسة
        // إغلاق الجلسة وترحيل البنود المعلقة (مؤجل + دراسة)
        // إغلاق الجلسة وإنشاء نسخ جديدة من البنود المؤجلة
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult CloseSession(int id)
        {
            var session = db.CouncilSessions.Find(id);
            if (session == null) return HttpNotFound();

            if (!session.IsFinalized)
            {
                // 1. إغلاق الجلسة الحالية
                session.IsFinalized = true;

                // 2. جلب البنود التي تحتاج ترحيل (مؤجل + دراسة) مع المرفقات
                var itemsToCarryOver = db.AgendaItems
                    .Include(i => i.Attachments) // ضروري لنسخ المرفقات
                    .Where(i => i.CouncilSessionId == id &&
                               (i.CouncilDecisionType == "Postponed" || i.CouncilDecisionType == "Study"))
                    .ToList();

                int clonedCount = 0;

                foreach (var oldItem in itemsToCarryOver)
                {
                    // 💡 الإجراء الصحيح: إنشاء بند جديد تماماً (Clone)
                    var newItem = new AgendaItem
                    {
                        CouncilSessionId = null, // يذهب إلى صندوق الوارد (Coordinator Inbox)

                        // نسخ البيانات الأساسية
                        Title = oldItem.Title,
                        Description = oldItem.Description, // يمكن إضافة نص: (مرحل من جلسة رقم...)
                        RequestType = oldItem.RequestType,
                        Source = oldItem.Source,
                        RequesterLawyerId = oldItem.RequesterLawyerId,
                        CreatedByUserId = Session["UserId"]?.ToString(), // الموظف الذي أغلق الجلسة هو من رحّل البند

                        // إعدادات الحالة الجديدة
                        IsApprovedForAgenda = true,
                        CouncilDecisionType = "Pending", // تصفير القرار

                        // تحديد حالة التنفيذ لتوضيح المصدر
                        ExecutionStatus = (oldItem.CouncilDecisionType == "Study")
                                          ? $"معاد للدراسة (من جلسة {session.SessionNumber})"
                                          : $"مؤجل (من جلسة {session.SessionNumber})",

                        Attachments = new List<AgendaAttachment>()
                    };

                    // 💡 نسخ المرفقات (دون تكرار الملفات الفعلية على السيرفر، فقط روابط قاعدة البيانات)
                    if (oldItem.Attachments != null)
                    {
                        foreach (var oldAtt in oldItem.Attachments)
                        {
                            newItem.Attachments.Add(new AgendaAttachment
                            {
                                FileName = oldAtt.FileName,
                                FilePath = oldAtt.FilePath, // استخدام نفس الملف الموجود على السيرفر
                                UploadedBy = "System (Cloned)"
                            });
                        }
                    }

                    db.AgendaItems.Add(newItem);
                    clonedCount++;

                    // ملاحظة: البند القديم (oldItem) يبقى كما هو في الجلسة المغلقة بحالته (Postponed)
                }

                db.SaveChanges();

                // 📝 تسجيل الحدث
                AuditService.LogAction("Close Session", "CouncilSessions", $"Finalized Session {id}. Cloned {clonedCount} items to Inbox.");

                TempData["Success"] = $"تم إغلاق الجلسة بنجاح. تم إنشاء {clonedCount} بند جديد في صندوق الوارد كنسخ عن البنود المؤجلة.";
            }

            return RedirectToAction("Details", new { id = id });
        }
        // طباعة قرار منفرد
        public ActionResult PrintDecision(int id)
        {
            var agendaItem = db.AgendaItems
                .Include(a => a.CouncilSession)
                .Include(a => a.Attachments)
                .Include(a => a.CouncilSession.Attendees)
                .FirstOrDefault(a => a.Id == id);

            if (agendaItem == null) return HttpNotFound();

            return View(agendaItem);
        }

        // ترحيل بنود من الوارد (Coordinator)
        // ترحيل بنود من الوارد (Coordinator)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignItemsToSession(CoordinatorDashboardViewModel model)
        {
            // التحقق من صحة البيانات
            if (model.SelectedSessionId == 0 || model.SelectedItemIds == null || !model.SelectedItemIds.Any())
            {
                TempData["Error"] = "يجب اختيار جلسة وتحديد بند واحد على الأقل للنقل.";
                return RedirectToAction("Index", "CoordinatorInbox");
            }

            // جلب البنود المحددة
            var itemsToUpdate = db.AgendaItems
                .Where(i => model.SelectedItemIds.Contains(i.Id))
                .ToList();

            foreach (var item in itemsToUpdate)
            {
                // 1. ربط بالجلسة الجديدة
                item.CouncilSessionId = model.SelectedSessionId;

                // 2. ⚠️ هام جداً: تصفير حالة القرار ليعتبره النظام بنداً جديداً يحتاج للبت
                item.CouncilDecisionType = "Pending";

                // 3. تحديث حالة التنفيذ
                // إذا كان قادماً من "تأجيل" أو "دراسة"، نغير حالته ليعرف الأعضاء أنه مدرج للنقاش
                if (item.ExecutionStatus != null && (item.ExecutionStatus.Contains("مؤجل") || item.ExecutionStatus.Contains("دراسة")))
                {
                    item.ExecutionStatus = "معاد للعرض - قيد المناقشة";
                }
                else
                {
                    item.ExecutionStatus = "مدرج في الجدول";
                }

                // 4. ضمان تحديث الكيان
                db.Entry(item).State = EntityState.Modified;
            }

            db.SaveChanges();

            // 📝 تسجيل الحدث
            AuditService.LogAction("Assign Items", "CouncilSessions", $"Assigned {itemsToUpdate.Count} items to Session {model.SelectedSessionId}");

            TempData["Success"] = $"تم ترحيل {itemsToUpdate.Count} بند بنجاح إلى الجلسة المختارة.";

            // التوجيه لصفحة تفاصيل الجلسة لرؤية النتيجة فوراً
            return RedirectToAction("Details", new { id = model.SelectedSessionId });
        }
        // الجرس (Notification)
        [ChildActionOnly]
        [AllowAnonymous] // 👈👈👈 هذه هي الإضافة المهمة جداً لحل المشكلة
        public ActionResult PendingRequestsNotification()
        {
            try
            {
                int pendingCount = db.AgendaItems.Count(i => i.CouncilSessionId == null);
                return PartialView("_PendingRequestsNotification", pendingCount);
            }
            catch
            {
                return Content("");
            }
        }

        // عرض مرفق
        public ActionResult ViewAgendaAttachment(int id)
        {
            var doc = db.AgendaAttachments.Find(id);
            if (doc == null || string.IsNullOrEmpty(doc.FilePath)) return HttpNotFound("المستند غير موجود.");

            string physicalPath = Server.MapPath(doc.FilePath);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound("الملف غير موجود.");

            string mimeType = MimeMapping.GetMimeMapping(physicalPath);
            return File(physicalPath, mimeType);
        }

        // بحث محامين (JSON)
        [HttpGet]
        public JsonResult SearchLawyers(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new { results = new List<object>() }, JsonRequestBehavior.AllowGet);

            int.TryParse(term, out int lawyerId);

            var lawyers = db.GraduateApplications
                .Where(g => g.ArabicName.Contains(term) || (lawyerId > 0 && g.Id == lawyerId))
                .Select(g => new {
                    id = g.Id,
                    text = g.ArabicName + " (ID: " + g.Id + ")"
                })
                .Take(20)
                .ToList();

            return Json(new { results = lawyers }, JsonRequestBehavior.AllowGet);
        }

        // تعديل بند يدوي
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult EditAgendaItem(int Id, string Title, string Description, string RequestType, int? RequesterLawyerId)
        {
            var item = db.AgendaItems.Find(Id);
            if (item == null) return HttpNotFound();

            if (item.CouncilDecisionType == "Pending")
            {
                string oldTitle = item.Title;
                item.Title = Title;
                item.Description = Description;
                item.RequestType = RequestType;
                item.RequesterLawyerId = RequesterLawyerId;
                db.SaveChanges();

                // 📝 تسجيل الحدث
                AuditService.LogAction("Edit Agenda Item", "CouncilSessions", $"Edited Item {Id} (Old Title: {oldTitle})");
            }

            return RedirectToAction("Details", new { id = item.CouncilSessionId });
        }

        // حذف بند يدوي
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteAgendaItem(int itemId)
        {
            var item = db.AgendaItems.Include(i => i.CouncilSession).FirstOrDefault(i => i.Id == itemId);
            if (item == null) return HttpNotFound();

            int? returnSessionId = item.CouncilSessionId;

            // السماح بالحذف فقط إذا كان معلقاً ولم تغلق الجلسة
            if (item.CouncilDecisionType == "Pending" && (item.CouncilSession == null || !item.CouncilSession.IsFinalized))
            {
                db.AgendaItems.Remove(item);
                db.SaveChanges();

                // 📝 تسجيل الحدث
                AuditService.LogAction("Delete Agenda Item", "CouncilSessions", $"Deleted Item {itemId} from Session {returnSessionId}");
            }

            if (returnSessionId != null)
                return RedirectToAction("Details", new { id = returnSessionId });
            else
                return RedirectToAction("Index", "CoordinatorInbox"); // عودة للوحة المنسق إذا لم يكن مرتبطاً بجلسة
        }

        // رفع المحضر الكامل
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UploadSignedMinutes(int sessionId, HttpPostedFileBase file)
        {
            var session = db.CouncilSessions.Find(sessionId);
            if (session == null) return HttpNotFound();

            if (file != null && file.ContentLength > 0)
            {
                string uploadPath = Server.MapPath("~/Uploads/SignedMinutes/");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var fileName = $"Session_{session.Id}_{session.Year}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var physicalPath = Path.Combine(uploadPath, fileName);
                file.SaveAs(physicalPath);

                session.SignedMinutesPath = "/Uploads/SignedMinutes/" + fileName;
                db.SaveChanges();

                // 📝 تسجيل الحدث
                AuditService.LogAction("Upload Signed Minutes", "CouncilSessions", $"Uploaded minutes for Session {sessionId}");

                TempData["Success"] = "تم رفع المحضر الموقع بنجاح.";
            }

            return RedirectToAction("Details", new { id = sessionId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}