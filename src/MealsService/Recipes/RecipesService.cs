using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using MealsService.Configurations;
using MealsService.Ingredients.Data;
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
            IEnumerable<Recipe> search = _dbContext.Recipes
                .Include(m => m.RecipeIngredients)
                    .ThenInclude(mi => mi.Ingredient)
                    .ThenInclude(i => i.IngredientCategory)
                .Include(m => m.Steps)
                .Include(m => m.RecipeDietTypes);

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
                        var matchedIngredients = m.RecipeIngredients.Count(mi => ingIds.Contains(mi.IngredientId));
                        return request.AllIngredients ? matchedIngredients == ingIds.Count : matchedIngredients > 0;
                    });
                }
                if (request.MealType != MealType.Any)
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

        public RecipeDto GetBySlug(string slug, int userId = 0)
        {
            return FindRecipesBySlug(new[] {slug}, userId)
                .Select(ToRecipeDto).FirstOrDefault();
        }

        public List<Recipe> FindRecipesBySlug(IEnumerable<string> slugs, int userId = 0)
        {
            var recipeIds = _dbContext.Recipes.Where(r => slugs.Contains(r.Slug))
                .ToList().Select(r => r.Id);

            return FindRecipes(recipeIds, userId);
        }

        public List<RecipeDto> GetRecipes(IEnumerable<int> ids, int userId = 0)
        {
            var recipes = FindRecipes(ids, userId);

            return recipes.Select(ToRecipeDto).ToList();
        }

        public List<Recipe> FindRecipes(IEnumerable<int> ids, int userId = 0)
        {
            var recipes = _dbContext.Recipes
                .Include(m => m.RecipeIngredients)
                    .ThenInclude(mi => mi.Ingredient)
                        .ThenInclude(i => i.IngredientCategory)
                .Include(m => m.RecipeIngredients)
                    .ThenInclude(mi => mi.MeasureType)
                .Include(m => m.RecipeDietTypes)
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
            MealType recipeType;
            Enum.TryParse(request.MealType, out recipeType);
            var changes = false;

            Recipe recipe;

            if (id > 0)
            {
                recipe = _dbContext.Recipes
                .Include(m => m.RecipeIngredients)
                .Include(m => m.Steps)
                .Include(m => m.RecipeDietTypes)
                .FirstOrDefault(m => m.Id == id);
            }
            else
            {
                recipe = new Recipe();
                _dbContext.Recipes.Add(recipe);
            }

            recipe.Name = request.Name;

            if (!string.IsNullOrEmpty(request.Slug))
            {
                recipe.Slug = request.Slug;
            }
            else if(string.IsNullOrEmpty(recipe.Slug))
            {
                recipe.Slug = GenerateSlug(request.Name, id);

                if (recipe.Slug == null)
                {
                    //TODO: Throw appropriate exception to be shown
                }
            }

            recipe.Brief = request.Brief;
            recipe.Description = request.Description;
            recipe.CookTime = request.CookTime;
            recipe.PrepTime = request.PrepTime;
            recipe.NumServings = request.NumServings;
            recipe.Image = request.Image;
            recipe.MealType = recipeType;
            recipe.Source = request.Source;

            for (var i = 0; i < request.DietTypeIds.Count; i++)
            {
                if (recipe.RecipeDietTypes?.Count > i)
                {
                    if (recipe.RecipeDietTypes[i].DietTypeId != request.DietTypeIds[i])
                    {
                        recipe.RecipeDietTypes[i].DietTypeId = request.DietTypeIds[i];
                        changes = true;
                    }
                }
                else
                {
                    changes = true;
                    if (recipe.RecipeDietTypes == null)
                    {
                        recipe.RecipeDietTypes = new List<RecipeDietType>();
                    }
                    recipe.RecipeDietTypes.Add(new RecipeDietType {DietTypeId = request.DietTypeIds[i]});
                }
            }
            if (request.DietTypeIds?.Count < recipe.RecipeDietTypes.Count)
            {
                var countToRemove = recipe.RecipeDietTypes.Count - request.DietTypeIds.Count;
                var toDelete = recipe.RecipeDietTypes.GetRange(request.DietTypeIds.Count, countToRemove);

                changes = true;
                _dbContext.RecipeDietTypes.RemoveRange(toDelete);
            }

            for (var i = 0; i < request.Ingredients.Count; i++)
            {
                if (recipe.RecipeIngredients?.Count > i)
                {
                    if (recipe.RecipeIngredients[i].IngredientId != request.Ingredients[i].IngredientId)
                    {
                        recipe.RecipeIngredients[i].IngredientId = request.Ingredients[i].IngredientId;
                        changes = true;
                    }
                    if (recipe.RecipeIngredients[i].Amount != request.Ingredients[i].Quantity)
                    {
                        recipe.RecipeIngredients[i].Amount = request.Ingredients[i].Quantity;
                        changes = true;
                    }
                    if (recipe.RecipeIngredients[i].AmountType != request.Ingredients[i].Measure)
                    {
                        recipe.RecipeIngredients[i].AmountType = request.Ingredients[i].Measure;
                        changes = true;
                    }
                    if (recipe.RecipeIngredients[i].MeasureTypeId != request.Ingredients[i].MeasureTypeId)
                    {
                        recipe.RecipeIngredients[i].MeasureTypeId = request.Ingredients[i].MeasureTypeId;
                        changes = true;
                    }
                }
                else
                {
                    changes = true;
                    if (recipe.RecipeIngredients == null)
                    {
                        recipe.RecipeIngredients = new List<RecipeIngredient>();
                    }
                    recipe.RecipeIngredients.Add(new RecipeIngredient
                    {
                        IngredientId = request.Ingredients[i].IngredientId,
                        Amount = request.Ingredients[i].Quantity,
                        AmountType = request.Ingredients[i].Measure,
                        MeasureTypeId = request.Ingredients[i].MeasureTypeId
                    });
                }
            }
            if (request.Ingredients.Count < recipe.RecipeIngredients.Count)
            {
                var countToRemove = recipe.RecipeIngredients.Count - request.Ingredients.Count;
                var toDelete = recipe.RecipeIngredients.GetRange(request.Ingredients.Count, countToRemove);

                changes = true;
                _dbContext.RecipeIngredients.RemoveRange(toDelete);
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
            var foundRecipe = _dbContext.Recipes.FirstOrDefault(p => p.Id == recipeId);
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

                if (_dbContext.Recipes.Any(m => m.Slug == testSlug && id != m.Id))
                {
                    return testSlug;
                }
            }

            return null;
        }

        public bool Remove(int id)
        {
            _dbContext.RecipeIngredients.RemoveRange(_dbContext.RecipeIngredients.Where(mi => mi.RecipeId == id));
            _dbContext.RecipeSteps.RemoveRange(_dbContext.RecipeSteps.Where(s =>  s.RecipeId == id));
            _dbContext.Recipes.Remove(_dbContext.Recipes.First(m => m.Id == id));
            
            foreach (var slot in _dbContext.Meals.Where(s => s.RecipeId == id))
            {
                slot.RecipeId = 0;
            }

            return _dbContext.SaveChanges() > 0;
        }

        public List<RecipeIngredientDto> ConvertMeasureType(List<RecipeIngredient> recipeIngredients, MeasureSystem system = MeasureSystem.IMPERIAL)
        {
            var dtos = new List<RecipeIngredientDto>();

            if (!recipeIngredients.Any())
            {
                return dtos;
            }


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
                MealType = recipe.MealType.ToString(),
                Source = recipe.Source,
                Ingredients = recipe.RecipeIngredients?.Select(ToRecipeIngredientDto)
                                    .OrderByDescending(i => !string.IsNullOrEmpty(i.Image))
                                    .ToList(),
                Steps = recipe.Steps
                            .OrderBy(s => s.Order)
                            .ToList(),
                DietTypes = recipe.RecipeDietTypes?.Select(mdt => mdt.DietTypeId).ToList(),
                Vote = recipe.Votes != null && recipe.Votes.Any() ? recipe.Votes.First().Vote : RecipeVote.VoteType.UNKNOWN,
                Slug = recipe.Slug
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


    }
}
