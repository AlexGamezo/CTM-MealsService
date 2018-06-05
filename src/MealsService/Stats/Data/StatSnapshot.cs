
using System;
using System.ComponentModel.DataAnnotations.Schema;
using NodaTime;

namespace MealsService.Stats.Data
{
    public class StatSnapshot
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public DateTime Week { get; set; }

        public int Goal { get; set; }
        public int Value { get; set; }
        public int Challenges { get; set; }
        public int MealsPerDay { get; set; }
        public int Streak { get; set; }

        [NotMapped]
        public LocalDate NodaWeek
        {
            get => LocalDate.FromDateTime(Week);
            set => Week = value.ToDateTimeUnspecified();
        }
    }
}
