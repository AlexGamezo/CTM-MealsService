
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MealsService.Models;

namespace MealsService.Responses
{
    public class ScheduleDayDto
    {
        public DateTime Date { get; set; }
        public DateTime LastModified { get; set; }

        public string DietType { get; set; }

        public List<ScheduleSlotDto> ScheduleSlots { get; set; }
    }
}
