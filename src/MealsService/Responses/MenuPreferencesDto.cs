using MealsService.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace MealsService.Responses
{
    public class MenuPreferencesDto
    {
        public List<DietGoal> DietGoals { get; set; }

        public int ShoppingFrequency { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MealStyle MealStyle { get; set; }

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<Meal.Type> MealTypes { get; set; }
    }
}
