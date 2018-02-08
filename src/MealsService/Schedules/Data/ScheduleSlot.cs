using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using MealsService.Recipes.Data;

namespace MealsService.Models
{
    public enum ConfirmStatus
    {
        UNSET = 0,
        CONFIRMED_YES = 1,
        CONFIRMED_NO = 2
    }

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
        public ConfirmStatus ConfirmStatus { get; set; }
        public bool IsChallenge { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        
        [IgnoreDataMember]
        public ScheduleDay ScheduleDay { get; set; }
        public Meal Meal { get; set; }
    }
}
