using System;

namespace MealsService.Common.Extensions
{
    public static class NumberExtensions
    {
        public static double RoundToQuarter(this double value)
        {
            return Math.Round(value * 4) / 4;
        }

        public static double RoundToThird(this double value)
        {
            return Math.Round(value * 3) / 3;
        }
    }
}
