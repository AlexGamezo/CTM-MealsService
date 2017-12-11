
namespace MealsService.Schedules.Dtos
{
    public class ConfirmDayRequest
    {
        public int ScheduleSlotId { get; set; }
        public ConfirmStatus Confirmation { get; set; }
    }

    public enum ConfirmStatus
    {
        UNSET = 0,
        CONFIRMED_YES = 1,
        CONFIRMED_NO = 2
    }
}
