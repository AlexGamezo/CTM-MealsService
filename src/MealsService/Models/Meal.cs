
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace MealsService.Models
{
    /// <summary>
    /// Container for a set dish.
    /// * ManyToMany Ingredients
    /// * HasMany MealKeywords
    /// * ManyToMany DietType
    /// </summary>
    public class Meal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Brief { get; set; }
        public string Image { get; set; }
        public string Description { get; set; }
        public Type MealType { get; set; }
        public int PrepTime { get; set; }
        public int CookTime { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        public List<MealIngredient> MealIngredients { get; set; }
        public List<RecipeStep> Steps { get; set; }

        public List<MealDietType> MealDietTypes { get; set; }

        public enum Type
        {
            Any,
            Breakfast,
            Lunch,
            Dinner,
            Snack
        }
    }
}
