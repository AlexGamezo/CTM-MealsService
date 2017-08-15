using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using MealsService.Recipes.Data;

namespace MealsService.Models
{
    /// <summary>
    /// Individual slot for a given schedule day
    /// * HasOne Meal
    /// </summary>
    public class ScheduleSlot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ScheduleDayId { get; set; }
        public int MealId { get; set; }
        public Meal.Type Type { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        
        [IgnoreDataMember]
        public ScheduleDay ScheduleDay { get; set; }
        public Meal Meal { get; set; }
    }
}
