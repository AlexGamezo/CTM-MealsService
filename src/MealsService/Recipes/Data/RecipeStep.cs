using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace MealsService.Recipes.Data
{
    public class RecipeStep
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [IgnoreDataMember]
        public int MealId { get; set; }
        public string Text { get; set; }
        public int Order { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        [IgnoreDataMember]
        public Meal Meal { get; set; }

    }
}
