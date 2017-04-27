using Microsoft.EntityFrameworkCore;

namespace MealsService.Models
{
    public class MealsDbContext : DbContext
    {
        public MealsDbContext(DbContextOptions<MealsDbContext> options)
            : base(options)
        { }

        public DbSet<DietType> DietTypes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<IngredientCategory> IngredientCategories { get; set; }


        public DbSet<MealDietType> MealDietTypes { get; set; } 
        public DbSet<MealIngredient> MealIngredients { get; set; }
        public DbSet<Meal> Meals { get; set; }
        public DbSet<RecipeStep> RecipeSteps { get; set; }


        #region User-Specific

        public DbSet<ScheduleDay> ScheduleDays { get; set; }
        public DbSet<ScheduleSlot> ScheduleSlots { get; set; }

        //Configurations/User Options
        public DbSet<DietGoal> DietGoals { get; set; }
        public DbSet<MenuPreference>  MenuPreferences { get; set; }
        
        #endregion
    }
}
