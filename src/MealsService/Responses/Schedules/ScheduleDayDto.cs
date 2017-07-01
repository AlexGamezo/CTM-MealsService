
using System;
using System.Collections.Generic;

namespace MealsService.Responses.Schedules
{
    public class ScheduleDayDto
    {
        public DateTime Date { get; set; }
        public DateTime LastModified { get; set; }

        public string DietType { get; set; }

        public List<ScheduleSlotDto> ScheduleSlots { get; set; }
    }
}
