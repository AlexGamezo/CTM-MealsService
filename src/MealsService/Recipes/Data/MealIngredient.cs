
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

using MealsService.Ingredients.Data;

namespace MealsService.Recipes.Data
{
    /// <summary>
    /// Link table associating a Meal with an Ingredient
    /// </summary>
    public class MealIngredient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int MealId { get; set; }
        public int IngredientId { get; set; }
        public float Amount { get; set; }
        public string AmountType { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        [IgnoreDataMember]
        public Meal Meal { get; set; }
        public Ingredient Ingredient { get; set; }
    }
}
