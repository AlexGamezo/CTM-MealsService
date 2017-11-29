using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using MealsService.Configurations;
using MealsService.Requests;
using MealsService.Recipes.Dtos;
using MealsService.Recipes.Data;

namespace MealsService.Recipes
{
    public class RecipesService
    {
        private string _recipeImagesBucketName;
        private string _region;

        private MealsDbContext _dbContext;
        private IAmazonS3 _s3Client;

        public RecipesService(MealsDbContext dbContext, IAmazonS3 s3Client, IOptions<AWSConfiguration> options)
        {
            _dbContext = dbContext;
            _s3Client = s3Client;
            _recipeImagesBucketName = options.Value.RecipeImagesBucket;
            _region = options.Value.Region;
        }

        public List<RecipeDto> ListRecipes(ListRecipesRequest request)
        {
            IEnumerable<Meal> search = _dbContext.Meals
                .Include(m => m.MealIngredients)
                    .ThenInclude(mi => mi.Ingredient)
                    .ThenInclude(i => i.IngredientCategory)
                .Include(m => m.Steps)
                .Include(m => m.MealDietTypes);

            if (request.RecipeIds.Any())
            {
                search = search.Where(m => request.RecipeIds.Contains(m.Id));
            }
            else
            {
                if (!string.IsNullOrEmpty(request.Search))
                {
                    search = search.Where(m => m.Name.Contains(request.Search));
                }
                if (request.IngredientIds != null && request.IngredientIds.Any())
                {
                    var ingIds = request.IngredientIds;

                    search = search.Where(m =>
                    {
                        var matchedIngredients = m.MealIngredients.Count(mi => ingIds.Contains(mi.IngredientId));
                        return request.AllIngredients ? matchedIngredients == ingIds.Count : matchedIngredients > 0;
                    });
                }
                if (request.MealType != Meal.Type.Any)
                {
                    search = search.Where(m => m.MealType == request.MealType);
                }
            }

            return search.ToList().Select(ToRecipeDto).ToList();
        }

        public RecipeDto GetRecipe(int id, int userId = 0)
        {
            return GetRecipes(new[] {id}, userId)
                .FirstOrDefault(m => m.Id == id);
        }

        public List<RecipeDto> GetRecipes(IEnumerable<int> ids, int userId = 0)
        {
            var recipes = FindRecipes(ids, userId);

            return recipes.Select(ToRecipeDto).ToList();
        }

        public List<Meal> FindRecipes(IEnumerable<int> ids, int userId = 0)
        {
            var recipes = _dbContext.Meals
                .Include(m => m.MealIngredients)
                    .ThenInclude(mi => mi.Ingredient)
                        .ThenInclude(i => i.IngredientCategory)
                .Include(m => m.MealIngredients)
                    .ThenInclude(mi => mi.MeasureType)
                .Include(m => m.MealDietTypes)
                .Include(m => m.Steps)
                .Where(m => ids.Contains(m.Id))
                .ToList();

            if (userId > 0)
            {
                recipes.ForEach(recipe =>
                    _dbContext.Entry(recipe).Collection(r => r.Votes).Query().Where(v => v.UserId == userId).Load());
            }

            return recipes;
        }

