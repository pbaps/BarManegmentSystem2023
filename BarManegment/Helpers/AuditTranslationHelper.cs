using System.Text;

namespace BarManegment.Helpers
{
    public static class AuditTranslationHelper
    {
        public static string TranslateDetails(string englishText)
        {
            if (string.IsNullOrEmpty(englishText)) return "";

            StringBuilder sb = new StringBuilder(englishText);

            // استبدال المصطلحات الشائعة
            sb.Replace("Create Purchase Invoice", "إنشاء فاتورة شراء");
            sb.Replace("Invoice #", "فاتورة رقم ");

            sb.Replace("Supplier:", "المورد:");
            sb.Replace("Amount:", "القيمة:");

            sb.Replace("Create Stock Issue", "إنشاء إذن صرف");
            sb.Replace("Issue #", "إذن رقم ");
            sb.Replace("To:", "إلى:");
            sb.Replace("Items Count:", "عدد الأصناف:");

            sb.Replace("Delete Stock Issue", "حذف إذن صرف");
            sb.Replace("Deleted Issue #", "تم حذف الإذن رقم ");
            sb.Replace("and reversed inventory/GL", "وتم عكس المخزون والقيد");

            sb.Replace("User", "المستخدم");
            sb.Replace("logged in", "سجل الدخول");
            sb.Replace("logged out", "سجل الخروج");
            sb.Replace("successfully", "بنجاح");
            sb.Replace("Failed", "فشل");

            return sb.ToString();
        }

        public static string TranslateAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return "";

            switch (action)
            {
                case "Create": return "إضافة";
                case "Edit": return "تعديل";
                case "Update": return "تحديث";
                case "Delete": return "حذف";
                case "Login": return "تسجيل دخول";
                case "Logout": return "تسجيل خروج";
                case "Create Purchase Invoice": return "توريد مخزني";
                case "Create Stock Issue": return "صرف مخزني";
                default: return action;
            }
        }
    }
}