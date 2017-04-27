using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using MealsService.Models;

namespace MealsService.Migrations
{
    [DbContext(typeof(MealsDbContext))]
    [Migration("20170224045552_Added Recipe Steps")]
    partial class AddedRecipeSteps
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752");

            modelBuilder.Entity("MealsService.Models.DietGoal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Current");

                    b.Property<int>("ReductionRate");

                    b.Property<int>("Target");

                    b.Property<int>("TargetDietId");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("TargetDietId");

                    b.ToTable("DietGoals");
                });

            modelBuilder.Entity("MealsService.Models.DietType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description");

                    b.Property<string>("Name");

                    b.Property<string>("ShortDescription");

                    b.HasKey("Id");

                    b.ToTable("DietTypes");
                });

            modelBuilder.Entity("MealsService.Models.Ingredient", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Brief");

                    b.Property<int?>("CategoryId");

                    b.Property<string>("Description");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("Ingredients");
                });

            modelBuilder.Entity("MealsService.Models.IngredientCategory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .HasMaxLength(32);

                    b.HasKey("Id");

                    b.ToTable("IngredientCategories");
                });

            modelBuilder.Entity("MealsService.Models.Meal", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Brief");

                    b.Property<int>("CookTime");

                    b.Property<string>("Description");

                    b.Property<string>("Image");

                    b.Property<int>("MealType");

                    b.Property<string>("Name");

                    b.Property<int>("PrepTime");

                    b.HasKey("Id");

                    b.ToTable("Meals");
                });

            modelBuilder.Entity("MealsService.Models.MealIngredient", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<float>("Amount");

                    b.Property<string>("AmountType");

                    b.Property<int>("IngredientId");

                    b.Property<int>("MealId");

                    b.HasKey("Id");

                    b.HasIndex("IngredientId");

                    b.HasIndex("MealId");

                    b.ToTable("MealIngredients");
                });

            modelBuilder.Entity("MealsService.Models.MenuPreference", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<int>("MealStyle");

                    b.Property<int>("ShoppingFreq");

                    b.HasKey("UserId");

                    b.ToTable("MenuPreferences");
                });

            modelBuilder.Entity("MealsService.Models.RecipeStep", b =>
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

            modelBuilder.Entity("MealsService.Models.DietGoal", b =>
                {
                    b.HasOne("MealsService.Models.DietType", "TargetDietType")
                        .WithMany()
                        .HasForeignKey("TargetDietId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Models.Ingredient", b =>
                {
                    b.HasOne("MealsService.Models.IngredientCategory", "Category")
                        .WithMany()
                        .HasForeignKey("CategoryId");
                });

            modelBuilder.Entity("MealsService.Models.MealIngredient", b =>
                {
                    b.HasOne("MealsService.Models.Ingredient", "Ingredient")
                        .WithMany()
                        .HasForeignKey("IngredientId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MealsService.Models.Meal", "Meal")
                        .WithMany("MealIngredients")
                        .HasForeignKey("MealId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Models.RecipeStep", b =>
                {
                    b.HasOne("MealsService.Models.Meal", "Meal")
                        .WithMany("Steps")
                        .HasForeignKey("MealId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Models.ScheduleDay", b =>
                {
                    b.HasOne("MealsService.Models.DietType", "DietType")
                        .WithMany()
                        .HasForeignKey("DietTypeId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("MealsService.Models.ScheduleSlot", b =>
                {
                    b.HasOne("MealsService.Models.Meal", "Meal")
                        .WithMany()
                        .HasForeignKey("MealId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("MealsService.Models.ScheduleDay", "ScheduleDay")
                        .WithMany("ScheduleSlots")
                        .HasForeignKey("ScheduleDayId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