        public RecipeDto UpdateRecipe(int id, UpdateRecipeRequest request)
        {
            Meal.Type mealType;
            Enum.TryParse(request.MealType, out mealType);
            var changes = false;

            Meal recipe;

            if (id > 0)
            {
                recipe = _dbContext.Meals
                .Include(m => m.MealIngredients)
                .Include(m => m.Steps)
                .Include(m => m.MealDietTypes)
                .FirstOrDefault(m => m.Id == id);
            }
            else
            {
                recipe = new Meal();
                _dbContext.Meals.Add(recipe);
            }

            recipe.Name = request.Name;
            recipe.Brief = request.Brief;
            recipe.Description = request.Description;
            recipe.CookTime = request.CookTime;
            recipe.PrepTime = request.PrepTime;
            recipe.NumServings = request.NumServings;
            recipe.Image = request.Image;
            recipe.MealType = mealType;
            recipe.Source = request.Source;

            for (var i = 0; i < request.DietTypeIds.Count; i++)
            {
                if (recipe.MealDietTypes?.Count > i)
                {
                    if (recipe.MealDietTypes[i].DietTypeId != request.DietTypeIds[i])
                    {
                        recipe.MealDietTypes[i].DietTypeId = request.DietTypeIds[i];
                        changes = true;
                    }
                }
                else
                {
                    changes = true;
                    if (recipe.MealDietTypes == null)
                    {
                        recipe.MealDietTypes = new List<MealDietType>();
                    }
                    recipe.MealDietTypes.Add(new MealDietType {DietTypeId = request.DietTypeIds[i]});
                }
            }
            if (request.DietTypeIds?.Count < recipe.MealDietTypes.Count)
            {
                var countToRemove = recipe.MealDietTypes.Count - request.DietTypeIds.Count;
                var toDelete = recipe.MealDietTypes.GetRange(request.DietTypeIds.Count, countToRemove);

                changes = true;
                _dbContext.MealDietTypes.RemoveRange(toDelete);
            }

            for (var i = 0; i < request.Ingredients.Count; i++)
            {
                if (recipe.MealIngredients?.Count > i)
                {
                    if (recipe.MealIngredients[i].IngredientId != request.Ingredients[i].IngredientId)
                    {
                        recipe.MealIngredients[i].IngredientId = request.Ingredients[i].IngredientId;
                        changes = true;
                    }
                    if (recipe.MealIngredients[i].Amount != request.Ingredients[i].Quantity)
                    {
                        recipe.MealIngredients[i].Amount = request.Ingredients[i].Quantity;
                        changes = true;
                    }
                    if (recipe.MealIngredients[i].AmountType != request.Ingredients[i].Measure)
                    {
                        recipe.MealIngredients[i].AmountType = request.Ingredients[i].Measure;
                        changes = true;
                    }
                    if (recipe.MealIngredients[i].MeasureTypeId != request.Ingredients[i].MeasureTypeId)
                    {
                        recipe.MealIngredients[i].MeasureTypeId = request.Ingredients[i].MeasureTypeId;
                        changes = true;
                    }
                }
                else
                {
                    changes = true;
                    if (recipe.MealIngredients == null)
                    {
                        recipe.MealIngredients = new List<MealIngredient>();
                    }
                    recipe.MealIngredients.Add(new MealIngredient
                    {
                        IngredientId = request.Ingredients[i].IngredientId,
                        Amount = request.Ingredients[i].Quantity,
                        AmountType = request.Ingredients[i].Measure,
                        MeasureTypeId = request.Ingredients[i].MeasureTypeId
                    });
                }
            }
            if (request.Ingredients.Count < recipe.MealIngredients.Count)
            {
                var countToRemove = recipe.MealIngredients.Count - request.Ingredients.Count;
                var toDelete = recipe.MealIngredients.GetRange(request.Ingredients.Count, countToRemove);

                changes = true;
                _dbContext.MealIngredients.RemoveRange(toDelete);
            }

            for (var i = 0; i < request.Steps.Count; i++)
            {
                if (recipe.Steps?.Count > i)
                {
                    if (recipe.Steps[i].Text != request.Steps[i].Text)
                    {
                        recipe.Steps[i].Text = request.Steps[i].Text;
                        changes = true;
                    }
                    if (recipe.Steps[i].Order != request.Steps[i].Order)
                    {
                        recipe.Steps[i].Order = request.Steps[i].Order;
                        changes = true;
                    }
                }
                else
                {
                    changes = true;
                    if (recipe.Steps == null)
                    {
                        recipe.Steps = new List<RecipeStep>();
                    }
                    recipe.Steps.Add(new RecipeStep { Text = request.Steps[i].Text, Order = request.Steps[i].Order });
                }
            }
            if (request.Steps.Count < recipe.Steps.Count)
            {
                var countToRemove = recipe.Steps.Count - request.Steps.Count;
                var toDelete = recipe.Steps.GetRange(request.Steps.Count, countToRemove);

                changes = true;
                _dbContext.RecipeSteps.RemoveRange(toDelete);
            }

            if ((_dbContext.Entry(recipe).State == EntityState.Unchanged && !changes) || _dbContext.SaveChanges() > 0)
            {
                return GetRecipe(recipe.Id);
            }

            return null;
        }

