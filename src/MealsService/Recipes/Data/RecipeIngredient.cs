
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using MealsService.Ingredients.Data;

namespace MealsService.Recipes.Data
{
    /// <summary>
    /// Link table associating a Recipe with an Ingredient
    /// </summary>
    public class RecipeIngredient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int IngredientId { get; set; }
        public float Amount { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        [IgnoreDataMember]
        public Recipe Recipe { get; set; }
        public Ingredient Ingredient { get; set; }
    }
}
