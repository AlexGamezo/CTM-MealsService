using System.Linq;
using FakeItEasy;
using FluentAssertions;
using MealsService.Recipes;
using MealsService.Recipes.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace MealsService.Tests
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
                Brief = "Brief description"
            };

            repo.SaveRecipe(recipe).Should().BeTrue();

            var context = new MealsDbContext(_options);
            var recipes = context.Recipes.ToList();

            recipes.Should().NotBeNullOrEmpty();
            recipes.Count.Should().Be(1);
            recipes[0].Id.Should().Be(1);
            recipes[0].Name.Should().Be("Recipe 1");
        }
    }
}
