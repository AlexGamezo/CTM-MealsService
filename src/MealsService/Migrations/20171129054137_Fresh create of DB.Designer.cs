using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using MealsService;
using MealsService.Diets.Data;
using MealsService.Recipes.Data;

namespace MealsService.Migrations
{
    [DbContext(typeof(MealsDbContext))]
    [Migration("20171129054137_Fresh create of DB")]
    partial class FreshcreateofDB
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2");

            modelBuilder.Entity("MealsService.Diets.Data.DietGoal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Current");

                    b.Property<int>("ReductionRate");

                    b.Property<int>("Target");

                    b.Property<int>("TargetDietId");

                    b.Property<DateTime>("Updated")
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("TargetDietId");

                    b.ToTable("DietGoals");
                });

            modelBuilder.Entity("MealsService.Diets.Data.DietType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description");

                    b.Property<string>("Name");

                    b.Property<string>("ShortDescription");

                    b.HasKey("Id");

                    b.ToTable("DietTypes");
                });

            modelBuilder.Entity("MealsService.Diets.Data.MenuPreference", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<int>("CurrentDietTypeId");

                    b.Property<int>("MealStyle");

                    b.Property<string>("MealTypesList");

                    b.Property<int>("ShoppingFreq");

                    b.HasKey("UserId");

                    b.HasIndex("CurrentDietTypeId");

                    b.ToTable("MenuPreferences");
                });

            modelBuilder.Entity("MealsService.Ingredients.Data.Ingredient", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Brief");

                    b.Property<int?>("CategoryId");

                    b.Property<string>("Description");

                    b.Property<string>("Image");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("Ingredients");
                });

            modelBuilder.Entity("MealsService.Ingredients.Data.IngredientCategory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .HasMaxLength(32);

                    b.HasKey("Id");

                    b.ToTable("IngredientCategories");
                });

            modelBuilder.Entity("MealsService.Ingredients.Data.IngredientMeasureType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("IngredientId");

                    b.Property<int>("MeasureTypeId");

                    b.HasKey("Id");

                    b.HasIndex("IngredientId");

                    b.HasIndex("MeasureTypeId");

                    b.ToTable("IngredientMeasureTypes");
                });

            modelBuilder.Entity("MealsService.Ingredients.Data.IngredientTag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("IngredientId");

                    b.Property<int>("TagId");

                    b.HasKey("Id");

                    b.HasIndex("IngredientId");

                    b.HasIndex("TagId");

                    b.ToTable("IngredientTags");
                });

            modelBuilder.Entity("MealsService.Ingredients.Data.MeasureType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<string>("Short");

                    b.HasKey("Id");

                    b.ToTable("MeasureTypes");
                });

            modelBuilder.Entity("MealsService.Models.ScheduleDay", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Created");

                    b.Property<DateTime>("Date");

                    b.Property<int>("DietTypeId");

                    b.Property<DateTime>("Modified");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("DietTypeId");

                    b.ToTable("ScheduleDays");
                });

            modelBuilder.Entity("MealsService.Models.ScheduleGenerated", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Created")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("EndDate");

                    b.Property<string>("ExcludedTags");

                    b.Property<DateTime>("StartDate");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.ToTable("ScheduleGenerations");
                });

            modelBuilder.Entity("MealsService.Models.ScheduleSlot", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("MealId");

                    b.Property<int>("ScheduleDayId");

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.HasIndex("MealId");

                    b.HasIndex("ScheduleDayId");

                    b.ToTable("ScheduleSlots");
                });

            modelBuilder.Entity("MealsService.Recipes.Data.Meal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Brief");

                    b.Property<int>("CookTime");

                    b.Property<string>("Description");

                    b.Property<string>("Image");

                    b.Property<int>("MealType");

                    b.Property<string>("Name");

                    b.Property<int>("NumServings");

                    b.Property<int>("PrepTime");

                    b.Property<string>("Source")
                        .HasColumnType("varchar(200)");

                    b.HasKey("Id");

                    b.ToTable("Meals");
                });

            modelBuilder.Entity("MealsService.Recipes.Data.MealDietType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("DietTypeId");

                    b.Property<int>("MealId");

                    b.HasKey("Id");

                    b.HasIndex("DietTypeId");

                    b.HasIndex("MealId");

                    b.ToTable("MealDietTypes");
                });

            modelBuilder.Entity("MealsService.Recipes.Data.MealIngredient", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<float>("Amount");

                    b.Property<string>("AmountType");

                    b.Property<int>("IngredientId");

                    b.Property<int>("MealId");

                    b.Property<int>("MeasureTypeId");

                    b.HasKey("Id");

                    b.HasIndex("IngredientId");

                    b.HasIndex("MealId");

                    b.HasIndex("MeasureTypeId");

                    b.ToTable("MealIngredients");
                });

            modelBuilder.Entity("MealsService.Recipes.Data.RecipeStep", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("MealId");

                    b.Property<int>("Order");

                    b.Property<string>("Text");

                    b.HasKey("Id");

                    b.HasIndex("MealId");

                    b.ToTable("RecipeSteps");
                });

            modelBuilder.Entity("MealsService.Recipes.Data.RecipeVote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("RecipeId");

                    b.Property<int>("UserId");

                    b.Property<int>("Vote");

                    b.HasKey("Id");

                    b.HasIndex("RecipeId");

                    b.ToTable("RecipeVotes");
                });

            modelBuilder.Entity("MealsService.ShoppingList.Data.ShoppingListItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<float>("Amount");

                    b.Property<bool>("Checked");

                    b.Property<int>("IngredientId");

                    b.Property<string>("IngredientName")
                        .HasMaxLength(64);

                    b.Property<bool>("ManuallyAdded");

                    b.Property<int>("MeasureTypeId");

                    b.Property<int>("UserId");

                    b.Property<DateTime>("WeekStart");

                    b.HasKey("Id");

                    b.HasIndex("IngredientId");

                    b.HasIndex("MeasureTypeId");

                    b.ToTable("ShoppingListItems");
                });

            modelBuilder.Entity("MealsService.ShoppingList.Data.ShoppingListItemScheduleSlot", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("ScheduleSlotId");

                    b.Property<int>("ShoppingListItemId");

                    b.HasKey("Id");

                    b.HasIndex("ScheduleSlotId");

                    b.HasIndex("ShoppingListItemId");

                    b.ToTable("ShoppingListItemScheduleSlot");
                });

            modelBuilder.Entity("MealsService.Tags.Data.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("MealsService.Diets.Data.DietGoal", b =>
                {
                    b.HasOne("MealsService.Diets.Data.DietType", "TargetDietType")
                        .WithMany()
                        .HasForeignKey("TargetDietId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Diets.Data.MenuPreference", b =>
                {
                    b.HasOne("MealsService.Diets.Data.DietType", "CurrentDietType")
                        .WithMany()
                        .HasForeignKey("CurrentDietTypeId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Ingredients.Data.Ingredient", b =>
                {
                    b.HasOne("MealsService.Ingredients.Data.IngredientCategory", "IngredientCategory")
                        .WithMany()
                        .HasForeignKey("CategoryId");
                });

            modelBuilder.Entity("MealsService.Ingredients.Data.IngredientMeasureType", b =>
                {
                    b.HasOne("MealsService.Ingredients.Data.Ingredient", "Ingredient")
                        .WithMany("IngredientMeasureTypes")
                        .HasForeignKey("IngredientId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MealsService.Ingredients.Data.MeasureType", "MeasureType")
                        .WithMany()
                        .HasForeignKey("MeasureTypeId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Ingredients.Data.IngredientTag", b =>
                {
                    b.HasOne("MealsService.Ingredients.Data.Ingredient", "Ingredient")
                        .WithMany("IngredientTags")
                        .HasForeignKey("IngredientId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MealsService.Tags.Data.Tag", "Tag")
                        .WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Models.ScheduleDay", b =>
                {
                    b.HasOne("MealsService.Diets.Data.DietType", "DietType")
                        .WithMany()
                        .HasForeignKey("DietTypeId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Models.ScheduleSlot", b =>
                {
                    b.HasOne("MealsService.Recipes.Data.Meal", "Meal")
                        .WithMany()
                        .HasForeignKey("MealId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MealsService.Models.ScheduleDay", "ScheduleDay")
                        .WithMany("ScheduleSlots")
                        .HasForeignKey("ScheduleDayId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Recipes.Data.MealDietType", b =>
                {
                    b.HasOne("MealsService.Diets.Data.DietType", "DietType")
                        .WithMany()
                        .HasForeignKey("DietTypeId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MealsService.Recipes.Data.Meal", "Meal")
                        .WithMany("MealDietTypes")
                        .HasForeignKey("MealId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Recipes.Data.MealIngredient", b =>
                {
                    b.HasOne("MealsService.Ingredients.Data.Ingredient", "Ingredient")
                        .WithMany()
                        .HasForeignKey("IngredientId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MealsService.Recipes.Data.Meal", "Meal")
                        .WithMany("MealIngredients")
                        .HasForeignKey("MealId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MealsService.Ingredients.Data.MeasureType", "MeasureType")
                        .WithMany()
                        .HasForeignKey("MeasureTypeId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Recipes.Data.RecipeStep", b =>
                {
                    b.HasOne("MealsService.Recipes.Data.Meal", "Meal")
                        .WithMany("Steps")
                        .HasForeignKey("MealId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Recipes.Data.RecipeVote", b =>
                {
                    b.HasOne("MealsService.Recipes.Data.Meal")
                        .WithMany("Votes")
                        .HasForeignKey("RecipeId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.ShoppingList.Data.ShoppingListItem", b =>
                {
                    b.HasOne("MealsService.Ingredients.Data.Ingredient", "Ingredient")
                        .WithMany()
                        .HasForeignKey("IngredientId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MealsService.Ingredients.Data.MeasureType", "MeasureType")
                        .WithMany()
                        .HasForeignKey("MeasureTypeId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.ShoppingList.Data.ShoppingListItemScheduleSlot", b =>
                {
                    b.HasOne("MealsService.Models.ScheduleSlot", "ScheduleSlot")
                        .WithMany()
                        .HasForeignKey("ScheduleSlotId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MealsService.ShoppingList.Data.ShoppingListItem", "ShoppingListItem")
                        .WithMany("ScheduleSlots")
                        .HasForeignKey("ShoppingListItemId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
