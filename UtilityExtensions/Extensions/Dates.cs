/* Author: David Carroll
 * Copyright (c) 2008, 2009 Bellevue Baptist Church 
 * Licensed under the GNU General Public License (GPL v2)
 * you may not use this code except in compliance with the License.
 * You may obtain a copy of the License at http://bvcms.codeplex.com/license 
 */
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Web;

namespace UtilityExtensions
{
    public static partial class Util
    {
        private const string STR_NOWOFFSET = "NOWOFFSET";
        public const int SignalNoYear = 1897;
        private static DateTime? GoodDate(DateTime? dt)
        {
            if (!dt.HasValue)
                return null;
            while (dt.Value.Year < 1900)
                dt = dt.Value.AddYears(100);
            return dt;
        }
        public static string Age(this string birthday)
        {
            DateTime bd;
            if (!birthday.DateTryParse(out bd))
                return "?";
            DateTime td = Now;
            int age = td.Year - bd.Year;
            if (td.Month < bd.Month || (td.Month == bd.Month && td.Day < bd.Day))
                age--;
            return age.ToString();
        }
        public static int Age0(this string birthday)
        {
            DateTime bd;
            if (!birthday.DateTryParse(out bd))
                return -1;
            DateTime td = Now;
            int age = td.Year - bd.Year;
            if (td.Month < bd.Month || (td.Month == bd.Month && td.Day < bd.Day))
                age--;
            return age;
        }
        public static int AgeAsOf(this DateTime bd, DateTime dt)
        {
            int y = bd.Year;
            if (y < 1000)
                if (y < 50)
                    y = y + 2000;
                else y = y + 1900;
            int age = dt.Year - y;
            if (dt.Month < bd.Month || (dt.Month == bd.Month && dt.Day < bd.Day))
                age--;
            return age;
        }
        public static DateTime Now
        {
            get
            {
#if DEBUG2
                return DateTime.Now.Add(NowOffset);
#else
                return DateTime.Now;
#endif
            }
        }
        public static TimeSpan NowOffset
        {
            get
            {
                var deb = TimeSpan.Zero;

                if (HttpContext.Current != null)
                {
                    if (HttpContext.Current.Session != null)
                        if (HttpContext.Current.Session[STR_SMTPDEBUG] != null)
                            deb = (TimeSpan)HttpContext.Current.Session[STR_NOWOFFSET];
                }
                else
                {
                    var localDataStoreSlot = Thread.GetNamedDataSlot(STR_NOWOFFSET);
                    deb = (TimeSpan)Thread.GetData(localDataStoreSlot);
                }
                return deb;
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    if (HttpContext.Current.Session != null)
                        HttpContext.Current.Session[STR_NOWOFFSET] = value;
                }
                else
                    Thread.SetData(Thread.GetNamedDataSlot(STR_NOWOFFSET), value);
            }
        }
        public static bool DateValid(string dt)
        {
            DateTime dt2;
            return DateValid(dt, out dt2);
        }

        public static bool DateValid(string dt, out DateTime dt2)
        {
            dt2 = DateTime.MinValue;
            if (!dt.HasValue())
                return false;

            if (DateTime.TryParse(dt, out dt2))
                return true;
            if (!Regex.IsMatch(dt, @"\A(?:\A(0[1-9]|1[012])(0[1-9]|[12][0-9]|3[01])((19|20)?[0-9]{2}))\Z"))
                return false;
            var culture = CultureInfo.InvariantCulture;
            var styles = DateTimeStyles.NoCurrentDateDefault;
            var s = dt.Substring(0, 2) + "/" + dt.Substring(2, 2) + "/" + dt.Substring(4);
            if (DateTime.TryParse(s, culture, styles, out dt2))
                return true;
            return false;
        }
        public static bool BirthDateValid(string dob, out DateTime dt2)
        {
            dt2 = DateTime.MinValue;
            if (DateTime.TryParseExact(dob, "m", CultureInfo.CurrentCulture, DateTimeStyles.None, out dt2))
            {
                dt2 = new DateTime(SignalNoYear, dt2.Month, dt2.Day);
                return true;
            }
            return DateValid(dob, out dt2);
        }
        public static bool BirthDateValid(int? bmon, int? bday, int? byear, out DateTime dt2)
        {
            dt2 = DateTime.MinValue;
            if (!bmon.HasValue || !bday.HasValue) // year is not required
                return false;
            var s = FormatBirthday(byear, bmon, bday);
            if (!DateValid(s, out dt2))
                return false;
            dt2 = new DateTime(byear ?? SignalNoYear, bmon.Value, bday.Value);
            if (dt2 > DateTime.Now)
                dt2 = dt2.AddYears(-100);
            return true;
        }
        public static int? GetWeekNumber(this DateTime? dt)
        {
            if (!dt.HasValue)
                return null;
            return GetWeekNumber(dt.Value);
        }
        public static int GetWeekNumber(this DateTime dt)
        {
            var cc = CultureInfo.CurrentCulture;
            int wk = cc.Calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
            return wk;
        }

        public static IEnumerable<DateTime> DaysOfMonth(DateTime dt)
        {
            var d = new DateTime(dt.Year, dt.Month, 1);
            while (d.Month == dt.Month)
            {
                yield return d;
                d = d.AddDays(1);
            }
        }
        public static int WeekOfMonth(this DateTime sunday)
        {
            var sundays = DaysOfMonth(sunday).Where(dd => dd.DayOfWeek == 0).ToList();
            var wk = 0;
            while (wk < sundays.Count && sunday.Date > sundays[wk])
                wk++;
            return wk + 1;
        }
        public static DateTime Sunday(int month, int year)
        {
            var first = new DateTime(year, month, 1);
            return new DateTime(year, month,
                1 + (7 - (int)first.DayOfWeek) % 7);
        }
        public static DateTime Sunday(this DateTime dt)
        {
            return dt.Date.AddDays(-(int)dt.DayOfWeek);
        }
        public static int SundaysInMonth(int month, int year)
        {
            var first = new DateTime(year, month, 1);
            var sun = new DateTime(year, month, 1 + (7 - (int)first.DayOfWeek) % 7);
            int n = 0;
            while (sun.Month == month)
            {
                n++;
                sun = sun.AddDays(7);
            }
            return n;
        }
        public static DateTime NextSemiMonthlyDate(int baseday, DateTime d)
        {
            var a = DateTime.DaysInMonth(d.Year, d.Month) / 2.0;
            a = Math.Ceiling(a);
            var d2 = d.AddDays(a);
            if (d2.Month != d.Month)
                d2 = new DateTime(d2.Year, d2.Month, baseday);
            return d2;
        }
        public static DateTime? ParseMMddyy(string s)
        {
            DateTime dt;
            if (DateTime.TryParseExact(s, "MMddyyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal,
                    out dt))
                return dt;
            if (DateTime.TryParseExact(s, "MMddyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal,
                    out dt))
                return dt;

            return null;
        }
        public static string BuildDate()
        {
            return GetBuildDate().FormatDateTm();
        }
        private static DateTime GetBuildDate()
        {
            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int cPeHeaderOffset = 60;
            const int cLinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            Stream s = null;

            try
            {
                s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                    s.Close();
            }

            var i = BitConverter.ToInt32(b, cPeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(b, i + cLinkerTimestampOffset);
            var dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }
    }
}

