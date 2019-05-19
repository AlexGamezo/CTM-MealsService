using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MealsService.Recipes.Data;
using MealsService.Recipes.Dtos;

namespace MealsService.Recipes
{
    public class RecipeRepository
    {
        private IServiceProvider _serviceContainer;

        public RecipeRepository(IServiceProvider serviceContainer)
        {
            _serviceContainer = serviceContainer;
        }

        public List<Recipe> ListRecipes(bool includeDeleted = false)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            return dbContext.Recipes
                .Include(m => m.RecipeIngredients)
                    .ThenInclude(mi => mi.Ingredient)
                        .ThenInclude(i => i.IngredientCategory)
                .Include(m => m.Steps)
                .Include(m => m.RecipeDietTypes)
                .Where(r => includeDeleted || !r.Deleted)
                .ToList();
        }

        public List<RecipeVote> GetUserVotes(int userId)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            return dbContext.RecipeVotes.Where(v => v.UserId == userId).ToList();
        }

        public bool DeleteRecipe(int recipeId)
        {
            var recipe = ListRecipes().FirstOrDefault(r => r.Id == recipeId);
            if (recipe != null)
            {
                recipe.Deleted = true;

                return SaveRecipe(recipe);
            }

            return false;
        }

        public bool SaveRecipe(Recipe recipe)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            if (recipe.Id > 0)
            {
                var tracked = dbContext.ChangeTracker.Entries<Recipe>()
                    .FirstOrDefault(m => m.Entity.Id == recipe.Id);
                if (tracked != null)
                {
                    dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                }
                dbContext.Recipes.Attach(recipe);
                dbContext.Entry(recipe).State = EntityState.Modified;
            }
            else
            {
                dbContext.Recipes.Add(recipe);
            }

            return dbContext.SaveChanges() > 0;
        }

        public bool SetDietTypes(Recipe recipe, List<int> dietTypeIds)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();
            var changes = false;

            for (var i = 0; i < dietTypeIds.Count; i++)
            {
                if (recipe.RecipeDietTypes?.Count > i)
                {
                    if (recipe.RecipeDietTypes[i].DietTypeId != dietTypeIds[i])
                    {
                        recipe.RecipeDietTypes[i].DietTypeId = dietTypeIds[i];
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
                    recipe.RecipeDietTypes.Add(new RecipeDietType { DietTypeId = dietTypeIds[i] });
                }
            }
            if (dietTypeIds.Count < recipe.RecipeDietTypes.Count)
            {
                var countToRemove = recipe.RecipeDietTypes.Count - dietTypeIds.Count;
                var toDelete = recipe.RecipeDietTypes.GetRange(dietTypeIds.Count, countToRemove);

                changes = true;
                dbContext.RecipeDietTypes.RemoveRange(toDelete);
            }

            return !changes || dbContext.SaveChanges() > 0;
        }

        public bool SetRecipeIngredients(Recipe recipe, List<RecipeIngredientDto> ingredients)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();
            var changes = false;

            for (var i = 0; i < ingredients.Count; i++)
            {
                if (recipe.RecipeIngredients?.Count > i)
                {
                    if (recipe.RecipeIngredients[i].IngredientId != ingredients[i].IngredientId)
                    {
                        recipe.RecipeIngredients[i].IngredientId = ingredients[i].IngredientId;
                        changes = true;
                    }
                    if (Math.Abs(recipe.RecipeIngredients[i].Amount - ingredients[i].Quantity) > 0.001)
                    {
                        recipe.RecipeIngredients[i].Amount = ingredients[i].Quantity;
                        changes = true;
                    }
                    if (recipe.RecipeIngredients[i].AmountType != ingredients[i].Measure)
                    {
                        recipe.RecipeIngredients[i].AmountType = ingredients[i].Measure;
                        changes = true;
                    }
                    if (recipe.RecipeIngredients[i].MeasureTypeId != ingredients[i].MeasureTypeId)
                    {
                        recipe.RecipeIngredients[i].MeasureTypeId = ingredients[i].MeasureTypeId;
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
                        IngredientId = ingredients[i].IngredientId,
                        Amount = ingredients[i].Quantity,
                        AmountType = ingredients[i].Measure,
                        MeasureTypeId = ingredients[i].MeasureTypeId
                    });
                }
            }
            if (ingredients.Count < recipe.RecipeIngredients.Count)
            {
                var countToRemove = recipe.RecipeIngredients.Count - ingredients.Count;
                var toDelete = recipe.RecipeIngredients.GetRange(ingredients.Count, countToRemove);

                changes = true;
                dbContext.RecipeIngredients.RemoveRange(toDelete);
            }

            return !changes || dbContext.SaveChanges() > 0;
        }

        public bool SetRecipeSteps(Recipe recipe, List<RecipeStep> steps)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();
            var changes = false;

            for (var i = 0; i < steps.Count; i++)
            {
                if (recipe.Steps?.Count > i)
                {
                    if (recipe.Steps[i].Text != steps[i].Text)
                    {
                        recipe.Steps[i].Text = steps[i].Text;
                        changes = true;
                    }
                    if (recipe.Steps[i].Order != steps[i].Order)
                    {
                        recipe.Steps[i].Order = steps[i].Order;
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
                    recipe.Steps.Add(new RecipeStep { Text = steps[i].Text, Order = steps[i].Order });
                }
            }
            if (steps.Count < recipe.Steps.Count)
            {
                var countToRemove = recipe.Steps.Count - steps.Count;
                var toDelete = recipe.Steps.GetRange(steps.Count, countToRemove);

                changes = true;
                dbContext.RecipeSteps.RemoveRange(toDelete);
            }

            return !changes || dbContext.SaveChanges() > 0;
        }

        public bool SaveVote(RecipeVote vote)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            if (vote.Id > 0)
            {
                var tracked = dbContext.ChangeTracker.Entries<RecipeVote>()
                    .FirstOrDefault(m => m.Entity.Id == vote.Id);
                if (tracked != null)
                {
                    dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                }
                dbContext.RecipeVotes.Attach(vote);
                dbContext.Entry(vote).State = EntityState.Modified;
            }
            else
            {
                dbContext.RecipeVotes.Add(vote);
            }

            return dbContext.SaveChanges() > 0;
        }
    }
}
