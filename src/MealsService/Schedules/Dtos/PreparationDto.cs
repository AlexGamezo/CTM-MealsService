using System;
using MealsService.Recipes.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MealsService.Schedules.Dtos
{
    public class PreparationDto : IEquatable<PreparationDto>
    {
        private DateTime _date;

        public int Id { get; set; }

        public DateTime Date
        {
            get { return _date; }
            set { _date = DateTime.SpecifyKind(value, DateTimeKind.Unspecified); }
        }
        public int RecipeId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MealType MealType { get; set; }
        public int NumServings { get; set; }
        
        public bool Equals(PreparationDto other)
        {
            return Id == other.Id;
        }
    }
}
