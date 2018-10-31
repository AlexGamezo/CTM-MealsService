using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealsService.Stats.Data
{
    public class StatSummary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }

        public int NumMeals { get; set; }
        public int NumChallenges { get; set; }
        public int MealsPerWeek { get; set; }

        public int CurrentStreak { get; set; }
    }
}
