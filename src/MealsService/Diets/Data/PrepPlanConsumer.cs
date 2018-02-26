using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MealsService.Recipes.Data;

namespace MealsService.Diets.Data
{
    public class PrepPlanConsumer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int DayOfWeek { get; set; }
        public MealType MealType { get; set; }
        public int GeneratorId { get; set; }
        public int PrepPlanId { get; set; }
        public int NumServings { get; set; }

        public PrepPlanGenerator Generator { get; set; }
        public PrepPlan PrepPlan { get; set; }
    }
}
