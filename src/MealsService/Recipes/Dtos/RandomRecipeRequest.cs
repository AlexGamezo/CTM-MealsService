
using System.Collections.Generic;
using MealsService.Recipes.Data;

namespace MealsService.Recipes.Dtos
{
    public class RandomRecipeRequest
    {
        public MealType MealType { get; set; }
        public int DietTypeId { get; set; }

        public List<string> ExcludeTags { get; set; } = new List<string>();
        public List<RecipeIngredientDto> ConsumeIngredients { get; set; } = new List<RecipeIngredientDto>();
        public List<int> ExcludeRecipes { get; set; } = new List<int>();
    }
}
