using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Enyim.Caching;
using MealsService.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using MealsService.Common.Errors;
using MealsService.Configurations;
using MealsService.Ingredients;
using MealsService.Ingredients.Data;
using MealsService.Requests;
using MealsService.Recipes.Dtos;
using MealsService.Recipes.Data;
using MealsService.Users;
using MealsService.Users.Data;

namespace MealsService.Recipes
{
    public class RecipesService
    {
        private string _recipeImagesBucketName;
        private string _region;

        private const int JOURNEY_FAVORITES_ID = 3;

        private IAmazonS3 _s3Client;

        private RecipeRepository _repository;
        private IngredientsService _ingredientsService;
        private IServiceProvider _serviceProvider;

        private IMemoryCache _localCache;
        private IMemcachedClient _memcache;

        private const int RECIPE_CACHE_TTL_SECONDS = 15 * 60;
        private const int VOTES_CACHE_TTL_SECONDS = 15 * 60;
        private const int RECENT_RECIPES_CACHE_TTL_SECONDS = 14 * 24 * 60 * 60;

        private const int RECENT_RECIPES_COUNT = 50;

        public RecipesService(IngredientsService ingredientsService, IAmazonS3 s3Client, IOptions<AWSConfiguration> options, IServiceProvider serviceProvider)
        {
            _repository = new RecipeRepository(serviceProvider);

            _serviceProvider = serviceProvider;
            _ingredientsService = ingredientsService;

            _localCache = _serviceProvider.GetService<IMemoryCache>();
            _memcache = _serviceProvider.GetService<IMemcachedClient>();

            _s3Client = s3Client;
            _recipeImagesBucketName = options.Value.RecipeImagesBucket;
            _region = options.Value.Region;
        }

        private List<Recipe> ListRecipesInternal(bool skipCache = false, bool includeDeleted = false)
        {
            if (skipCache || includeDeleted)
            {
                return _repository.ListRecipes(includeDeleted);
            }

            var recipes = _localCache.GetOrCreate(CacheKeys.Recipes.AllRecipes, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(RECIPE_CACHE_TTL_SECONDS);
                return _repository.ListRecipes();
            });

            return recipes;
        }

        public List<RecipeDto> ListRecipes(ListRecipesRequest request)
        {
            IEnumerable<Recipe> recipes = null;

            if (request.RecipeIds.Any())
            {
                recipes = FindRecipes(request.RecipeIds).AsEnumerable();
            }
            else
            {
                recipes = ListRecipesInternal(includeDeleted: request.IncludeDeleted).AsEnumerable();
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
            }

            return recipes.Select(ToRecipeDto).ToList();
        }

        public async Task<List<RecipeDto>> ListRecipesAsync(ListRecipesRequest request, int userId = 0)
        {
            var recipeDtos = ListRecipes(request);

            if (userId > 0)
            {
                await HydrateRecipeVoteAsync(recipeDtos, userId);
            }

            return recipeDtos;
        }

        private async Task HydrateRecipeVoteAsync(IEnumerable<RecipeDto> recipes, int userId)
        {
            var votes = (await GetVotesAsync(userId)).ToDictionary(v => v.RecipeId, v => v);

            foreach (var recipe in recipes)
            {
                if (votes.ContainsKey(recipe.Id))
                {
                    recipe.Vote = votes[recipe.Id].Vote;
                }
            }
        }

        /*public List<RecipeVote> GetVotes(int userId)
        {
            return _repository.GetUserVotes(userId);
        }*/
        
        public async Task<List<RecipeVoteDto>> GetVotesAsync(int userId, bool skipCache = false)
        {
            if (skipCache)
            {
                var votes = GetVotesInternal(userId);

                return votes.Select(v => new RecipeVoteDto
                {
                    RecipeId = v.RecipeId,
                    Vote = v.Vote
                }).ToList();
            }

            return await _memcache.GetValueOrCreateAsync(CacheKeys.Recipes.UserVotes(userId), VOTES_CACHE_TTL_SECONDS, () =>
            {
                var votes = GetVotesInternal(userId);

                return Task.FromResult(votes.Select(v => new RecipeVoteDto
                {
                    RecipeId = v.RecipeId,
                    Vote = v.Vote
                }).ToList());
            });
        }

