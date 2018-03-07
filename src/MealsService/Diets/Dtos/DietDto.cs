
using System.Collections.Generic;

namespace MealsService.Diets.Dtos
{
    public class DietDto
    {
        public MenuPreferencesDto Preferences { get; set; }

        public List<DietGoalDto> Goals { get; set; }

        public List<PrepPlanDay> PrepPlanDays { get; set; }
    }
}
