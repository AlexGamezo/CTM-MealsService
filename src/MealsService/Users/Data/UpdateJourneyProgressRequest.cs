namespace MealsService.Users.Data
{
    public class UpdateJourneyProgressRequest
    {
        public int JourneyStepId { get; set; }

        public bool Completed { get; set; }
    }
}
