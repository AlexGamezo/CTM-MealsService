using System.Threading.Tasks;

using MealsService.Users.Data;

namespace MealsService.Notifications
{
    public interface INotificationsService
    {
        Task<bool> SendNextWeekScheduleNotificationsAsync();
        Task<bool> SendNextWeekScheduleAsync(UserDto user);

    }
}
