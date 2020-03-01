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

namespace MealsService.Tests.Ingredients
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

            var fakeTagService = A.Fake<ITagsService>();

            A.CallTo(() => fakeTagService.GetOrCreateTags(A<IEnumerable<string>>.Ignored))
                .ReturnsLazily((IEnumerable<string> tagStrings) =>
                {
                    var missingTags = tagStrings
                        .Where(t => context.Tags.All(tag => tag.Name != t))
                        .Select(t => new Tag { Name = t })
                        .ToList();

                    context.Tags.AddRange(missingTags);
                    context.SaveChanges();

                    context.Tags.AddRange(missingTags);
                    return context.Tags.Where(t => tagStrings.Contains(t.Name)).ToList();
                });

            A.CallTo(() => fakeTagService.ListTags())
                .ReturnsLazily(() => context.Tags.ToList());

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
            GetTagsService().GetOrCreateTags(new List<string> {"tag1", "tag2", "tag3"});
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
        public void EditIngredientCategoryTest()
        {
            PopulateFakeData();

            var repo = GetIngredientsRepository();
            var categories = repo.ListIngredientCategories();
            categories.Count.Should().Be(2);

            categories[0].Name = "Updated Category";
            repo.SaveIngredientCategory(categories[0]);

            repo = GetIngredientsRepository();
            var updatedCategories = repo.ListIngredientCategories();
            updatedCategories[0].Name.Should().Be("Updated Category");
        }

        [Test]
        public void AddIngredientNewTagTest()
        {
            PopulateFakeData();

            var repo = GetIngredientsRepository();
            var ingredients = repo.ListIngredients();

            var status = repo.SetTags(ingredients[0].Id, new List<string> { "new tag" });

            status.Should().BeTrue();

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

            var status = repo.SetTags(ingredients[0].Id, new List<string> { "tag1" });

            status.Should().BeTrue();

            repo = GetIngredientsRepository();
            var updatedIngredients = repo.ListIngredients();

            updatedIngredients[0].IngredientTags.Count.Should().Be(1);
            updatedIngredients[0].IngredientTags[0].Tag.Name.Should().Be("tag1");
        }

        [Test]
        public void UpdateIngredientTagsTest()
        {
            PopulateFakeData();
            PopulateFakeTags();

            var repo = GetIngredientsRepository();
            var ingredients = repo.ListIngredients();

            repo.SetTags(ingredients[0].Id, new List<string> { "tag1" });

            var updatedIngredients = repo.ListIngredients();

            updatedIngredients[0].IngredientTags.Count.Should().Be(1);
            updatedIngredients[0].IngredientTags[0].Id.Should().Be(1);
            updatedIngredients[0].IngredientTags[0].Tag.Name.Should().Be("tag1");

            var status = repo.SetTags(ingredients[0].Id, new List<string>{"tag2", "tag3"});
            status.Should().BeTrue();

            repo = GetIngredientsRepository();
            var modifiedTagsIngredients = repo.ListIngredients();

            var modifiedIng = modifiedTagsIngredients[0];

            modifiedIng.Id.Should().Be(updatedIngredients[0].Id);
            modifiedIng.Tags.Should().BeEquivalentTo(new List<string> {"tag2", "tag3"});
            
            modifiedIng.IngredientTags[0].Id.Should().Be(updatedIngredients[0].IngredientTags[0].Id);
            modifiedIng.IngredientTags[1].Id.Should().Be(2);

            status = repo.SetTags(ingredients[0].Id, new List<string> { "tag4" });
            status.Should().BeTrue();

            repo = GetIngredientsRepository();
            modifiedTagsIngredients = repo.ListIngredients();

            modifiedIng = modifiedTagsIngredients[0];

            modifiedIng.Id.Should().Be(updatedIngredients[0].Id);
            modifiedIng.Tags.Should().BeEquivalentTo(new List<string> { "tag4" });

            modifiedIng.IngredientTags.Count.Should().Be(1);
            modifiedIng.IngredientTags[0].Id.Should().Be(updatedIngredients[0].IngredientTags[0].Id);

            var context = new MealsDbContext(_options);
            context.IngredientTags.ToList().Count.Should().Be(1);
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