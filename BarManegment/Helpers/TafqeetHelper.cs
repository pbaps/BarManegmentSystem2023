using System;

namespace BarManegment.Helpers
{
    public static class TafqeetHelper
    {
        private static readonly string[] Ones = { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة" };
        private static readonly string[] Teens = { "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };
        private static readonly string[] Tens = { "", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };
        private static readonly string[] Hundreds = { "", "مئة", "مئتان", "ثلاثمئة", "أربعمئة", "خمسمئة", "ستمئة", "سبعمئة", "ثمانمئة", "تسعمئة" };
        private static readonly string[] Thousands = { "", "ألف", "ألفان", "ثلاثة آلاف", "أربعة آلاف", "خمسة آلاف", "ستة آلاف", "سبعة آلاف", "ثمانية آلاف", "تسعة آلاف", "عشرة آلاف" };

        private static string ConvertNumberToWords(long number)
        {
            if (number == 0) return "صفر";
            if (number < 0) return "سالب " + ConvertNumberToWords(Math.Abs(number));

            string words = "";

            if ((number / 1000000) > 0)
            {
                words += ConvertNumberToWords(number / 1000000) + " مليون ";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                if (number / 1000 == 1) words += "ألف ";
                else if (number / 1000 == 2) words += "ألفان ";
                else if (number / 1000 > 2 && number / 1000 < 11) words += Thousands[number / 1000] + " ";
                else words += ConvertNumberToWords(number / 1000) + " ألف ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += Hundreds[number / 100] + " ";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "") words += "و";

                if (number < 10)
                    words += Ones[number];
                else if (number < 20)
                    words += Teens[number - 10];
                else
                {
                    words += Ones[number % 10];
                    if ((number % 10) > 0) words += " و";
                    words += Tens[number / 10];
                }
            }
            return words.Trim();
        }

        public static string ConvertToArabic(decimal number, string currencySymbol)
        {
            if (number == 0) return "فقط صفر لا غير";

            long mainPart = (long)Math.Truncate(number);
            int fractionalPart = (int)Math.Round((number - mainPart) * 100);

            string mainCurrency, fractionalCurrency;

            if (currencySymbol == "JD")
            {
                mainCurrency = (mainPart == 1) ? "دينار أردني" : (mainPart == 2) ? "ديناران أردنيان" : "دينار أردني";
                fractionalCurrency = (fractionalPart == 1) ? "قرش واحد" : (fractionalPart == 2) ? "قرشان" : "قرش";
            }
            else if (currencySymbol == "₪")
            {
                mainCurrency = (mainPart == 1) ? "شيكل" : (mainPart == 2) ? "شيكلان" : "شيكل";
                fractionalCurrency = (fractionalPart == 1) ? "أغورة واحدة" : (fractionalPart == 2) ? "أغورتان" : "أغورة";
            }
            else if (currencySymbol == "$")
            {
                mainCurrency = (mainPart == 1) ? "دولار أمريكي" : (mainPart == 2) ? "دولاران أمريكيان" : "دولار أمريكي";
                fractionalCurrency = "سنت";
            }
            else
            {
                mainCurrency = "وحدة";
                fractionalCurrency = "جزء";
            }

            string result = "فقط " + ConvertNumberToWords(mainPart) + " " + mainCurrency;

            if (fractionalPart > 0)
            {
                result += " و" + ConvertNumberToWords(fractionalPart) + " " + fractionalCurrency;
            }

            return result + " لا غير";
        }
    }
}