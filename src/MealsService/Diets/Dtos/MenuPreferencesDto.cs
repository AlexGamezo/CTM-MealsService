using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using MealsService.Diets.Data;
using MealsService.Recipes.Data;

namespace MealsService.Diets.Dtos
{
    public class MenuPreferencesDto
    {
        public int ShoppingFreq { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public RecipeStyle RecipeStyle { get; set; }

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<MealType> MealTypes { get; set; }

        public int CurrentDietId { get; set; }
    }
}
