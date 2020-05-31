using System.Collections.Generic;
using MealsService.Ingredients.Dtos;
using MealsService.Recipes.Dtos;
using MealsService.Responses.Schedules;
using MealsService.Stats.Data;

namespace MealsService.Schedules.Dtos
{
    public class ScheduleRecipeContainer
    {
        public List<ScheduleDayDto> Schedule { get; set; }
        public Dictionary<int, RecipeDto> Recipes { get; set; }

        public Dictionary<int, IngredientDto> Ingredients { get; set; }
        public DidYouKnowStat DidYouKnowStat { get; set; }

    }
}