        public RecipeDto GetRecipe(int id)
        {
            return GetRecipes(new[] {id}).FirstOrDefault();
        }
        public async Task<RecipeDto> GetRecipeAsync(int id, int userId = 0)
        {
            return (await GetRecipesAsync(new[] {id}, userId)).FirstOrDefault();
        }

        public RecipeDto GetBySlug(string slug)
        {
            return FindRecipesBySlug(new[] { slug })
                .Select(ToRecipeDto).FirstOrDefault();
        }

        public async Task<RecipeDto> GetBySlugAsync(string slug, int userId = 0)
        {
            var recipe = GetBySlug(slug);

            if (userId > 0)
            {
                await HydrateRecipeVoteAsync(new [] {recipe}, userId);
            }

            return recipe;
        }

        public List<Recipe> FindRecipesBySlug(IEnumerable<string> slugs)
        {
            return ListRecipesInternal().Where(r => slugs.Contains(r.Slug)).ToList();
        }

        public List<RecipeDto> GetRecipes(IEnumerable<int> ids)
        {
            return FindRecipes(ids).Select(ToRecipeDto).ToList();
        }

        public async Task<List<RecipeDto>> GetRecipesAsync(IEnumerable<int> ids, int userId = 0)
        {
            var recipes = GetRecipes(ids);
            
            if (userId > 0)
            {
                await HydrateRecipeVoteAsync(recipes, userId);
            }

            return recipes;
        }

        public Recipe FindRecipeById(int id)
        {
            return FindRecipes(new List<int> {id}).FirstOrDefault();
        }

        public List<Recipe> FindRecipes(IEnumerable<int> ids)
        {
            var recipes = ListRecipesInternal().AsEnumerable();

            return recipes.Where(m => ids.Contains(m.Id)).ToList();
        }

        public RecipeDto UpdateRecipe(int id, UpdateRecipeRequest request)
        {
            MealType recipeType;
            Enum.TryParse(request.MealType, out recipeType);
            var changes = false;

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
                _repository.SetDietTypes(recipe, request.DietTypeIds) &&
                _repository.SetRecipeIngredients(recipe, request.Ingredients) &&
                _repository.SetRecipeSteps(recipe, request.Steps))
            {
                return GetRecipe(recipe.Id);
            }

