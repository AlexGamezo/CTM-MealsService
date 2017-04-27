
using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.Models;
using MealsService.Requests;
using MealsService.Responses;
using Microsoft.EntityFrameworkCore;

namespace MealsService.Services
{
    public class RecipesService
    {
        private MealsDbContext _dbContext;

        public RecipesService(MealsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<RecipeDto> ListRecipes(ListRecipesRequest request)
        {
            IEnumerable<Meal> search = _dbContext.Meals
                .Include(m => m.MealIngredients)
                    .ThenInclude(mi => mi.Ingredient)
                    .ThenInclude(i => i.Category)
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

        public RecipeDto GetRecipe(int id)
        {
            var recipe = _dbContext.Meals
                .Include(m => m.MealIngredients)
                    .ThenInclude(mi => mi.Ingredient)
                    .ThenInclude(i => i.Category)
                .Include(m => m.MealDietTypes)
                .Include(m => m.Steps)
                .FirstOrDefault(m => m.Id == id);

            return ToRecipeDto(recipe);
        }

        public List<RecipeDto> GetRecipes(IEnumerable<int> ids)
        {
            var recipes = _dbContext.Meals
                .Include(m => m.MealIngredients)
                    .ThenInclude(mi => mi.Ingredient)
                    .ThenInclude(i => i.Category)
                .Include(m => m.MealDietTypes)
                .Include(m => m.Steps)
                .Where(m => ids.Contains(m.Id))
                .ToList();

            return recipes.Select(ToRecipeDto).ToList();
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
            recipe.Image = request.Image;
            recipe.MealType = mealType;

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
                }
                else
                {
                    changes = true;
                    recipe.MealIngredients.Add(new MealIngredient
                    {
                        IngredientId = request.Ingredients[i].IngredientId,
                        Amount = request.Ingredients[i].Quantity,
                        AmountType = request.Ingredients[i].Measure,
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

        public bool Remove(int id)
        {
            _dbContext.MealIngredients.RemoveRange(_dbContext.MealIngredients.Where(mi => mi.MealId == id));
            _dbContext.RecipeSteps.RemoveRange(_dbContext.RecipeSteps.Where(s =>  s.MealId == id));
            _dbContext.Meals.Remove(_dbContext.Meals.First(m => m.Id == id));
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
                MealType = meal.MealType.ToString(),
                Ingredients = meal.MealIngredients?.Select(ToRecipeIngredientDto).ToList(),
                Steps = meal.Steps,
                DietTypes = meal.MealDietTypes?.Select(mdt => mdt.DietTypeId).ToList()
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
                Measure = mealIngredient.AmountType,
                Name = mealIngredient.Ingredient.Name,
                Category = mealIngredient.Ingredient.Category.Name
            };
        }


    }
}
