using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using NUnit.Framework;
using FakeItEasy;
using FluentAssertions;

using MealsService;
using MealsService.Ingredients;
using MealsService.Ingredients.Data;
using System.Collections.Generic;
using MealsService.Tags;
using MealsService.Tags.Data;
using System.Linq;

namespace MealsService.Tests
{
    public class IngredientRepositoryTests
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

        private IIngredientsRepository GetIngredientsRepository()
        {
            var context = new MealsDbContext(_options);
            context.Database.EnsureCreated();

            var fakeTagService = GetTagsService();
            
            return new IngredientsRepository(context, fakeTagService);
        }

        private ITagsService GetTagsService()
        {
            var context = new MealsDbContext(_options);
            context.Database.EnsureCreated();

            var tags = new List<Tag> { new Tag { Id = 1, Name = "tag1" }, new Tag { Id = 2, Name = "tag2" } };

            var fakeTagService = A.Fake<ITagsService>();

            A.CallTo(() => fakeTagService.GetOrCreateTags(A<IEnumerable<string>>.Ignored))
                .ReturnsLazily((IEnumerable<string> tagStrings) =>
                {
                    var missingTags = tagStrings
                        .Where(t => tags.All(tag => tag.Name != t))
                        .Select(t => new Tag { Id = tags.Count + 1, Name = t })
                        .ToList();

                    context.Tags.AddRange(missingTags);
                    context.SaveChanges();

                    tags.AddRange(missingTags);
                    return tags.Where(t => tagStrings.Contains(t.Name)).ToList();
                });

            A.CallTo(() => fakeTagService.ListTags())
                .ReturnsLazily(() => tags);

            return fakeTagService;
        }

        private void PopulateFakeData()
        {
            var repo = GetIngredientsRepository();

            var category1 = new IngredientCategory { Name = "Category 1", Order = 1 };
            var category2 = new IngredientCategory { Name = "Category 2", Order = 2 };
            repo.SaveIngredientCategory(category1);
            repo.SaveIngredientCategory(category2);

            repo.SaveIngredient(new Ingredient { Brief = "Ingredient 1", IngredientCategory = category1 });
            repo.SaveIngredient(new Ingredient { Brief = "Ingredient 2", IngredientCategory = category1 });
            repo.SaveIngredient(new Ingredient { Brief = "Ingredient 3", IngredientCategory = category2 });
        }

        private void PopulateFakeTags()
        {
            var context = new MealsDbContext(_options);
            context.Tags.AddRange(new List<Tag> {
                new Tag { Id = 1, Name = "tag1" },
                new Tag { Id = 2, Name = "tag2" }
            });
            context.SaveChanges();
        }

        [Test]
        public void CreateIngredientTest()
        {
            // Create the schema in the database
            var repo = GetIngredientsRepository();
            repo.SaveIngredient(new Ingredient { Brief = "Test Ingredient" });

            repo = GetIngredientsRepository();
            var ingredients = repo.ListIngredients();
            ingredients.Count.Should().Be(1);
            ingredients[0].Brief.Should().Be("Test Ingredient");
        }

        [Test]
        public void EditIngredientTest()
        {
            PopulateFakeData();

            var repo = GetIngredientsRepository();
            var ingredients = repo.ListIngredients();
            ingredients.Count.Should().Be(3);

            ingredients[0].Brief = "Updated Ingredient";
            repo.SaveIngredient(ingredients[0]);

            repo = GetIngredientsRepository();
            var updatedIngredients = repo.ListIngredients();
            updatedIngredients[0].Brief.Should().Be("Updated Ingredient");
        }

        [Test]
        public void AddIngredientNewTagTest()
        {
            PopulateFakeData();

            var repo = GetIngredientsRepository();
            var ingredients = repo.ListIngredients();

            repo.SetTags(ingredients[0].Id, new List<string> { "new tag" });

            repo = GetIngredientsRepository();
            var updatedIngredients = repo.ListIngredients();

            updatedIngredients[0].IngredientTags.Count.Should().Be(1);
            updatedIngredients[0].IngredientTags[0].Tag.Name.Should().Be("new tag");
        }

        [Test]
        public void AddIngredientExistingTagTest()
        {
            PopulateFakeData();
            PopulateFakeTags();

            var repo = GetIngredientsRepository();
            var ingredients = repo.ListIngredients();

            repo.SetTags(ingredients[0].Id, new List<string> { "tag1" });

            repo = GetIngredientsRepository();
            var updatedIngredients = repo.ListIngredients();

            updatedIngredients[0].IngredientTags.Count.Should().Be(1);
            updatedIngredients[0].IngredientTags[0].Tag.Name.Should().Be("tag1");
        }

        [Test]
        public void ListIngredientsTest()
        {
            PopulateFakeData();

            var repo = GetIngredientsRepository();
            var ingredients = repo.ListIngredients();
            ingredients.Count.Should().Be(3);

            ingredients[0].IngredientCategory.Id.Should().Be(1);
            ingredients[2].IngredientCategory.Id.Should().Be(2);
        }

        [Test]
        public void DeleteIngredientTest()
        {
            PopulateFakeData();

            var repo = GetIngredientsRepository();
            var ingredients = repo.ListIngredients();

            ingredients.Count.Should().Be(3);

            repo.DeleteIngredientById(ingredients[0].Id);

            var updatedRepo = GetIngredientsRepository();
            var updatedIngredients = updatedRepo.ListIngredients();

            updatedIngredients.Count.Should().Be(2);
        }

        [Test]
        public void DeleteIngredientWithTagsTest()
        {
            PopulateFakeData();
            PopulateFakeTags();

            var repo = GetIngredientsRepository();
            var ingredients = repo.ListIngredients();

            ingredients.Count.Should().Be(3);

            repo.SetTags(ingredients[0].Id, new List<string> { "tag1" });

            var context = new MealsDbContext(_options);
            context.IngredientTags.ToList().Count.Should().Be(1);

            repo.DeleteIngredientById(ingredients[0].Id);

            var updatedRepo = GetIngredientsRepository();
            var updatedIngredients = updatedRepo.ListIngredients();

            updatedIngredients.Count.Should().Be(2);

            context = new MealsDbContext(_options);
            context.IngredientTags.ToList().Count.Should().Be(0);
        }

        [Test]
        public void DeleteIngredientCategoryWithIngredientsTest()
        {
            PopulateFakeData();

            var repo = GetIngredientsRepository();
            var categories = repo.ListIngredientCategories();
            var ingredients = repo.ListIngredients();

            categories.Count.Should().Be(2);

            var category = categories.First();
            var catIngredients = ingredients.Where(i => i.IngredientCategory.Id == category.Id).ToList();

            catIngredients.Should().NotBeEmpty();

            repo.DeleteIngredientCategoryById(category.Id);
            
            foreach (var ing in catIngredients)
            {
                ing.CategoryId.Should().BeNull();
                repo.DeleteIngredientById(ing.Id);
            }

            var updatedRepo = GetIngredientsRepository();
            var updatedCategories = updatedRepo.ListIngredientCategories();

            updatedCategories.Count.Should().Be(1);
        }
    }
}