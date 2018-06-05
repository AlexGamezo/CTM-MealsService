using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using MealsService.Recipes.Data;

namespace MealsService.Schedules.Data
{
    public enum ConfirmStatus
    {
        UNSET = 0,
        CONFIRMED_YES = 1,
        CONFIRMED_NO = 2
    }

    /// <summary>
    /// Individual slot for a given schedule day
    /// * HasOne Recipe
    /// </summary>
    public class Meal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ScheduleDayId { get; set; }
        public int PreparationId { get; set; }
        public int RecipeId { get; set; }
        public MealType Type { get; set; }
        public ConfirmStatus ConfirmStatus { get; set; }
        public bool IsChallenge { get; set; }
        public int Servings { get; set; }
        public bool IsLeftovers { get; set; }
        
        /// <summary>
        /// Relationships
        /// </summary>
        
        [IgnoreDataMember]
        public ScheduleDay ScheduleDay { get; set; }
        public Recipe Recipe { get; set; }

        public Preparation Preparation { get; set; }
    }
}
