using System;

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
    }
}