            return null;
        }

        //TODO: Extract "ImageService" to upload images, specifying the bucket
        public async Task<bool> UpdateRecipeImageAsync(int recipeId, IFormFile avatarFile)
        {
            var foundRecipe = FindRecipeById(recipeId);
            var extension = avatarFile.ContentType.Substring(avatarFile.ContentType.IndexOf("/") + 1);
            var avatarFilename = recipeId + "." + extension;
            var stream = new MemoryStream();

            if (foundRecipe == null)
            {
                return false;
            }

            avatarFile.CopyTo(stream);

            var response = await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _recipeImagesBucketName,
                InputStream = stream,
                Key = avatarFilename,
                CannedACL = S3CannedACL.PublicRead
            });

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                foundRecipe.Image = GetRecipeImageUrl(avatarFilename);
                
                return _repository.SaveRecipe(foundRecipe);
            }
            else
            {
                return false;
            }
        }

        public List<RecipeVote> GetVotesInternal(int userId)
        {
            return _repository.GetUserVotes(userId);
        }

        public async Task<RecipeVoteResponse> VoteAsync(int recipeId, int userId, RecipeVote.VoteType voteValue)
        {
            var vote = GetVotesInternal(userId).FirstOrDefault(rv => rv.RecipeId == recipeId);

            if (vote == null)
            {
                vote = new RecipeVote {UserId = userId, RecipeId = recipeId };
            }

            vote.Vote = voteValue;

            var success = _repository.SaveVote(vote);
            ClearVoteCache(userId);

            if (!success)
            {
                throw RecipeErrors.RecipeVoteFailed;
            }

            var likes = (await GetVotesAsync(userId)).Count(v => v.Vote == RecipeVote.VoteType.LIKE);

            var response = new RecipeVoteResponse
            {
                RecipeId = recipeId,
                Vote = voteValue,
            };

            if (likes <= 1)
            {
                var updateRequest = new UpdateJourneyProgressRequest
                {
                    JourneyStepId = JOURNEY_FAVORITES_ID,
                    Completed = likes == 1
                };
                await _serviceProvider.GetService<UsersService>().UpdateJourneyProgressAsync(userId, updateRequest);

                response.JourneyUpdated = true;
            }

            return response;
        }

        private string GetRecipeImageUrl(string filename)
        {
            return $"https://s3-{_region}.amazonaws.com/{_recipeImagesBucketName}/{filename}";
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

        public bool Remove(int id)
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

        public List<RecipeIngredientDto> ConvertMeasureType(List<RecipeIngredient> recipeIngredients, MeasureSystem system = MeasureSystem.IMPERIAL)
        {
            var dtos = new List<RecipeIngredientDto>();

            if (!recipeIngredients.Any())
            {
                return dtos;
            }

            return dtos;
        }

        public async Task<RecipeDto> GetRandomRecipeAsync(RandomRecipeRequest request, int userId = 0)
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
            var consumeIngredientIds = request.ConsumeIngredients != null ? request.ConsumeIngredients.Select(ri => ri.IngredientId).ToList() : new List<int>();

            var sortedRecipes = (await ListRecipesAsync(new ListRecipesRequest {MealType = MealType.Dinner}))
                //TODO: Fix
                .Where(m => (request.DietTypeId == 0 || m.DietTypes.Contains(request.DietTypeId)))
                //Exclude any recipes that have ingredients that were requested to be excluded
                .Where(m => m.Ingredients.All(mi => !excludedIngredientIds.Contains(mi.IngredientId)))
                .Where(r => !request.ExcludeRecipes.Contains(r.Id) && !recentPulls.Contains(r.Id))
                //Sort recipes that have the requested ingredients to the top
                .OrderByDescending(m => m.Ingredients.Count(mi => consumeIngredientIds.Contains(mi.IngredientId)))
                .ThenByDescending(r => r.Priority);
                //Preference the recipes that haven't been used yet
                //.ThenBy(m => recipeWeights != null && recipeWeights.ContainsKey(m.Id) ? recipeWeights[m.Id] : 0);

            var rand = new Random();
            var countDiff = 0; //sortedRecipes.Count() - sortedRecipes.Count(r => recipeWeights != null && recipeWeights.ContainsKey(r.Id));
            var index = countDiff > 0 ? rand.Next(countDiff) : rand.Next(sortedRecipes.Count());

            var recipe = sortedRecipes.Skip(index).FirstOrDefault();

            if(recipe != null)
            {
                await TrackRecentRecipeId(userId, recipe.Id);
            }

            //TODO: Add logger
            /*if (recipe == null)
            {
                throw new Exception("No recipes for type " + request.MealType);
            }*/

            return recipe;
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

        public RecipeDto ToRecipeDto(Recipe recipe)
        {
            if (recipe == null)
            {
                return null;
            }

            return new RecipeDto
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Brief = recipe.Brief,
                Description = recipe.Description,
                Image = recipe.Image,
                CookTime = recipe.CookTime,
                PrepTime = recipe.PrepTime,
                NumServings = recipe.NumServings,
                MealType = recipe.MealType,
                Source = recipe.Source,
                Ingredients = recipe.RecipeIngredients?.Select(ToRecipeIngredientDto)
                                    .OrderByDescending(i => !string.IsNullOrEmpty(i.Image))
                                    .ToList(),
                Steps = recipe.Steps
                            .OrderBy(s => s.Order)
                            .ToList(),
                DietTypes = recipe.RecipeDietTypes?.Select(mdt => mdt.DietTypeId).ToList(),
                Vote = recipe.Votes != null && recipe.Votes.Any() ? recipe.Votes.First().Vote : RecipeVote.VoteType.UNKNOWN,
                Slug = recipe.Slug,
                Priority = recipe.Priority,
                IsDeleted = recipe.Deleted,
            };
        }

        public RecipeIngredientDto ToRecipeIngredientDto(RecipeIngredient recipeIngredient)
        {
            if (recipeIngredient == null)
            {
                return null;
            }

            return new RecipeIngredientDto
            {
                Id = recipeIngredient.IngredientId,
                IngredientId = recipeIngredient.IngredientId,
                Quantity = recipeIngredient.Amount,
                Measure = recipeIngredient.MeasureType?.Name ?? recipeIngredient.AmountType,
                MeasureTypeId = recipeIngredient.MeasureTypeId,
                Name = recipeIngredient.Ingredient.Name,
                Image = recipeIngredient.Ingredient.Image,
                Category = recipeIngredient.Ingredient.Category
            };
        }

        private void ClearVoteCache(int userId)
        {
            _memcache.Remove(CacheKeys.Recipes.UserVotes(userId));
        }
    }
}
