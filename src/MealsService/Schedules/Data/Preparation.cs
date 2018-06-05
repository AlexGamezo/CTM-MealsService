using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using MealsService.Recipes.Data;

namespace MealsService.Schedules.Data
{
    public class Preparation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        //Maybe take out
        public int UserId { get; set; }
        public int ScheduleDayId { get; set; }
        public int RecipeId { get; set; }
        public MealType MealType { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        public Recipe Recipe { get; set; }
        public List<Meal> Meals { get; set; }
        public ScheduleDay ScheduleDay { get; set; }
    }
}
