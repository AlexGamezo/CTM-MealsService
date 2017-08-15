using System.Collections.Generic;

using MealsService.Recipes.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MealsService.Recipes.Dtos
{
    public class RecipeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Brief { get; set; }

        public string Image { get; set; }

        public int PrepTime { get; set; }
        public int CookTime { get; set; }

        public string MealType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public RecipeVote.VoteType Vote { get; set; }

        public List<RecipeIngredientDto> Ingredients {get; set; }

        public List<RecipeStep> Steps { get; set; }
        public List<int> DietTypes { get; set; }
    }
}
