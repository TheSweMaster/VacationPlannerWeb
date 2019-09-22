using System;
using System.Collections.Generic;
using System.Globalization;

namespace VacationPlannerWeb.Extensions
{
    public static class CalendarHelper
    {
        public static List<string> GetDayOfWeekList()
        {
            return new List<string>() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        }

        public static List<string> GetDayOfWeekListShort()
        {
            return new List<string>() { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        }

        public static DateTime GetFirstDayInWeek(DateTime dayInWeek, List<string> dayOfWeekList)
        {
            int weekdayNumberOfFirstDay = dayOfWeekList.FindIndex(x => x == dayInWeek.DayOfWeek.ToString());
            DateTime dateOfFirstDayInWeek = dayInWeek.AddDays(-weekdayNumberOfFirstDay);
            return dateOfFirstDayInWeek;
        }

        public static DateTime GetFirstDayOfMonth(int year, int month)
        {
            return new DateTime(year, month, 1);
        }

        public static DateTime GetFirstDayOfMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear, CultureInfo culture)
        {
            // Source: https://stackoverflow.com/a/9064954 
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - culture.Calendar.GetDayOfWeek(jan1);

            DateTime firstThursday = jan1.AddDays(daysOffset);
            int firstWeek = culture.Calendar.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            if (firstWeek == 1)
            {
                weekOfYear -= 1;
            }
            //Todo: Throw exception if weekOfYear bigger than allowed (52,53) or lower?
            var result = firstThursday.AddDays(weekOfYear * 7);

            return result.AddDays(-3);
        }

        public static int GetISO8601WeekNumber(DateTime date, CultureInfo culture)
        {
            // Source: https://stackoverflow.com/a/11155102 
            DayOfWeek day = culture.Calendar.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }

            return culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public static int GetLastWeekOfYear(int year, CultureInfo culture)
        {
            var dayInLastWeekOfDecember = new DateTime(year, 12, 28);
            return GetISO8601WeekNumber(dayInLastWeekOfDecember, culture);
        }

    }
}
