
using System.Collections.Generic;
using MealsService.Recipes.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MealsService.Diets.Dtos
{
    public class PrepPlanGeneratorDto
    {
        public int DayOfWeek { get; set; }
        public int NumServings { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MealType MealType { get; set; }

        public List<PrepPlanConsumerDto> Consumers { get; set; }
    }

    public class PrepPlanConsumerDto
    {
        public int DayOfWeek { get; set; }
        public int NumServings { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MealType MealType { get; set; }
    }

    public class PrepPlanDay
    {
        public int DayOfWeek { get; set; }
        public List<PrepPlanMeal> Meals { get; set; }
    }

    public class PrepPlanMeal
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MealType MealType { get; set; }
        public int NumServings { get; set; }

        //Generator
        public int PreppedDay { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public MealType PreppedMeal { get; set; }
    }
}
