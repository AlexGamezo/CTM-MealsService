using System;
using System.Collections.Generic;

namespace MealsService.Responses.Schedules
{
    public class ScheduleDayDto
    {
        private DateTime _date;
        private DateTime _lastModified;

        public int Id { get; set; }

        public DateTime Date
        {
            get { return _date; }
            set { _date = DateTime.SpecifyKind(value, DateTimeKind.Unspecified); }
        }

        public DateTime LastModified
        {
            get { return _lastModified; }
            set { _lastModified = DateTime.SpecifyKind(value, DateTimeKind.Unspecified); }
        }

        public string DietType { get; set; }
        public int DietTypeId { get; set; }

        public bool IsChallenge { get; set; }
        public List<MealDto> Meals { get; set; }
    }
}
