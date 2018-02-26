
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

        public string Name { get; set; }
        public string Slug { get; set; }

        public string Brief { get; set; }
        public string Description { get; set; }

        public string Image { get; set; }

        public MealType MealType { get; set; }

        public int PrepTime { get; set; }
        public int CookTime { get; set; }
        public int NumServings { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string Source { get; set; }

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
