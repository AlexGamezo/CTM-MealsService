
using MealsService.Models;
using MealsService.Schedules.Dtos;

namespace MealsService.Schedules.Data
{
    public class ScheduleSlotConfirmation
    {
        public int Id { get; set; }
        public int ScheduleSlotId { get; set; }
        public int UserId { get; set; }
        public ConfirmStatus Confirm { get; set; }

        /// <summary>
        /// Relations
        /// </summary>
        public ScheduleSlot ScheduleSlot { get; set; }
    }
}
