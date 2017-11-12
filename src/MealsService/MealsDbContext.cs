using MealsService.Diets.Data;
using MealsService.Ingredients.Data;
using MealsService.Models;
using MealsService.Recipes.Data;
using MealsService.ShoppingList.Data;
using MealsService.Tags.Data;
using Microsoft.EntityFrameworkCore;

namespace MealsService
{
    public class MealsDbContext : DbContext
    {
        public MealsDbContext(DbContextOptions<MealsDbContext> options)
            : base(options)
        { }

        public DbSet<DietType> DietTypes { get; set; }
        public DbSet<MeasureType> MeasureTypes { get; set; }

        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<IngredientCategory> IngredientCategories { get; set; }
        public DbSet<IngredientTag> IngredientTags { get; set; }
        public DbSet<IngredientMeasureType> IngredientMeasureTypes { get; set; }

        public DbSet<MealDietType> MealDietTypes { get; set; } 
        public DbSet<MealIngredient> MealIngredients { get; set; }
        public DbSet<Meal> Meals { get; set; }
        public DbSet<RecipeStep> RecipeSteps { get; set; }
        public DbSet<RecipeVote> RecipeVotes { get; set; }


        #region User-Specific

        public DbSet<ScheduleGenerated> ScheduleGenerations { get; set; }
        public DbSet<ScheduleDay> ScheduleDays { get; set; }
        public DbSet<ScheduleSlot> ScheduleSlots { get; set; }

        public DbSet<ShoppingListItem> ShoppingListItems { get; set; }

        //Configurations/User Options
        public DbSet<DietGoal> DietGoals { get; set; }
        public DbSet<MenuPreference>  MenuPreferences { get; set; }
        
        #endregion

        public DbSet<Tag> Tags { get; set; }
    }
}
