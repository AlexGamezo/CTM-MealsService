namespace MealsService.Schedules.Dtos
{
    public class PreparationPatchRequest
    {
        public enum Operation
        {
            Unknown = 0,
            MovePreparation = 1,
            SetRecipe = 2
        }

        public Operation Op { get; set; }

        //Used only if moving day, this is the target day to move to
        public int ScheduleDayId { get; set; }
        
        public int RecipeId { get; set; }

    }
}
