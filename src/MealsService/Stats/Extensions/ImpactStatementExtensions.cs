using MealsService.Stats.Data;

namespace MealsService.Stats.Extensions
{
    public static class ImpactStatementExtensions
    {
        public static PersonalizedStatement PersonalizeStatement(this ImpactStatement statement, int userId, StatSummary summary)
        {
            var personalized = new PersonalizedStatement
            {
                Id = statement.Id,
                CalcType = statement.CalcType,
                ImpactType = statement.ImpactType,
                Alt = statement.Alt,
                RefUrl = statement.RefUrl,
            };

            var text = statement.Text;

            if (statement.CalcType == CalculationType.STATIC)
            {
                personalized.Text = statement.Text;
            }
            else if (statement.CalcType == CalculationType.PER_MEAL)
            {

                foreach (var param in statement.Parameters)
                {
                    var value = param.Multiplier * summary?.NumMeals ?? 0;

                    if (param.Threshold < 0.001 || value < param.Threshold)
                    {
                        text = text.Replace(param.Key, value.ToString(param.Format));
                    }
                }

            }
            else if (statement.CalcType == CalculationType.PER_WEEK_DAYS)
            {
                foreach (var param in statement.Parameters)
                {
                    var value = param.Multiplier * summary?.MealsPerWeek ?? 0;
                    text = text.Replace(param.Key, value.ToString(param.Format));
                }
            }

            personalized.Text = text;

            return personalized;
        }
    }
}
