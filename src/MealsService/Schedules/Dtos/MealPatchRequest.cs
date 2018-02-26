using MealsService.Schedules.Data;

namespace MealsService.Schedules.Dtos
{
    public class MealPatchRequest
    {
        public enum Operation
        {
            Unknown = 0,
            MoveMeal = 1,
            UpdateConfirmState = 2
        }

        public Operation Op { get; set; }

        //Used only if moving day, this is the target day to move to
        public int ScheduleDayId { get; set; }

        //Used only if 
        public ConfirmStatus Confirm { get; set; }

        public bool IsChallenge { get; set; }

    }
}
