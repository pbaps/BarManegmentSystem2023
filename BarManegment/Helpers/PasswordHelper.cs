using System;
using System.Security.Cryptography;
using System.Text;

namespace BarManegment.Helpers
{
    public static class PasswordHelper
    {
        // نستخدم مفتاحاً ثابتاً لضمان أن التشفير يعطي نفس النتيجة دائماً لنفس الكلمة
        // (يمكنك تغيير هذا النص لأي شيء تريده لزيادة الأمان)
        private static readonly string FixedSalt = "PBA_Secret_Salt_2026@Gaza";

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return null;

            using (var sha256 = SHA256.Create())
            {
                // دمج كلمة المرور مع الملح الثابت
                var combinedPassword = password + FixedSalt;
                var bytes = Encoding.UTF8.GetBytes(combinedPassword);
                var hash = sha256.ComputeHash(bytes);

                // تحويل البايتات إلى نص Base64 للحفظ في قاعدة البيانات
                return Convert.ToBase64String(hash);
            }
        }

        // دالة للتحقق (اختيارية لأننا سنستخدم المقارنة المباشرة)
        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            string newHash = HashPassword(enteredPassword);
            return newHash == storedHash;
        }
    }
}