using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

using NUnit.Framework;
using FakeItEasy;
using FluentAssertions;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Ingredients;
using MealsService.Ingredients.Data;
using MealsService.Ingredients.Dtos;
using MealsService.Tags.Data;

namespace MealsService.Tests
{
    public class IngredientsServiceTests
    {
        private IngredientsService _ingredientsService;

        private IMemoryCache _memoryCache;
        private IIngredientsRepository _fakeRepository;

        [SetUp]
        public void Setup()
        {
            _memoryCache = A.Fake<IMemoryCache>();
            _fakeRepository = GetIngredientsRepository();

            _ingredientsService = new IngredientsService(_memoryCache, _fakeRepository);
        }

        private IIngredientsRepository GetIngredientsRepository()
        {
            var fakeRepository = A.Fake<IIngredientsRepository>();

            var ingredients = new Dictionary<int, Ingredient>();
            var ingredientCategories = new Dictionary<int, IngredientCategory>
            {
                {1, new IngredientCategory{ Id = 1, Name = "category 1", Order = 1}},
                {2, new IngredientCategory{ Id = 2, Name = "category 2", Order = 2}}
            };

            A.CallTo(() => fakeRepository.SaveIngredient(A<Ingredient>.Ignored))
                .ReturnsLazily((Ingredient i) =>
                {
                    if (i.Id == 0)
                    {
                        i.Id = ingredients.Count + 1;
                    }

                    if (!ingredients.ContainsKey(i.Id))
                    {
                        ingredients.Add(i.Id, i);
                    }
                    else
                    {
                        ingredients[i.Id] = i;
                    }
                    
                    return true;
                });
            A.CallTo(() => fakeRepository.SaveIngredientCategory(A<IngredientCategory>.Ignored))
                .ReturnsLazily((IngredientCategory i) =>
                {
                    if (i.Id == 0)
                    {
                        i.Id = ingredientCategories.Count + 1;
                    }

                    if (!ingredientCategories.ContainsKey(i.Id))
                    {
                        ingredientCategories.Add(i.Id, i);
                    }
                    else
                    {
                        ingredientCategories[i.Id] = i;
                    }

                    return true;
                });
            A.CallTo(() => fakeRepository.SetTags(A<int>.Ignored, A<List<string>>.Ignored))
                .ReturnsLazily((int ingredientId, List<string> tags) =>
                {
                    if (!ingredients.ContainsKey(ingredientId))
                    {
                        throw StandardErrors.MissingRequestedItem;
                    }

                    ingredients[ingredientId].IngredientTags = tags.Select(((tag, index) => new IngredientTag
                    {
                        Id = index, Ingredient = ingredients[ingredientId], IngredientId = ingredientId,
                        Tag = new Tag {Id = index, Name = tag}
                    })).ToList();

                    return true;
                });
            A.CallTo(() => fakeRepository.ListIngredients())
                .ReturnsLazily(() => ingredients.Values.ToList());

            A.CallTo(() => fakeRepository.ListIngredientCategories())
                .ReturnsLazily(() => ingredientCategories.Values.ToList());

            return fakeRepository;
        }

        [Test]
        public void CreateIngredientTest()
        {
            var ingredient = _ingredientsService.SaveIngredient(new IngredientDto
            {
                Name = "New Ingredient"
            });

            ingredient.Should().NotBeNull();
            ingredient.Id.Should().Be(1);
            ingredient.Name.Should().Be("new ingredient");

            A.CallTo(() => _memoryCache.Remove(CacheKeys.Ingredients.IngredientsList))
                .MustHaveHappened(1, Times.Exactly);
            A.CallTo(() => _fakeRepository.SaveIngredient(A<Ingredient>.Ignored))
                .MustHaveHappened(1, Times.Exactly);
        }

        [Test]
        public void CreateIngredientExistingCategoryTest()
        {
            var ingredient = _ingredientsService.SaveIngredient(new IngredientDto
            {
                Name = "New Ingredient",
                Category = "Category 1"
            });

            ingredient.Should().NotBeNull();
            ingredient.Id.Should().Be(1);
            ingredient.Name.Should().Be("new ingredient");

            A.CallTo(() => _memoryCache.Remove(CacheKeys.Ingredients.IngredientsList))
                .MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _memoryCache.Remove(CacheKeys.Ingredients.IngredientCategoriesList))
                .MustHaveHappened(0, Times.Exactly);
            A.CallTo(() => _fakeRepository.SaveIngredient(A<Ingredient>.Ignored))
                .MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _fakeRepository.SaveIngredientCategory(A<IngredientCategory>.Ignored))
                .MustHaveHappened(0, Times.Exactly);

