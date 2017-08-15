
using System.Collections.Generic;
using MealsService.Recipes.Dtos;

namespace MealsService.Requests
{
    public class GenerateScheduleRequest
    {
        public List<string> ExcludeTags { get; set; }
        public List<RecipeIngredientDto> RecipeIngredients { get; set; }
    }
}
