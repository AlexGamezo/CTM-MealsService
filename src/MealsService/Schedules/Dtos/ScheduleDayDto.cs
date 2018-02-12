
using System;
using System.Collections.Generic;

namespace MealsService.Responses.Schedules
{
    public class ScheduleDayDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public DateTime LastModified { get; set; }

        public string DietType { get; set; }
        public int DietTypeId { get; set; }

        public bool IsChallenge { get; set; }
        public List<ScheduleSlotDto> ScheduleSlots { get; set; }
    }
}
