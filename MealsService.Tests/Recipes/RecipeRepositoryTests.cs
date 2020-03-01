using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using FakeItEasy;
using FluentAssertions;
using MealsService.Ingredients.Data;
using NUnit.Framework;

using MealsService.Recipes;
using MealsService.Recipes.Data;


namespace MealsService.Tests.Recipes
{
    public class RecipeRepositoryTests
    {
        private DbContextOptions<MealsDbContext> _options;

        [SetUp]
        public void Setup()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            _options = new DbContextOptionsBuilder<MealsDbContext>()
                .UseSqlite(connection)
                .Options;
        }

        private void PopulateFakeIngredients()
        {
            var context = new MealsDbContext(_options);
            context.Database.EnsureCreated();

            context.Ingredients.AddRange(new List<Ingredient>
            {
                new Ingredient{ Id = 1, Name = "Ingredient 1"},
                new Ingredient{ Id = 2, Name = "Ingredient 2"},
                new Ingredient{ Id = 3, Name = "Ingredient 3"}
            });

            context.SaveChanges();
        }

        private IRecipeRepository GetRecipeRepository()
        {
            var context = new MealsDbContext(_options);
            context.Database.EnsureCreated();

            return new RecipeRepository(context);
        }

        [Test]
        public void CreateRecipe()
        {
            var repo = GetRecipeRepository();

            var recipe = new Recipe
            {
                Name = "Recipe 1",
                Brief = "Brief description",
            };

            repo.SaveRecipe(recipe).Should().BeTrue();

            var context = new MealsDbContext(_options);
            var recipes = context.Recipes.ToList();

            recipes.Should().NotBeNullOrEmpty();
            recipes.Count.Should().Be(1);
            recipes[0].Id.Should().Be(1);
            recipes[0].Name.Should().Be("Recipe 1");
        }

        [Test]
        public void SetIngredients()
        {
            PopulateFakeIngredients();

            var repo = GetRecipeRepository();

            var recipe = new Recipe
            {
                Name = "Recipe 1",
                Brief = "Brief description",
            };

            repo.SaveRecipe(recipe).Should().BeTrue();

            repo.SetRecipeIngredients(recipe.Id, new List<RecipeIngredient>
            {
                new RecipeIngredient {Amount = 2.5f, IngredientId = 1},
                new RecipeIngredient {Amount = 0.5f, IngredientId = 3},
            });

            var context = new MealsDbContext(_options);
            var recipeIngredients = context.RecipeIngredients.ToList();

            recipeIngredients.Should().NotBeNullOrEmpty();
            recipeIngredients.Count.Should().Be(2);
            recipeIngredients[0].Id.Should().Be(1);
            recipeIngredients[0].RecipeId.Should().Be(1);
            recipeIngredients[0].IngredientId.Should().Be(1);

            repo.SetRecipeIngredients(recipe.Id, new List<RecipeIngredient>
            {
                new RecipeIngredient {Amount = 3.2f, IngredientId = 2}
            });

            context = new MealsDbContext(_options);
            recipeIngredients = context.RecipeIngredients.ToList();

            recipeIngredients.Should().NotBeNullOrEmpty();
            recipeIngredients.Count.Should().Be(1);
            recipeIngredients[0].Id.Should().Be(1);
            recipeIngredients[0].RecipeId.Should().Be(1);
            recipeIngredients[0].IngredientId.Should().Be(2);
        }
    }
}
