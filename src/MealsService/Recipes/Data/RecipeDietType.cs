using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using MealsService.Diets.Data;

namespace MealsService.Recipes.Data
{
    public class RecipeDietType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int RecipeId { get; set; }
        public int DietTypeId { get; set; }

        //RELATIONSHIPS

        public Recipe Recipe { get; set; }
        public DietType DietType { get; set; }
    }
}
