
using System.Collections.Generic;
using MealsService.Recipes.Data;

namespace MealsService.Recipes.Dtos
{
    public class RandomRecipeRequest
    {
        public MealType MealType { get; set; }
        public int DietTypeId { get; set; }

        public List<string> ExcludeTags { get; set; }
        public List<RecipeIngredientDto> ConsumeIngredients { get; set; }
    }
}
