
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MealsService.Recipes.Data;

namespace MealsService.Diets.Data
{
    public class PrepPlanGenerator
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int PrepPlanId { get; set; }
        public int DayOfWeek { get; set; }
        public MealType MealType { get; set; }
        public int NumServings { get; set; }

        public List<PrepPlanConsumer> Consumers { get; set; }
    }
}
