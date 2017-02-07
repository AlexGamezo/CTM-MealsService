
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public List<MealIngredient> MealIngredients { get; set; }

        public enum Type
        {
            Breakfast,
            Lunch,
            Dinner,
            Snack
        }
    }
}
