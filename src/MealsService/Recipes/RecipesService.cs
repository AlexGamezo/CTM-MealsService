using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Ingredients;
using MealsService.Requests;
using MealsService.Recipes.Dtos;
using MealsService.Recipes.Data;

namespace MealsService.Recipes
{
    public class RecipesService : IRecipesService
    {
        private IRecipeRepository _repository;

        private IMemoryCache _localCache;
        private IIngredientsService _ingredientsService;

        private const int RECIPE_CACHE_TTL_SECONDS = 15 * 60;
        
        public RecipesService(IRecipeRepository recipeRepo, IIngredientsService ingredientsService, IMemoryCache memoryCache)
        {
            _ingredientsService = ingredientsService;

            _repository = recipeRepo;
            _localCache = memoryCache;
        }

        public List<RecipeDto> ListRecipes(RecipeListRequest request = null)
        {
            var dtos = ListRecipesInternal()
                .Where(r => request?.RecipeIds == null || !request.RecipeIds.Any() || request.RecipeIds.Contains(r.Id))
                .Select(r => r.ToDto())
                .ToList();

            dtos.ForEach(NormalizeIngredientsForRecipe);

            return dtos;
        }

        private void NormalizeIngredientsForRecipe(RecipeDto dto)
        {
            var measuredIngredients = dto.Ingredients?.Select(i => i.MeasuredIngredient).ToList();
            measuredIngredients?.ForEach(_ingredientsService.NormalizeMeasuredIngredient);
        }

        public List<RecipeDto> SearchRecipes(RecipeSearchRequest request)
        {
            IEnumerable<Recipe> recipes = null;

            if (request.IncludeDeleted)
            {
                recipes = ListRecipesWithDeleted();
            }
            else
            {
                recipes = ListRecipesInternal();
            }

            if (!string.IsNullOrEmpty(request.Search))
            {
                recipes = recipes.Where(m => m.Name.Contains(request.Search));
            }
            if (request.IngredientIds != null && request.IngredientIds.Any())
            {
                var ingIds = request.IngredientIds;

                recipes = recipes.Where(m =>
                {
                    var matchedIngredients = m.RecipeIngredients.Count(mi => ingIds.Contains(mi.IngredientId));
                    return request.AllIngredients ? matchedIngredients == ingIds.Count : matchedIngredients > 0;
                });
            }
            if (request.MealType != MealType.Any)
            {
                recipes = recipes.Where(m => m.MealType == request.MealType);
            }
            
            var dtos = recipes.Select(r => r.ToDto()).ToList();
            dtos.ForEach(NormalizeIngredientsForRecipe);

            return dtos;
        }

        public RecipeDto GetRecipe(int id)
        {
            return GetRecipes(new[] {id}).FirstOrDefault();
        }
        
        public RecipeDto GetRecipeBySlug(string slug)
        {
            return FindRecipesBySlug(new[] { slug })
                .Select(r => r.ToDto()).FirstOrDefault();
        }

        public Recipe FindRecipeBySlug(string slug)
        {
            return FindRecipesBySlug(new[] {slug}).FirstOrDefault();
        }

        public List<Recipe> FindRecipesBySlug(IEnumerable<string> slugs)
        {
            return ListRecipesInternal().Where(r => slugs.Contains(r.Slug)).ToList();
        }

        public List<RecipeDto> GetRecipes(IEnumerable<int> ids)
        {
            var dtos = FindRecipesById(ids).Select(r => r.ToDto()).ToList();
            dtos.ForEach(NormalizeIngredientsForRecipe);

            return dtos;
        }
        
        public Recipe FindRecipeById(int id)
        {
            return FindRecipesById(new List<int> {id}).FirstOrDefault();
        }

        public List<Recipe> FindRecipesById(IEnumerable<int> ids)
        {
            var recipes = ListRecipesInternal().AsEnumerable();

            return recipes.Where(m => ids.Contains(m.Id)).ToList();
        }

        public RecipeDto SaveRecipe(RecipeDto recipeDto)
        {
            var measuredIngredients = recipeDto.Ingredients.Select(i => i.MeasuredIngredient).ToList();
            measuredIngredients.ForEach(_ingredientsService.DenormalizeMeasuredIngredient);
            
            var recipe = recipeDto.FromDto();

            if (string.IsNullOrEmpty(recipe.Slug))
            {
                recipe.Slug = GenerateSlug(recipe.Name, recipeDto.Id);
            }

            var recipeIngredients = recipeDto.Ingredients?.Select(i => i.FromDto()).ToList();
            
            if (_repository.SaveRecipe(recipe) &&
                _repository.SetDietTypes(recipe.Id, recipeDto.DietTypes) &&
                _repository.SetRecipeIngredients(recipe.Id, recipeIngredients) &&
                _repository.SetRecipeSteps(recipe.Id, recipeDto.Steps))
            {
                var dto = recipe.ToDto();
                NormalizeIngredientsForRecipe(dto);

                _localCache.Remove(CacheKeys.Recipes.AllRecipes);

                return dto;
            }

            return null;
        }

        private string GenerateSlug(string name, int id)
        {
            var regex = new Regex("[^A-Za-z0-9]+");
            var baseSlug = regex.Replace(name.ToLower(), "-");

            for (var i = 0; i < 10; i++)
            {
                string testSlug;
                if (i == 0)
                {
                    testSlug = baseSlug;
                }
                else
                {
                    testSlug = $"baseSlug-{i}";
                }

                if (FindRecipesBySlug(new List<string>{testSlug}).All(r => id != r.Id))
                {
                    return testSlug;
                }
            }

            throw RecipeErrors.FailedToGenerateSlug;
        }

        private List<Recipe> ListRecipesInternal()
        {
            var recipes = _localCache.GetOrCreate(CacheKeys.Recipes.AllRecipes, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(RECIPE_CACHE_TTL_SECONDS);
                return _repository.ListRecipes();
            });

            return recipes;
        }

        private List<Recipe> ListRecipesWithDeleted()
        {
            return _repository.ListRecipesWithDeleted();
        }

        public bool DeleteRecipe(int id)
        {
            if (_repository.DeleteRecipe(id))
            {
                _localCache.Remove(CacheKeys.Recipes.AllRecipes);

                return true;
            }

            return false;
        }
    }
}
