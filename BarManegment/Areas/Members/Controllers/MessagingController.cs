using BarManegment.Areas.Members.ViewModels;
using BarManegment.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Net;
using System.Net.Mime;

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class MessagingController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // (دالة مساعدة لجلب ID المستخدم الحالي)
        private int GetCurrentUserId()
        {
            // ✅ تعديل: بدلاً من الانهيار، سنعيد قيمة غير صالحة
            if (Session["UserId"] == null)
            {
                return -1;
            }
            return (int)Session["UserId"];
        }

        // (دالة مساعدة لتحديد مستقبل الرسالة)
        // (الكود المحدث)
        private UserModel GetRecipient(string identifier)
        {
            // ✅ تعديل: تم تحديث الاستعلام ليعمل مع علاقة 1-to-Many الجديدة
            return db.Users.Include(u => u.GraduateApplications)
                           .FirstOrDefault(u => u.IdentificationNumber == identifier ||
                                                // (البحث في أي طلب تخرج مرتبط بهذا المستخدم)
                                                (u.GraduateApplications.Any(g => g.MembershipId == identifier)) ||
                                                u.Email == identifier ||
                                                u.Username == identifier); // 💡💡 (الإضافة الأهم)
        }

        // GET: Members/Messaging/Inbox
        public ActionResult Inbox()
        {
            var userId = GetCurrentUserId();

            // ✅ تعديل: التحقق من الجلسة وإعادة التوجيه
            if (userId == -1)
            {
                // (التوجيه إلى صفحة انتهاء الجلسة التي أنشأتها في AccountController)
                return RedirectToAction("SessionExpired", "Account", new { area = "Members" });
            }

            var inboxMessages = db.InternalMessages
                .Include(m => m.Sender)
                .Include(m => m.Replies)
                .Where(m => m.RecipientId == userId && m.ParentMessageId == null)
                .OrderByDescending(m => m.Timestamp)
                .ToList();

            var viewModelList = inboxMessages.Select(m => new MessageListItemViewModel
            {
                Id = m.Id,
                Subject = m.Subject,
                SenderName = m.Sender.FullNameArabic,
                Timestamp = m.Timestamp,
                IsRead = m.IsRead,
                HasAttachment = m.HasAttachment,
                ReplyCount = m.Replies.Count
            }).ToList();

            return View("Index", viewModelList);
        }

        // GET: Members/Messaging/Outbox
        public ActionResult Outbox()
        {
            var userId = GetCurrentUserId();

            // ✅ تعديل: التحقق من الجلسة وإعادة التوجيه
            if (userId == -1)
            {
                return RedirectToAction("SessionExpired", "Account", new { area = "Members" });
            }

            var sentMessages = db.InternalMessages
                .Include(m => m.Recipient)
                .Include(m => m.Replies)
                .Where(m => m.SenderId == userId && m.ParentMessageId == null)
                .OrderByDescending(m => m.Timestamp)
                .ToList();

            var viewModelList = sentMessages.Select(m => new MessageListItemViewModel
            {
                Id = m.Id,
                Subject = m.Subject,
                RecipientName = m.Recipient.FullNameArabic,
                Timestamp = m.Timestamp,
                IsRead = m.IsRead,
                HasAttachment = m.HasAttachment,
                ReplyCount = m.Replies.Count
            }).ToList();

            return View("Index", viewModelList);
        }

        // GET: Members/Messaging/Compose
        public ActionResult Compose(int? parentId)
        {
            var userId = GetCurrentUserId();

            // ✅ تعديل: التحقق من الجلسة وإعادة التوجيه
            if (userId == -1)
            {
                return RedirectToAction("SessionExpired", "Account", new { area = "Members" });
            }

            var viewModel = new ComposeMessageViewModel
            {
                SenderId = userId,
                ParentMessageId = parentId
            };

            if (parentId.HasValue)
            {
                var parent = db.InternalMessages.Include(m => m.Sender).Include(m => m.Recipient).FirstOrDefault(m => m.Id == parentId.Value);
                if (parent != null)
                {
                    var otherUser = parent.SenderId == viewModel.SenderId ? parent.Recipient : parent.Sender;

                    viewModel.Subject = "رد: " + parent.Subject.Replace("رد: ", "");
                    viewModel.RecipientIdentifier = otherUser.Username;
                    viewModel.RecipientNameDisplay = otherUser.FullNameArabic;
                }
            }
            return View(viewModel);
        }

        // POST: Members/Messaging/Compose
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Compose(ComposeMessageViewModel viewModel)
        {
            var userId = GetCurrentUserId();

            // ✅ تعديل: التحقق من الجلسة وإعادة التوجيه
            if (userId == -1)
            {
                return RedirectToAction("SessionExpired", "Account", new { area = "Members" });
            }

            var recipientUser = GetRecipient(viewModel.RecipientIdentifier);

            if (recipientUser == null)
            {
                ModelState.AddModelError("RecipientIdentifier", "لم يتم العثور على مستلم بهذا المعرف.");
            }

            if (!ModelState.IsValid)
            {
                viewModel.RecipientNameDisplay = recipientUser?.FullNameArabic;
                return View(viewModel);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var newMessage = new InternalMessage
                    {
                        Subject = viewModel.Subject,
                        Body = viewModel.Body,
                        SenderId = userId, // استخدام المتغير الآمن
                        RecipientId = recipientUser.Id,
                        Timestamp = DateTime.Now,
                        ParentMessageId = viewModel.ParentMessageId,
                        HasAttachment = viewModel.Files != null && viewModel.Files.Any(f => f != null && f.ContentLength > 0),
                        IsRead = false
                    };
                    db.InternalMessages.Add(newMessage);
                    db.SaveChanges();

                    if (newMessage.HasAttachment)
                    {
                        SaveAttachments(newMessage.Id, viewModel.Files);
                        db.SaveChanges();
                    }

                    transaction.Commit();
                    TempData["SuccessMessage"] = "تم إرسال الرسالة بنجاح.";
                    return RedirectToAction("Outbox");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "حدث خطأ أثناء إرسال الرسالة: " + ex.Message;
                    return View(viewModel);
                }
            }
        }

        // GET: Members/Messaging/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var userId = GetCurrentUserId();

            // ✅ تعديل: التحقق من الجلسة وإعادة التوجيه
            if (userId == -1)
            {
                return RedirectToAction("SessionExpired", "Account", new { area = "Members" });
            }

            var threadMessages = db.InternalMessages
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .Include(m => m.Attachments)
                .Where(m => m.Id == id || m.ParentMessageId == id)
                .OrderBy(m => m.Timestamp)
                .ToList();

            var rootMessage = threadMessages.FirstOrDefault(m => m.Id == id);
            if (rootMessage == null) return HttpNotFound();

            if (rootMessage.SenderId != userId && rootMessage.RecipientId != userId)
            {
                return new HttpUnauthorizedResult();
            }

            if (rootMessage.RecipientId == userId && !rootMessage.IsRead)
            {
                rootMessage.IsRead = true;
                db.Entry(rootMessage).State = EntityState.Modified;
                db.SaveChanges();
            }

            var viewModel = new MessageThreadViewModel
            {
                ThreadId = rootMessage.Id,
                Subject = rootMessage.Subject,
                Messages = threadMessages,
                ReplyModel = new ComposeMessageViewModel
                {
                    ParentMessageId = rootMessage.Id,
                    SenderId = userId,
                    Subject = "رد: " + rootMessage.Subject.Replace("رد: ", ""),
                    RecipientIdentifier = rootMessage.SenderId == userId
                                          ? rootMessage.Recipient.Username
                                          : rootMessage.Sender.Username,
                    RecipientNameDisplay = rootMessage.SenderId == userId
                                           ? rootMessage.Recipient.FullNameArabic
                                           : rootMessage.Sender.FullNameArabic
                }
            };

            return View(viewModel);
        }

        // (دالة مساعدة لحفظ المرفقات)
        private void SaveAttachments(int messageId, IEnumerable<HttpPostedFileBase> files)
        {
            var uploadPath = Server.MapPath($"~/Uploads/Messages/{messageId}");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            foreach (var file in files.Where(f => f != null && f.ContentLength > 0))
            {
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);
                file.SaveAs(filePath);

                var attachment = new MessageAttachment
                {
                    InternalMessageId = messageId,
                    OriginalFileName = file.FileName,
                    FilePath = $"/Uploads/Messages/{messageId}/{fileName}"
                };
                db.MessageAttachments.Add(attachment);
            }
        }
        // 💡 2. إضافة هذه الدالة الجديدة بالكامل
        // GET: Members/Messaging/ViewAttachment/5
        public ActionResult ViewAttachment(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("SessionExpired", "Account", new { area = "Members" });
            }

            // جلب المرفق مع الرسالة للتأكد من الصلاحية
            var attachment = db.MessageAttachments
                               .Include(a => a.Message)
                               .FirstOrDefault(a => a.Id == id);

            if (attachment == null)
            {
                return HttpNotFound();
            }

            // التحقق الأمني: هل المستخدم الحالي طرف في هذه المحادثة؟
            if (attachment.Message.SenderId != userId && attachment.Message.RecipientId != userId)
            {
                return new HttpUnauthorizedResult("لا تملك الصلاحية لعرض هذا المرفق.");
            }

            try
            {
                // تحويل المسار النسبي (~) إلى مسار فيزيائي
                var physicalPath = Server.MapPath(attachment.FilePath);

                if (!System.IO.File.Exists(physicalPath))
                {
                    return HttpNotFound("الملف غير موجود على الخادم.");
                }

                // تحديد نوع الملف (MimeType)
                var mimeType = MimeMapping.GetMimeMapping(physicalPath);

                // 💡 الأهم: إجبار المتصفح على "العرض" (inline) بدلاً من "التحميل" (attachment)
                var cd = new ContentDisposition
                {
                    FileName = attachment.OriginalFileName,
                    Inline = true, // هذا هو الأمر بعرض الملف
                };
                Response.AppendHeader("Content-Disposition", cd.ToString());

                // إرجاع الملف كـ FileStream
                return File(physicalPath, mimeType);
            }
            catch (Exception)
            {
                // معالجة أي خطأ في قراءة الملف
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "خطأ في قراءة الملف.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}