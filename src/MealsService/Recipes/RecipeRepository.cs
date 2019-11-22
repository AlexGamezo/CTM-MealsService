using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using MealsService.Recipes.Data;

namespace MealsService.Recipes
{
    public class RecipeRepository : IRecipeRepository
    {
        private MealsDbContext _dbContext;

        public RecipeRepository(MealsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<Recipe> ListRecipes()
        {
            return _dbContext.Recipes
                .Include(m => m.RecipeIngredients)
                    .ThenInclude(mi => mi.Ingredient)
                        .ThenInclude(i => i.IngredientCategory)
                .Include(m => m.RecipeIngredients)
                    .ThenInclude(mi => mi.Ingredient)
                .Include(m => m.Steps)
                .Include(m => m.RecipeDietTypes)
                .Where(r => !r.Deleted)
                .ToList();
        }

        public List<Recipe> ListRecipesWithDeleted()
        {
            return _dbContext.Recipes
                .Include(m => m.RecipeIngredients)
                .ThenInclude(mi => mi.Ingredient)
                .ThenInclude(i => i.IngredientCategory)
                .Include(m => m.RecipeIngredients)
                .ThenInclude(mi => mi.Ingredient)
                .Include(m => m.Steps)
                .Include(m => m.RecipeDietTypes)
                .ToList();
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
            if (recipe.Id > 0)
            {
                var tracked = _dbContext.ChangeTracker.Entries<Recipe>()
                    .FirstOrDefault(m => m.Entity.Id == recipe.Id);
                if (tracked != null)
                {
                    _dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                }
                _dbContext.Recipes.Attach(recipe);
                _dbContext.Entry(recipe).State = EntityState.Modified;
            }
            else
            {
                _dbContext.Recipes.Add(recipe);
            }

            return _dbContext.SaveChanges() > 0;
        }

        public bool SetDietTypes(int recipeId, List<int> dietTypeIds)
        {
            var changes = false;
            var recipe = _dbContext.Recipes.Find(recipeId);

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
                _dbContext.RecipeDietTypes.RemoveRange(toDelete);
            }

            return !changes || _dbContext.SaveChanges() > 0;
        }

        public bool SetRecipeIngredients(int recipeId, List<RecipeIngredient> ingredients)
        {
            var changes = false;
            var recipe = _dbContext.Recipes.Find(recipeId);

            for (var i = 0; i < ingredients.Count; i++)
            {
                if (recipe.RecipeIngredients?.Count > i)
                {
                    if (recipe.RecipeIngredients[i].IngredientId != ingredients[i].IngredientId)
                    {
                        recipe.RecipeIngredients[i].IngredientId = ingredients[i].IngredientId;
                        changes = true;
                    }
                    if (Math.Abs(recipe.RecipeIngredients[i].Amount - ingredients[i].Amount) > 0.001)
                    {
                        recipe.RecipeIngredients[i].Amount = ingredients[i].Amount;
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
                        Amount = ingredients[i].Amount
                    });
                }
            }
            if (ingredients.Count < recipe.RecipeIngredients.Count)
            {
                var countToRemove = recipe.RecipeIngredients.Count - ingredients.Count;
                var toDelete = recipe.RecipeIngredients.GetRange(ingredients.Count, countToRemove);

                changes = true;
                _dbContext.RecipeIngredients.RemoveRange(toDelete);
            }

            return !changes || _dbContext.SaveChanges() > 0;
        }

        public bool SetRecipeSteps(int recipeId, List<RecipeStep> steps)
        {
            var changes = false;
            var recipe = _dbContext.Recipes.Find(recipeId);

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
                _dbContext.RecipeSteps.RemoveRange(toDelete);
            }

            return !changes || _dbContext.SaveChanges() > 0;
        }

    }
}
