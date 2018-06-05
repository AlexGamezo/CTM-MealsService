
namespace MealsService.Schedules.Dtos
{
    public class ScheduleDayPatchRequest
    {
        public enum Operation
        {
            Unknown = 0,
            AcceptChallenge = 1,
            DeclineChallenge = 2
        }

        public Operation Op { get; set; }
    }
}
