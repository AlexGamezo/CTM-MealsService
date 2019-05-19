using System;
using NodaTime;

namespace MealsService.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime GetWeekStart(this DateTime when)
        {
            when = when.Date;

            var days = (int)when.DayOfWeek - 1;
            if (days < 0) days += 7;

            return when.Subtract(new TimeSpan(days, 0, 0, 0));
        }

        public static DateTime GetWeekStart(this DateTime? when)
        {
            if (!when.HasValue)
            {
                when = DateTime.UtcNow;
            }

            return when.Value.GetWeekStart();
        }

        public static LocalDate GetWeekStart(this LocalDate when)
        {
            return when.PlusDays(-((int)when.DayOfWeek - 1));
        }

        public static DateTime GetWeekEnd(this DateTime when)
        {
            return when.GetWeekStart().AddDays(6);
        }

        public static LocalDate GetWeekEnd(this LocalDate when)
        {
            return when.GetWeekStart().PlusDays(6);
        }
    }
}
