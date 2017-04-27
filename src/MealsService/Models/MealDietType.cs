
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace MealsService.Models
{
    public class MealDietType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int MealId { get; set; }
        public int DietTypeId { get; set; }

        //RELATIONSHIPS

        public Meal Meal { get; set; }
        public DietType DietType { get; set; }
    }
}
