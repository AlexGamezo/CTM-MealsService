
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MealsService.Recipes.Data
{
    /// <summary>
    /// Container for a set dish.
    /// * ManyToMany Ingredients
    /// * ManyToMany DietType
    /// </summary>
    public class Recipe
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [StringLength(64)]
        public string Name { get; set; }

        [StringLength(64)]
        public string Slug { get; set; }

        [StringLength(128)]
        public string Brief { get; set; }
        [StringLength(512)]
        public string Description { get; set; }

        [StringLength(80)]
        public string Image { get; set; }

        public MealType MealType { get; set; }

        public int PrepTime { get; set; }
        public int CookTime { get; set; }
        public int NumServings { get; set; }

        [StringLength(200)]
        public string Source { get; set; }

        public int Priority { get; set; }

        public bool Deleted { get; set; }

        /// <summary>
        /// Relationships
        /// </summary>
        public List<RecipeIngredient> RecipeIngredients { get; set; }
        public List<RecipeStep> Steps { get; set; }

        [ForeignKey("RecipeId")]
        public List<RecipeVote> Votes { get; set; }

        public List<RecipeDietType> RecipeDietTypes { get; set; }
    }

    public enum MealType
    {
        Any = 0,
        Breakfast = 1,
        Lunch = 2,
        Dinner = 3,
        Snack = 4
    }
}
