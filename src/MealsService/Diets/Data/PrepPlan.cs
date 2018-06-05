
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealsService.Diets.Data
{
    public class PrepPlan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int UserId { get; set; }
        public int NumTargetDays { get; set; }

        public List<PrepPlanGenerator> Generators { get; set; }
        public List<PrepPlanConsumer> Consumers { get; set; }
    }
}
