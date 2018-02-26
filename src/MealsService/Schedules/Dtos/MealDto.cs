using MealsService.Schedules.Data;

namespace MealsService.Responses.Schedules
{
    public class MealDto
    {
        public int Id { get; set; }
        public string MealType { get; set; }
        public int RecipeId { get; set; }
        public ConfirmStatus Confirmed { get; set; }
        public int ScheduleDayId { get; set; }
        public bool IsChallenge { get; set; }
    }
}