            ingredient.CategoryId.Should().NotBeNull();
            ingredient.IngredientCategory.Should().NotBeNull();
            ingredient.IngredientCategory.Name.Should().Be("category 1");
        }

        [Test]
        public void CreateIngredientNewCategoryTest()
        {
            var ingredient = _ingredientsService.SaveIngredient(new IngredientDto
            {
                Name = "New Ingredient",
                Category = "New Category"
            });

            ingredient.Should().NotBeNull();
            ingredient.Id.Should().Be(1);
            ingredient.Name.Should().Be("new ingredient");

            A.CallTo(() => _memoryCache.Remove(CacheKeys.Ingredients.IngredientsList))
                .MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _memoryCache.Remove(CacheKeys.Ingredients.IngredientCategoriesList))
                .MustHaveHappened(1, Times.Exactly);

            A.CallTo(() => _fakeRepository.SaveIngredient(A<Ingredient>.Ignored))
                .MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _fakeRepository.SaveIngredientCategory(A<IngredientCategory>.Ignored))
                .MustHaveHappened(1, Times.Exactly);

            ingredient.CategoryId.Should().NotBeNull();
            ingredient.IngredientCategory.Should().NotBeNull();
            ingredient.IngredientCategory.Name.Should().Be("new category");
        }

        [Test]
        public void AddIngredientTagTest()
        {
            var ingredient = _ingredientsService.SaveIngredient(new IngredientDto
            {
                Name = "New Ingredient",
                Tags = new List<string> { "New Tag"}
            });

            ingredient.Should().NotBeNull();
            ingredient.Id.Should().Be(1);
            ingredient.Name.Should().Be("new ingredient");

            A.CallTo(() => _memoryCache.Remove(CacheKeys.Ingredients.IngredientsList))
                .MustHaveHappened(2, Times.Exactly);

            A.CallTo(() => _fakeRepository.SaveIngredient(A<Ingredient>.Ignored))
                .MustHaveHappened(1, Times.Exactly);
            A.CallTo(() => _fakeRepository.SetTags(A<int>.Ignored, A<List<string>>.Ignored))
                .MustHaveHappened(1, Times.Exactly);

            ingredient.Tags.Should().NotBeNullOrEmpty();
            ingredient.Tags.Count.Should().Be(1);
            ingredient.Tags[0].Should().Be("New Tag");
        }

        [Test]
        public void ListIngredientsTest()
        {
            var ingredients = _ingredientsService.ListIngredients();
            object ignored = null;

            A.CallTo(() => _fakeRepository.ListIngredients())
                .MustHaveHappened(1, Times.Exactly);
            A.CallTo(() => _memoryCache.TryGetValue(CacheKeys.Ingredients.IngredientsList, out ignored))
                .MustHaveHappened(1, Times.Exactly);
            A.CallTo(() => _memoryCache.CreateEntry(CacheKeys.Ingredients.IngredientsList))
                .MustHaveHappened(1, Times.Exactly);
        }

        [Test]
        public void GetIngredientTest()
        {
            IngredientDto ingredient = _ingredientsService.GetIngredient(1);
            object ignored = null;
            ingredient.Should().BeNull();

            A.CallTo(() => _memoryCache.TryGetValue(CacheKeys.Ingredients.IngredientsList, out ignored))
                .MustHaveHappened(1, Times.Exactly);
            A.CallTo(() => _memoryCache.CreateEntry(CacheKeys.Ingredients.IngredientsList))
                .MustHaveHappened(1, Times.Exactly);
            A.CallTo(() => _fakeRepository.ListIngredients())
                .MustHaveHappened(1, Times.Exactly);

            _ingredientsService.SaveIngredient(new IngredientDto
            {
                Name = "New Ingredient"
            });

            ingredient = _ingredientsService.GetIngredient(1);

            A.CallTo(() => _memoryCache.TryGetValue(CacheKeys.Ingredients.IngredientsList, out ignored))
                .MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _memoryCache.CreateEntry(CacheKeys.Ingredients.IngredientsList))
                .MustHaveHappened(2, Times.Exactly);
            A.CallTo(() => _fakeRepository.ListIngredients())
                .MustHaveHappened(2, Times.Exactly);

            ingredient.Should().NotBeNull();
            ingredient.Id.Should().Be(1);
        }

        [Test]
        public void SearchIngredientTest()
        {
            _ingredientsService.SaveIngredient(new IngredientDto
            {
                Name = "Pepper (black)"
            });
            _ingredientsService.SaveIngredient(new IngredientDto
            {
                Name = "Tomatoes (Cherry)"
            });
            _ingredientsService.SaveIngredient(new IngredientDto
            {
                Name = "Red Cherries"
            });
            _ingredientsService.SaveIngredient(new IngredientDto
            {
                Name = "Plum (Red)"
            });

            var ingredients = _ingredientsService.ListIngredients();
            ingredients.Count.Should().Be(4);

            var redIngredients = _ingredientsService.SearchIngredients("Red");
            redIngredients.Should().NotBeNullOrEmpty();
            redIngredients.Count.Should().Be(2);
            redIngredients.Should().Contain((i => i.Name == "red cherries"));
            redIngredients.Should().Contain((i => i.Name == "plum (red)"));
            redIngredients.Should().NotContain((i => i.Name == "tomatoes (cherry)"));
            redIngredients.Should().NotContain((i => i.Name == "pepper (black)"));

            _ingredientsService.SearchIngredients("red").Count.Should().Be(redIngredients.Count);
        }
    }
}