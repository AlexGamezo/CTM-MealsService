using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

using NUnit.Framework;
using FakeItEasy;
using FluentAssertions;

using MealsService.Common.Errors;
using MealsService.Diets.Data;
using MealsService.Ingredients.Data;
using MealsService.Recipes;
using MealsService.Recipes.Data;
using MealsService.Recipes.Dtos;
using MealsService.Requests;

namespace MealsService.Tests
{
    public class RecipesServiceTests
    {
        private IRecipesService _recipesService;
        private IRecipeRepository _fakeRepo;
        private IMemoryCache _fakeCache;

        [SetUp]
        public void Setup()
        {
            _fakeCache = A.Fake<IMemoryCache>();
            _fakeRepo = GetFakeRecipeRepository();
            
            _recipesService = new RecipesService(_fakeRepo, _fakeCache);
        }

        private IRecipeRepository GetFakeRecipeRepository()
        {
            var fakeRepo = A.Fake<IRecipeRepository>();
            var recipes = GetFakeRecipesData();

            A.CallTo(() => fakeRepo.ListRecipes())
                .ReturnsLazily(() => recipes.Where(r => !r.Deleted).ToList());
            A.CallTo(() => fakeRepo.ListRecipesWithDeleted())
                .ReturnsLazily(() => recipes);

            A.CallTo(() => fakeRepo.SaveRecipe(A<Recipe>.Ignored))
                .ReturnsLazily((Recipe recipe) =>
                {
                    var existingRecipe = recipes.FirstOrDefault(r => r.Id == recipe.Id);
                    if (existingRecipe == null)
                    {
                        recipe.Id = recipes.Count + 1;
                    }
                    else
                    {
                        recipes.Remove(existingRecipe);
                    }

                    recipes.Add(recipe);
                    return true;
                });
            A.CallTo(() => fakeRepo.DeleteRecipe(A<int>.Ignored))
                .ReturnsLazily((int id) =>
                {
                    var existingRecipe = recipes.FirstOrDefault(r => r.Id == id);

                    if (existingRecipe != null)
                    {
                        recipes.Remove(existingRecipe);
                        return true;
                    }

                    throw StandardErrors.MissingRequestedItem;
                });

            A.CallTo(() => fakeRepo.SetRecipeIngredients(A<int>.Ignored, A<List<RecipeIngredientDto>>.Ignored))
                .ReturnsLazily((int recipeId, List<RecipeIngredientDto> ingredientDtos) =>
                {
                    var existingRecipe = recipes.FirstOrDefault(r => r.Id == recipeId);
                    if (existingRecipe == null)
                    {
                        throw StandardErrors.MissingRequestedItem;
                    }

                    var recipeIngredients = ingredientDtos.Select((i, index) =>
                    {
                        var model = i.FromDto();
                        model.RecipeId = recipeId;
                        model.Recipe = existingRecipe;

                        if (model.Id == 0)
                        {
                            model.Id = recipeId * 100 + index + 1;
                        }

                        return model;
                    }).ToList();

                    existingRecipe.RecipeIngredients = recipeIngredients;

                    return true;
                });

            return fakeRepo;
        }

        private List<Recipe> GetFakeRecipesData()
        {
            return new List<Recipe>
            {
                new Recipe
                {
                    Id = 1,
                    Name = "Recipe 1",
                    Brief = "Brief of Recipe 1",
                    Description = "Description of Recipe 1",
                    PrepTime = 15,
                    CookTime = 20,
                    MealType = MealType.Dinner,
                    Deleted = false,
                    Image = "http://images.greenerplate.com/recipes/1.jpg",
                    RecipeDietTypes = new List<RecipeDietType>
                    {
                        new RecipeDietType
                        {
                            Id = 1, DietTypeId = 1,
                            DietType = new DietType
                                {Id = 1, Name = "Vegan", Description = "Vegan Diet", ShortDescription = "Vegan Diet"}
                        }
                    },
                    RecipeIngredients = new List<RecipeIngredient>
                    {
                        new RecipeIngredient
                        {
                            Id = 1,
                            IngredientId = 1,
                            Ingredient = new Ingredient {Id = 1, Name = "Tomatoes", MeasurementType = "mass"},
                            Amount = 0.25f,
                            RecipeId = 1
                        }
                    },
                    Priority = 1,
                    Slug = "recipe-1",
                    Source = "fake-data",
                    Steps = new List<RecipeStep>
                    {
                        new RecipeStep{Id = 1, Order = 1, RecipeId = 1, Text = "Step 1"},
                        new RecipeStep{Id = 2, Order = 2, RecipeId = 1, Text = "Step 2"},
                    }
                }
            };
        }

        [Test]
        public void ListRecipesTest()
        {
            var recipes = _recipesService.ListRecipes();
            recipes.Should().NotBeNullOrEmpty();
            recipes.Count.Should().Be(1);

            recipes[0].Id.Should().BeGreaterThan(0);
            recipes[0].Vote.Should().Be(RecipeVote.VoteType.UNKNOWN);

            var listRequest = new RecipeListRequest
            {
                IncludeDeleted = false,
                Limit = 2,
                Offset = 0
            };

            recipes = _recipesService.ListRecipes(listRequest);
            recipes.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void SearchRecipesTest()
        {
            Assert.Fail();
        }

        [Test]
        public void GetRecipeByIdTest()
        {
            Assert.Fail();
        }

        [Test]
        public void GetRecipeBySlugTest()
        {
            Assert.Fail();
        }

        [Test]
        public void CreateNewRecipeTest()
        {
            Assert.Fail();
        }

        [Test]
        public void UpdateExistingRecipeTest()
        {
            Assert.Fail();
        }

        [Test]
        public void DeleteExistingRecipeTest()
        {
            Assert.Fail();
        }

        [Test]
        public void DeleteMissingRecipeShouldFailTest()
        {
            Assert.Fail();
        }
    }
}