        //TODO: Extract "ImageService" to upload images, specifying the bucket
        public async Task<bool> UpdateRecipeImage(int recipeId, IFormFile avatarFile)
        {
            var foundRecipe = _dbContext.Meals.FirstOrDefault(p => p.Id == recipeId);
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
                return _dbContext.Entry(foundRecipe).State == EntityState.Unchanged || _dbContext.SaveChanges() == 1;
            }
            else
            {
                return false;
            }
        }

        public List<RecipeVoteDto> GetVotes(int userId)
        {
            var votes = _dbContext.RecipeVotes.Where(rv => rv.UserId == userId).ToList();

            return votes.Select(v => new RecipeVoteDto
            {
                RecipeId = v.RecipeId,
                Vote = v.Vote
            }).ToList();
        }

        public bool Vote(int recipeId, int userId, RecipeVote.VoteType voteValue)
        {
            var vote = _dbContext.RecipeVotes.FirstOrDefault(rv => rv.UserId == userId && rv.RecipeId == recipeId);

            if (vote == null)
            {
                vote = new RecipeVote {UserId = userId, RecipeId = recipeId };
                _dbContext.RecipeVotes.Add(vote);
            }

            vote.Vote = voteValue;

            return _dbContext.Entry(vote).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0;
        }

        private string GetRecipeImageUrl(string filename)
        {
            return $"https://s3-{_region}.amazonaws.com/{_recipeImagesBucketName}/{filename}";
        }

        public bool Remove(int id)
        {
            _dbContext.MealIngredients.RemoveRange(_dbContext.MealIngredients.Where(mi => mi.MealId == id));
            _dbContext.RecipeSteps.RemoveRange(_dbContext.RecipeSteps.Where(s =>  s.MealId == id));
            _dbContext.Meals.Remove(_dbContext.Meals.First(m => m.Id == id));
            
            foreach (var slot in _dbContext.ScheduleSlots.Where(s => s.MealId == id))
            {
                slot.MealId = 0;
            }

            return _dbContext.SaveChanges() > 0;
        }
        
        public RecipeDto ToRecipeDto(Meal meal)
        {
            if (meal == null)
            {
                return null;
            }

            return new RecipeDto
            {
                Id = meal.Id,
                Name = meal.Name,
                Brief = meal.Brief,
                Description = meal.Description,
                Image = meal.Image,
                CookTime = meal.CookTime,
                PrepTime = meal.PrepTime,
                NumServings = meal.NumServings,
                MealType = meal.MealType.ToString(),
                Source = meal.Source,
                Ingredients = meal.MealIngredients?.Select(ToRecipeIngredientDto)
                                    .OrderByDescending(i => !string.IsNullOrEmpty(i.Image))
                                    .ToList(),
                Steps = meal.Steps
                            .OrderBy(s => s.Order)
                            .ToList(),
                DietTypes = meal.MealDietTypes?.Select(mdt => mdt.DietTypeId).ToList(),
                Vote = meal.Votes != null && meal.Votes.Any() ? meal.Votes.First().Vote : RecipeVote.VoteType.UNKNOWN
            };
        }

        public RecipeIngredientDto ToRecipeIngredientDto(MealIngredient mealIngredient)
        {
            if (mealIngredient == null)
            {
                return null;
            }

            return new RecipeIngredientDto
            {
                Id = mealIngredient.IngredientId,
                IngredientId = mealIngredient.IngredientId,
                Quantity = mealIngredient.Amount,
                Measure = mealIngredient.MeasureType?.Name ?? mealIngredient.AmountType,
                MeasureTypeId = mealIngredient.MeasureTypeId,
                Name = mealIngredient.Ingredient.Name,
                Image = mealIngredient.Ingredient.Image,
                Category = mealIngredient.Ingredient.Category
            };
        }


    }
}
