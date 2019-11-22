using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enyim.Caching;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Ingredients;
using MealsService.Recipes.Data;
using MealsService.Recipes.Dtos;
using MealsService.Requests;
using MealsService.Users;
using MealsService.Users.Data;

namespace MealsService.Recipes
{
    public class UserRecipesService : IUserRecipesService
    {
        private const int JOURNEY_FAVORITES_ID = 3;

        private const int VOTES_CACHE_TTL_SECONDS = 15 * 60;
        private const int RECENT_RECIPES_CACHE_TTL_SECONDS = 14 * 24 * 60 * 60;

        private const int RECENT_RECIPES_COUNT = 50;

        private UsersService _usersService;

        private IUserRecipeRepository _userRecipesRepo;
        private IMemcachedClient _memcache;
        private IRecipesService _recipeService;
        private IIngredientsService _ingredientsService;

        public UserRecipesService(IIngredientsService ingredientsService, IRecipesService recipeService, IUserRecipeRepository userRecipesRepo,
            IMemcachedClient memcachedClient, UsersService usersService)
        {
            _ingredientsService = ingredientsService;
            _recipeService = recipeService;
            _userRecipesRepo = userRecipesRepo;
            _memcache = memcachedClient;

            _usersService = usersService;
        }

        public async Task<List<RecipeVoteDto>> ListRecipeVotesAsync(int userId)
        {
            return await _memcache.GetValueOrCreateAsync(CacheKeys.Recipes.UserVotes(userId), VOTES_CACHE_TTL_SECONDS, () =>
            {
                var votes = _userRecipesRepo.GetUserVotes(userId);

                return Task.FromResult(votes.Select(v => new RecipeVoteDto
                {
                    RecipeId = v.RecipeId,
                    Vote = v.Vote
                }).ToList());
            });
        }

        public async Task<bool> AddRecipeVoteAsync(int userId, int recipeId, RecipeVote.VoteType vote)
        {
            var existingVote = _userRecipesRepo.GetUserVotes(userId)
                .FirstOrDefault(rv => rv.RecipeId == recipeId);

            if (existingVote == null)
            {
                existingVote = new RecipeVote { UserId = userId, RecipeId = recipeId };
            }

            existingVote.Vote = vote;

            var success = _userRecipesRepo.SaveVote(existingVote);
            ClearVoteCache(userId);

            if (!success)
            {
                throw RecipeErrors.RecipeVoteFailed;
            }

            await CheckForJourneyProgressOnVotesAsync(userId);

            var response = new RecipeVoteResponse
            {
                RecipeId = recipeId,
                Vote = vote,
            };

            return success;
        }

        public async Task<bool> CheckForJourneyProgressOnVotesAsync(int userId)
        {
            var likes = (await ListRecipeVotesAsync(userId))
                .Count(v => v.Vote == RecipeVote.VoteType.LIKE);

            if (likes <= 1)
            {
                var updateRequest = new UpdateJourneyProgressRequest
                {
                    JourneyStepId = JOURNEY_FAVORITES_ID,
                    Completed = likes == 1
                };
                return await _usersService.UpdateJourneyProgressAsync(userId, updateRequest);
            }

            return false;
        }

        public async Task<bool> PopulateRecipeVotesAsync(List<RecipeDto> recipes, int userId)
        {
            var votes = (await ListRecipeVotesAsync(userId))
                .ToDictionary(v => v.RecipeId, v => v);

            foreach (var recipe in recipes)
            {
                if (votes.ContainsKey(recipe.Id))
                {
                    recipe.Vote = votes[recipe.Id].Vote;
                }
            }

            return true;
        }

        public async Task<List<int>> GetRecentRecipeIds(int userId)
        {
            return await _memcache.GetValueOrCreateAsync(CacheKeys.Recipes.RecentGenerations(userId),
                RECENT_RECIPES_CACHE_TTL_SECONDS, () => Task.FromResult(new List<int>()));
        }

        public async Task TrackRecentRecipeId(int userId, int recipeId)
        {
            var recentIds = await GetRecentRecipeIds(userId);
            recentIds.Insert(0, recipeId);

            await _memcache.SetAsync(CacheKeys.Recipes.RecentGenerations(userId), recentIds.Take(RECENT_RECIPES_COUNT),
                RECENT_RECIPES_CACHE_TTL_SECONDS);
        }

        public async Task ClearRecentRecipeIds(int userId)
        {
            await _memcache.RemoveAsync(CacheKeys.Recipes.RecentGenerations(userId));
        }

        public async Task<RecipeDto> GetRandomRecipeAsync(RandomRecipeRequest request, int userId = 0, bool retry = false)
        {
            request.ExcludeTags = request.ExcludeTags?.Select(g => g.ToLower()).ToList();

            //TODO: Add tags to recipes, specific to the recipe, but also pulled up from the ingredients
            //TODO: Refactor to use recipe's tags instead of ingredients
            //TODO: Don't count optional ingredients, once there is such a thing
            var excludedIngredientIds = new List<int>();

            if (request.ExcludeTags != null && request.ExcludeTags.Any())
            {
                excludedIngredientIds = _ingredientsService.GetIngredientsByTags(request.ExcludeTags)
                    .Select(i => i.Id)
                    .ToList();
            }

            var recentPulls = await GetRecentRecipeIds(userId);
            var consumeIngredientIds = request.ConsumeIngredients != null ? request.ConsumeIngredients.Select(ri => ri.MeasuredIngredient.IngredientId).ToList() : new List<int>();

            var sortedRecipes = (_recipeService.SearchRecipes(new RecipeSearchRequest { MealType = MealType.Dinner }))
                //TODO: Fix
                .Where(m => (request.DietTypeId == 0 || m.DietTypes.Contains(request.DietTypeId)))
                //Exclude any recipes that have ingredients that were requested to be excluded
                .Where(m => m.Ingredients.All(mi => !excludedIngredientIds.Contains(mi.MeasuredIngredient.IngredientId)))
                .Where(r => !request.ExcludeRecipes.Contains(r.Id) && !recentPulls.Contains(r.Id))
                //Sort recipes that have the requested ingredients to the top
                .OrderByDescending(m => m.Ingredients.Count(mi => consumeIngredientIds.Contains(mi.MeasuredIngredient.IngredientId)))
                .ThenByDescending(r => r.Priority);
            //Preference the recipes that haven't been used yet
            //.ThenBy(m => recipeWeights != null && recipeWeights.ContainsKey(m.Id) ? recipeWeights[m.Id] : 0);

            var rand = new Random();
            var countDiff = 0; //sortedRecipes.Count() - sortedRecipes.Count(r => recipeWeights != null && recipeWeights.ContainsKey(r.Id));
            var index = countDiff > 0 ? rand.Next(countDiff) : rand.Next(sortedRecipes.Count());

            var recipe = sortedRecipes.Skip(index).FirstOrDefault();

            if (recipe != null)
            {
                await TrackRecentRecipeId(userId, recipe.Id);
            }
            else if (!retry)
            {
                await ClearRecentRecipeIds(userId);
                return await GetRandomRecipeAsync(request, userId, true);
            }
            //TODO: Add logger
            /*else
            {
                throw new Exception("No recipes for type " + request.MealType);
            }*/

            return recipe;
        }

        private void ClearVoteCache(int userId)
        {
            _memcache.Remove(CacheKeys.Recipes.UserVotes(userId));
        }
    }
}
