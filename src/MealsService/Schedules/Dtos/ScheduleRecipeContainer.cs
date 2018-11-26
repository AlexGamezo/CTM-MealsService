using System.Collections.Generic;
using MealsService.Recipes.Dtos;
using MealsService.Responses.Schedules;

namespace MealsService.Schedules.Dtos
{
    public class ScheduleRecipeContainer
    {
        public List<ScheduleDayDto> Schedule { get; set; }
        public Dictionary<int, RecipeDto> Recipes { get; set; }
    }
}
