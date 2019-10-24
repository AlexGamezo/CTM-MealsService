using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Requests;
using MealsService.Recipes.Dtos;
using MealsService.Recipes.Data;
using MealsService.Recipes.Strategies;

namespace MealsService.Recipes
{
    public interface IRecipesService
    {
        List<RecipeDto> ListRecipes(RecipeListRequest request = null);
        List<RecipeDto> SearchRecipes(RecipeSearchRequest request);
        RecipeDto GetRecipe(int recipeId);
        RecipeDto GetRecipeBySlug(string slug);

        List<Recipe> FindRecipesById(IEnumerable<int> ids);
        Recipe FindRecipeById(int id);
        List<Recipe> FindRecipesBySlug(IEnumerable<string> slugs);
        Recipe FindRecipeBySlug(string slug);
        RecipeDto SaveRecipe(RecipeDto recipe);
        bool DeleteRecipe(int recipeId);
    }

    public interface IUserRecipesService
    {
        Task<List<RecipeVoteDto>> ListRecipeVotesAsync(int userId);
        bool AddRecipeVote(int userId, int recipeId, RecipeVote.VoteType vote);
        Task<bool> PopulateRecipeVotesAsync(List<RecipeDto> recipes, int userId);
        Task<RecipeDto> GetRandomRecipeAsync(RandomRecipeRequest request, int userId, bool retry = false);
    }

    public class RecipesService : IRecipesService
    {
        /*private string _recipeImagesBucketName;
        private string _region;*/

        private IRecipeRepository _repository;

        private IMemoryCache _localCache;

        private const int RECIPE_CACHE_TTL_SECONDS = 15 * 60;
        
        public RecipesService(IRecipeRepository recipeRepo, IMemoryCache memoryCache)
        {
            _repository = recipeRepo;
            _localCache = memoryCache;

            /*_s3Client = s3Client;
            _recipeImagesBucketName = options.Value.RecipeImagesBucket;
            _region = options.Value.Region;*/
        }

        public List<RecipeDto> ListRecipes(RecipeListRequest request = null)
        {
            return ListRecipesInternal()
                .Select(r => r.ToDto())
                .ToList();
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
            
            return recipes.Select(r => r.ToDto()).ToList();
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
            return FindRecipesById(ids).Select(r => r.ToDto()).ToList();
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
            var recipe = recipeDto.FromDto();

            if (_repository.SaveRecipe(recipe))
            {
                return recipe.ToDto();
            }

            return recipe.ToDto();
        }

        public RecipeDto UpdateRecipe(int id, UpdateRecipeRequest request)
        {
            MealType recipeType;
            Enum.TryParse(request.MealType, out recipeType);
            Recipe recipe;

            if (id > 0)
            {
                recipe = FindRecipeById(id);
            }
            else
            {
                recipe = new Recipe();
            }

            recipe.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Slug))
            {
                recipe.Slug = request.Slug;
            }
            else if(string.IsNullOrEmpty(recipe.Slug))
            {
                recipe.Slug = GenerateSlug(request.Name, id);
            }

            recipe.Brief = request.Brief;
            recipe.Description = request.Description;
            recipe.CookTime = request.CookTime;
            recipe.PrepTime = request.PrepTime;
            recipe.NumServings = request.NumServings;
            recipe.Image = request.Image;
            recipe.MealType = recipeType;
            recipe.Source = request.Source;

            if (_repository.SaveRecipe(recipe) &&
                _repository.SetDietTypes(recipe.Id, request.DietTypeIds) &&
                _repository.SetRecipeIngredients(recipe.Id, request.Ingredients) &&
                _repository.SetRecipeSteps(recipe.Id, request.Steps))
            {
                return GetRecipe(recipe.Id);
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

            /*foreach (var slot in _dbContext.Meals.Where(s => s.RecipeId == id))
            {
                slot.RecipeId = 0;
            }*/
        }
    }
}
