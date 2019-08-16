using Microsoft.EntityFrameworkCore;

using MealsService.Diets.Data;
using MealsService.Ingredients.Data;
using MealsService.Recipes.Data;
using MealsService.Schedules.Data;
using MealsService.ShoppingList.Data;
using MealsService.Stats.Data;
using MealsService.Tags.Data;

namespace MealsService
{
    public class MealsDbContext : DbContext
    {
        public MealsDbContext(DbContextOptions<MealsDbContext> options)
            : base(options)
        { }

        public DbSet<DietType> DietTypes { get; set; }
        public DbSet<MeasureType> MeasureTypes { get; set; }
        public DbSet<MeasureConverter> MeasureConverters { get; set; }

        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<IngredientCategory> IngredientCategories { get; set; }
        public DbSet<IngredientTag> IngredientTags { get; set; }
        public DbSet<IngredientMeasureType> IngredientMeasureTypes { get; set; }

        public DbSet<RecipeDietType> RecipeDietTypes { get; set; } 
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeStep> RecipeSteps { get; set; }
        public DbSet<RecipeVote> RecipeVotes { get; set; }

        public DbSet<ImpactStatement> ImpactStatements { get; set; }
        public DbSet<DidYouKnowStat> DidYouKnowStats { get; set; }
        public DbSet<StatSnapshot> StatSnapshots { get; set; }
        public DbSet<StatSummary> StatSummaries { get; set; }


        #region User-Specific

        public DbSet<ScheduleGenerated> ScheduleGenerations { get; set; }
        public DbSet<ScheduleDay> ScheduleDays { get; set; }
        public DbSet<Meal> Meals { get; set; }
        public DbSet<Preparation> Preparations { get; set; }

        public DbSet<PrepPlan> PrepPlans { get; set; }
        public DbSet<PrepPlanGenerator> PrepPlanGenerators { get; set; }
        public DbSet<PrepPlanConsumer> PrepPlanConsumers { get; set; }
        
        public DbSet<ShoppingListItem> ShoppingListItems { get; set; }

        //Configurations/User Options
        public DbSet<DietGoal> DietGoals { get; set; }
        public DbSet<MenuPreference>  MenuPreferences { get; set; }
        public DbSet<ChangeDay> ChangeDays { get; set; }
        
        #endregion

        public DbSet<Tag> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DietGoal>()
                .HasIndex(g => g.UserId);
            modelBuilder.Entity<ScheduleDay>()
                .HasIndex(d => d.UserId);
            modelBuilder.Entity<ScheduleGenerated>()
                .HasIndex(g => g.UserId);

            modelBuilder.Entity<ShoppingListItem>()
                .HasIndex(i => new {i.UserId, i.WeekStart});

            modelBuilder.Entity<RecipeVote>()
                .HasIndex(v => v.UserId);

            modelBuilder.Entity<Recipe>()
                .HasIndex(v => v.Slug);
        }
    }
}
