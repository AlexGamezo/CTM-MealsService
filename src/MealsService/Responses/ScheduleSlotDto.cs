using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MealsService.Models;

namespace MealsService.Responses
{
    public class ScheduleSlotDto
    {
        public int Id { get; set; }
        public string MealType { get; set; }

        public MealDto Meal { get; set; }
    }
}
