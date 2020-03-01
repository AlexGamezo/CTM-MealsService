using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

using NUnit.Framework;
using FakeItEasy;
using FluentAssertions;

using MealsService.Common.Errors;
using MealsService.Diets.Data;
using MealsService.Ingredients;
using MealsService.Ingredients.Data;
using MealsService.Recipes;
using MealsService.Recipes.Data;
using MealsService.Recipes.Dtos;
using MealsService.Requests;

namespace MealsService.Tests.Recipes
{
    public class RecipesServiceTests
    {
        private IRecipesService _recipesService;
        private IRecipeRepository _fakeRepo;
        private IMemoryCache _fakeCache;
        private IIngredientsService _fakeIngredientsService;

        [SetUp]
        public void Setup()
        {
            _fakeCache = A.Fake<IMemoryCache>();
            _fakeRepo = GetFakeRecipeRepository();
            _fakeIngredientsService = GetFakeIngredientsService();
            
            _recipesService = new RecipesService(_fakeRepo, _fakeIngredientsService, _fakeCache);
        }

        private IIngredientsService GetFakeIngredientsService()
        {
            var fakeService = A.Fake<IIngredientsService>();

            return fakeService;
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

            A.CallTo(() => fakeRepo.SetRecipeIngredients(A<int>.Ignored, A<List<RecipeIngredient>>.Ignored))
                .ReturnsLazily((int recipeId, List<RecipeIngredient> ingredients) =>
                {
                    var existingRecipe = recipes.FirstOrDefault(r => r.Id == recipeId);
                    if (existingRecipe == null)
                    {
                        throw StandardErrors.MissingRequestedItem;
                    }

                    for(var index = 0; index < ingredients.Count; index++)
                    {
                        var model = ingredients[index];
                        model.RecipeId = recipeId;
                        model.Recipe = existingRecipe;

                        if (model.Id == 0)
                        {
                            model.Id = recipeId * 100 + index + 1;
                        }
                    }

                    existingRecipe.RecipeIngredients = ingredients;

                    return true;
                });


            A.CallTo(() => fakeRepo.SetDietTypes(A<int>.Ignored, A<List<int>>.Ignored))
                .ReturnsLazily((int recipeId, List<int> dietTypes) =>
                {
                    var existingRecipe = recipes.FirstOrDefault(r => r.Id == recipeId);
                    if (existingRecipe == null)
                    {
                        throw StandardErrors.MissingRequestedItem;
                    }

                    existingRecipe.RecipeDietTypes = new List<RecipeDietType>();
                    for (var index = 0; index < dietTypes.Count; index++)
                    {
                        var dietType = new RecipeDietType
                        {
                            DietTypeId = dietTypes[index],
                            RecipeId = recipeId,
                            Recipe = existingRecipe,
                            Id = recipeId * 100 + index + 1
                        };
                        existingRecipe.RecipeDietTypes.Add(dietType);
                    }

                    return true;
                });

            A.CallTo(() => fakeRepo.SetRecipeSteps(A<int>.Ignored, A<List<RecipeStep>>.Ignored))
                .ReturnsLazily((int recipeId, List<RecipeStep> steps) =>
                {
                    var existingRecipe = recipes.FirstOrDefault(r => r.Id == recipeId);
                    if (existingRecipe == null)
                    {
                        throw StandardErrors.MissingRequestedItem;
                    }

                    for (var index = 0; index < steps.Count; index++)
                    {
                        var model = steps[index];
                        model.RecipeId = recipeId;
                        model.Recipe = existingRecipe;

                        if (model.Id == 0)
                        {
                            model.Id = recipeId * 100 + index + 1;
                        }
                    }

                    existingRecipe.Steps = steps;

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
                            Ingredient = new Ingredient {Id = 1, Name = "Tomatoes", IsMeasuredVolume = false},
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
        public void GetRecipeByIdTest()
        {
            var foundRecipe = _recipesService.GetRecipe(1);
            foundRecipe.Should().NotBeNull();
            foundRecipe.Id.Should().Be(1);
        }

        [Test]
        public void GetRecipeBySlugTest()
        {
            var foundRecipe = _recipesService.GetRecipeBySlug("recipe-1");
            foundRecipe.Should().NotBeNull();
            foundRecipe.Id.Should().Be(1);
            foundRecipe.Slug.Should().Be("recipe-1");
        }

        [Test]
        public void CreateNewRecipeTest()
        {
            var recipeDto = new RecipeDto
            {
                Name = "Test Recipe",
                DietTypes = new List<int> {1, 2},
                Ingredients = new List<RecipeIngredientDto>
                {
                    new RecipeIngredientDto
                        {MeasuredIngredient = new MeasuredIngredient {IngredientId = 1, Measure = "cups", Quantity = 2.5}}
                },
                MealType = MealType.Dinner,
                NumServings = 1,
                Steps = new List<RecipeStep> {new RecipeStep {Text = "Step 1", Order = 1}}
            };

            var result = _recipesService.SaveRecipe(recipeDto);

            result.Id.Should().NotBe(0);
            result.Ingredients.Should().NotBeNullOrEmpty();
            result.Ingredients[0].Id.Should().NotBe(0);
        }

        [Test]
        public void UpdateExistingRecipeTest()
        {
            var recipeDto = new RecipeDto
            {
                Id = 1,
                Name = "Updated Recipe",
                DietTypes = new List<int> { 1, 2 },
                Ingredients = new List<RecipeIngredientDto>
                {
                    new RecipeIngredientDto
                        {MeasuredIngredient = new MeasuredIngredient {IngredientId = 1, Measure = "cups", Quantity = 2.5}}
                },
                MealType = MealType.Dinner,
                NumServings = 1,
                Steps = new List<RecipeStep> { new RecipeStep { Text = "Step 1", Order = 1 } }
            };
            var updatedDto = _recipesService.SaveRecipe(recipeDto);

            updatedDto.Id.Should().Be(1);
            updatedDto.Name.Should().Be(recipeDto.Name);
        }

        [Test]
        public void DeleteExistingRecipeTest()
        {
            Action act = () =>
            {
                var result = _recipesService.DeleteRecipe(1);
                result.Should().BeTrue();
            };

            act.Should().NotThrow();
            
        }

        [Test]
        public void DeleteMissingRecipeShouldFailTest()
        {
            Action act = () => _recipesService.DeleteRecipe(5);

            act.Should().Throw<Exception>()
                .Where(e => e.InnerException is ServiceException && ((ServiceException)e.InnerException).ErrorCode == StandardErrors.MissingRequestedItem.ErrorCode);
        }
    }
}
