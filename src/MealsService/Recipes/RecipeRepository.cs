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
            var recipeDietTypes = _dbContext.RecipeDietTypes.Where(dt => dt.RecipeId == recipeId).ToList();
            
            if (dietTypeIds == null)
            {
                dietTypeIds = new List<int>();
            }

            for (var i = 0; i < dietTypeIds.Count; i++)
            {
                if (recipeDietTypes.Count > i)
                {
                    if (recipeDietTypes[i].DietTypeId != dietTypeIds[i])
                    {
                        recipeDietTypes[i].DietTypeId = dietTypeIds[i];
                        changes = true;
                    }
                }
                else
                {
                    changes = true;
                    _dbContext.RecipeDietTypes.Add(new RecipeDietType { DietTypeId = dietTypeIds[i], RecipeId = recipeId });
                }
            }
            if (dietTypeIds.Count < recipeDietTypes.Count)
            {
                var countToRemove = recipeDietTypes.Count - dietTypeIds.Count;
                var toDelete = recipeDietTypes.GetRange(dietTypeIds.Count, countToRemove);

                changes = true;
                _dbContext.RecipeDietTypes.RemoveRange(toDelete);
            }

            return !changes || _dbContext.SaveChanges() > 0;
        }

        public bool SetRecipeIngredients(int recipeId, List<RecipeIngredient> ingredients)
        {
            var changes = false;
            var recipeIngredients = _dbContext.RecipeIngredients.Where(ri => ri.RecipeId == recipeId).ToList();

            if (ingredients == null)
            {
                ingredients = new List<RecipeIngredient>();
            }

            for (var i = 0; i < ingredients.Count; i++)
            {
                if (recipeIngredients.Count > i)
                {
                    if (recipeIngredients[i].IngredientId != ingredients[i].IngredientId)
                    {
                        recipeIngredients[i].IngredientId = ingredients[i].IngredientId;
                        changes = true;
                    }
                    if (Math.Abs(recipeIngredients[i].Amount - ingredients[i].Amount) > 0.001)
                    {
                        recipeIngredients[i].Amount = ingredients[i].Amount;
                        changes = true;
                    }
                }
                else
                {
                    changes = true;
                    
                    _dbContext.RecipeIngredients.Add(new RecipeIngredient
                    {
                        RecipeId = recipeId,
                        IngredientId = ingredients[i].IngredientId,
                        Amount = ingredients[i].Amount
                    });
                }
            }
            if (ingredients.Count < recipeIngredients.Count)
            {
                var countToRemove = recipeIngredients.Count - ingredients.Count;
                var toDelete = recipeIngredients.GetRange(ingredients.Count, countToRemove);

                changes = true;
                _dbContext.RecipeIngredients.RemoveRange(toDelete);
            }

            return !changes || _dbContext.SaveChanges() > 0;
        }

        public bool SetRecipeSteps(int recipeId, List<RecipeStep> steps)
        {
            var changes = false;
            var recipeSteps = _dbContext.RecipeSteps.Where(rs => rs.RecipeId == recipeId).ToList();

            if (steps == null)
            {
                steps = new List<RecipeStep>();
            }

            for (var i = 0; i < steps.Count; i++)
            {
                if (recipeSteps?.Count > i)
                {
                    if (recipeSteps[i].Text != steps[i].Text)
                    {
                        recipeSteps[i].Text = steps[i].Text;
                        changes = true;
                    }
                    if (recipeSteps[i].Order != steps[i].Order)
                    {
                        recipeSteps[i].Order = steps[i].Order;
                        changes = true;
                    }
                }
                else
                {
                    changes = true;
                    _dbContext.RecipeSteps.Add(new RecipeStep { RecipeId = recipeId, Text = steps[i].Text, Order = steps[i].Order });
                }
            }
            if (steps.Count < recipeSteps.Count)
            {
                var countToRemove = recipeSteps.Count - steps.Count;
                var toDelete = recipeSteps.GetRange(steps.Count, countToRemove);

                changes = true;
                _dbContext.RecipeSteps.RemoveRange(toDelete);
            }

            return !changes || _dbContext.SaveChanges() > 0;
        }

    }
}
