using System;
using MealsService.Schedules.Data;
using MealsService.Schedules.Dtos;

namespace MealsService.Responses.Schedules
{
    public class MealDto
    {
        public int Id { get; set; }
        public string MealType { get; set; }
        public int RecipeId { get; set; }
        public PreparationDto Preparation { get; set; }
        public ConfirmStatus Confirmed { get; set; }
        public int ScheduleDayId { get; set; }
        public DateTime Date { get; set; }
        public bool IsChallenge { get; set; }
        public bool IsLeftovers { get; set; }
        public int NumServings { get; set; }
    }
}
