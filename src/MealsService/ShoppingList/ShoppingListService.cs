using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MealsService.Recipes;
using MealsService.Schedules.Data;
using MealsService.Schedules.Dtos;
using MealsService.Services;
using MealsService.ShoppingList.Data;
using MealsService.ShoppingList.Dtos;
using MealsService.Users;
using MealsService.Users.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace MealsService.ShoppingList
{
    public class ShoppingListService
    {
        private MealsDbContext _dbContext;

        private const int JOURNEY_SHOPPING_ID = 2;

        private ScheduleService _scheduleService;
        private RecipesService _recipesService;
        private SubscriptionsService _subService;
        private IServiceProvider _serviceProvider;

        public ShoppingListService(MealsDbContext dbContext, ScheduleService scheduleService, RecipesService recipesService, SubscriptionsService subService, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;

            _scheduleService = scheduleService;
            _recipesService = recipesService;
            _serviceProvider = serviceProvider;
            _subService = subService;
        }

        public async Task<List<ShoppingListItem>> GetShoppingList(int userId, LocalDate weekStart, bool regenIfEmpty = true)
        {
            await _subService.VerifyDateInSubscriptionAsync(userId, weekStart);

            var items = _dbContext.ShoppingListItems
                .Where(i => i.UserId == userId && i.WeekStart == weekStart.ToDateTimeUnspecified())
                .Include(s => s.Ingredient)
                    .ThenInclude(i => i.IngredientCategory)
                .Include(s => s.MeasureType)
                .ToList();

            if (items.All(i => i.ManuallyAdded) && regenIfEmpty)
            {
                await GenerateShoppingListAsync(userId, weekStart);

                return await GetShoppingList(userId, weekStart, false);
            }

            return items;
        }

        public void ClearShoppingList(int userId, LocalDate weekStart, bool includeManuals = false)
        {
            var items = _dbContext.ShoppingListItems
                .Where(i => i.WeekStart == weekStart.ToDateTimeUnspecified() && (includeManuals || !i.ManuallyAdded))
                .ToList();

            _dbContext.ShoppingListItems.RemoveRange(items);
            _dbContext.SaveChanges();
        }

        public async Task GenerateShoppingListAsync(int userId, LocalDate weekStart)
        {
            await _subService.VerifyDateInSubscriptionAsync(userId, weekStart);

            var schedule = await _scheduleService.GetScheduleAsync(userId, weekStart, weekStart.PlusDays(6));

            ClearShoppingList(userId, weekStart);
            if (schedule.Any(d => d.Meals != null && d.Meals.Any(s => s.RecipeId > 0)))
            {
                var preparations = schedule.Where(d => d.Meals != null)
                    .SelectMany(d => d.Meals.Select(m => m.Preparation))
                    .Distinct()
                    .ToList();
                await HandlePreparationsAddedAsync(userId, preparations, weekStart, false);
            }
        }

        public void HandlePreparationsRemoved(int userId, List<PreparationDto> preparations)
        {
            //If these preparations had no recipe assigned, there's nothing to remove/update
            if (preparations.All(s => s.RecipeId == 0))
            {
                return;
            }

            var recipes = _recipesService.GetRecipes(preparations.Select(s => s.RecipeId).ToList()).ToDictionary(r => r.Id);
            var prepRecipes = preparations.Where(s => s.RecipeId != 0).ToDictionary(s => s.Id, s => recipes[s.RecipeId]);

            var associations = _dbContext.ShoppingListItemPreparations
                .Include(i => i.ShoppingListItem)
                .Where(i => prepRecipes.ContainsKey(i.PreparationId))
                .ToList();

            var unusedIngredients = new List<ShoppingListItem>();
            
            foreach (var assoc in associations)
            {
                if (!prepRecipes.ContainsKey(assoc.PreparationId))
                {
                    continue;
                }
                
                var preparation = preparations.First(p => p.Id == assoc.PreparationId);

                var recipe = prepRecipes[assoc.PreparationId];
                var ingredientScale = (float)preparation.NumServings / recipe.NumServings;
                var ingredient = recipe.Ingredients
                    .FirstOrDefault(i => i.IngredientId == assoc.ShoppingListItem.IngredientId);

                if (ingredient != null)
                {
                    if (assoc.ShoppingListItem.Checked)
                    {
                        var unused = unusedIngredients.FirstOrDefault(i =>
                            i.IngredientId == ingredient.IngredientId && i.MeasureTypeId == ingredient.MeasureTypeId);

                        if (unused == null)
                        {
                            unused = new ShoppingListItem
                            {
                                UserId = userId,
                                Checked = true,
                                IngredientId = ingredient.IngredientId,
                                MeasureTypeId = ingredient.MeasureTypeId,
                                NodaWeekStart = assoc.ShoppingListItem.NodaWeekStart,
                                Unused = true
                            };
                            unusedIngredients.Add(unused);
                        }

                        unused.Amount += ingredient.Quantity * ingredientScale;
                    }

                    assoc.ShoppingListItem.Amount -= ingredient.Quantity * ingredientScale;
                    
                    if (assoc.ShoppingListItem.Amount < 0.001)
                    {
                        _dbContext.Remove(assoc.ShoppingListItem);
                    }
                }
            }

            if (unusedIngredients.Any())
            {
                _dbContext.ShoppingListItems.AddRange(unusedIngredients);
            }

            //Need to remove, in case Preparation is being regenerated
            if (associations.Any())
            {
                _dbContext.RemoveRange(associations);
            }

            _dbContext.SaveChanges();
        }

        public async Task HandlePreparationsAddedAsync(int userId, List<PreparationDto> preparations, LocalDate weekStart, bool pregenShoppingList = true)
        {
            //Make sure the shopping list has been generated before making changes to it
            if (pregenShoppingList)
            {
                await GetShoppingList(userId, weekStart);
            }

            var recipeServings = preparations.Where(s => s.RecipeId > 0)
                .GroupBy(id => id.RecipeId).ToDictionary(g => g.Key, g => g.Sum(p => p.NumServings));

            //RecipeId => Recipe
            var recipes = _recipesService.FindRecipes(recipeServings.Keys).ToDictionary(r => r.Id);

            //RecipeIngredientId => RecipeId
            var recipeIngredientMap = recipes.Values
                .SelectMany(m => m.RecipeIngredients.Select(mi => new KeyValuePair<int, int>(mi.Id, m.Id)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var listItems = new List<ShoppingListItem>();

            //IngredientId => List<RecipeIngredient>
            var ingredients = recipes.Values.SelectMany(m => m.RecipeIngredients).GroupBy(mi => mi.IngredientId);

            foreach (var group in ingredients)
            {
                //List of RecipeIngredients, grouped by MeasureType
                foreach (var measureGroup in group.GroupBy(i => i.MeasureTypeId))
                {
                    var items = _dbContext.ShoppingListItems.Where(s =>
                            s.UserId == userId && s.IngredientId == group.Key && s.MeasureTypeId == measureGroup.Key &&
                            s.WeekStart == weekStart.ToDateTimeUnspecified())
                        .OrderByDescending(i => i.Unused)
                        .ToList();

                    var item = items.FirstOrDefault(i => !i.Unused && !i.Checked);
                    var checkedItem = items.FirstOrDefault(i => !i.Unused && i.Checked);
                    var unused = items.FirstOrDefault(i => i.Unused);
                    var amountNeeded = measureGroup.Sum(mi =>
                    {
                        var recipe = recipes[mi.RecipeId];
                        var recipeScale = (float) recipeServings[mi.RecipeId] / recipe.NumServings;
                        return mi.Amount * recipeScale;
                    });

                    if (unused != null)
                    {
                        var amountUsed = Math.Min(amountNeeded, unused.Amount);
                        amountNeeded -= amountUsed;

                        if (checkedItem == null)
                        {
                            checkedItem = new ShoppingListItem
                            {
                                UserId = userId,
                                IngredientId = group.Key,
                                MeasureTypeId = measureGroup.Key,
                                Amount = 0,
                                IngredientName = measureGroup.First().Ingredient.Name,
                                NodaWeekStart = weekStart,
                                Checked = true
                            };

                            listItems.Add(checkedItem);
                        }

                        checkedItem.Amount += amountUsed;
                        unused.Amount -= amountUsed;
                        if (unused.Amount <= 0.001)
                        {
                            _dbContext.Remove(unused);
                        }

                    }
                    if (item == null)
                    {
                        if (amountNeeded > 0)
                        {
                            item = new ShoppingListItem
                            {
                                UserId = userId,
                                IngredientId = group.Key,
                                MeasureTypeId = measureGroup.Key,
                                Amount = 0,
                                IngredientName = measureGroup.First().Ingredient.Name,
                                NodaWeekStart = weekStart
                            };

                            listItems.Add(item);
                        }
                        else if (checkedItem != null)
                        {
                            item = checkedItem;
                        }
                    }
                    
                    if (item.ShoppingListItemPreparations == null)
                    {
                        item.ShoppingListItemPreparations = new List<ShoppingListItemPreparation>();
                    }
                    //It's already been added
                    if (item.Id != checkedItem?.Id)
                    {
                        item.Amount += amountNeeded;
                    }

                    item.ShoppingListItemPreparations.AddRange(measureGroup.SelectMany(mi =>
                    {
                        var recipeId = recipeIngredientMap[mi.Id];
                        var recipePreps = preparations.Where(p => p.RecipeId == recipeId);
                        return recipePreps.Select(g =>
                            new ShoppingListItemPreparation
                            {
                                PreparationId = g.Id
                            });
                    }));
                }
            }

            _dbContext.ShoppingListItems.AddRange(listItems);
            _dbContext.SaveChanges();
        }

        public async Task<ShoppingListItem> AddItemAsync(int userId, LocalDate weekStart, ShoppingListItemDto dto)
        {
            await _subService.VerifyDateInSubscriptionAsync(userId, weekStart);

            var item = FromDto(dto);

            item.NodaWeekStart = weekStart;

            _dbContext.ShoppingListItems.Add(item);

            if (_dbContext.SaveChanges() > 0)
            {
                return item;
            }

            return null;
        }

        public async Task<bool> UpdateItemAsync(int userId, ShoppingListItemDto request)
        {
            var item = _dbContext.ShoppingListItems.FirstOrDefault(i => i.Id == request.Id);

            if (item.UserId != userId)
            {
                return false;
            }

            var changes = false;

            if (item.Checked != request.Checked)
            {
                item.Checked = request.Checked;
                changes = true;
            }
            if (item.ManuallyAdded != request.ManuallyAdded)
            {
                item.ManuallyAdded = request.ManuallyAdded;
                changes = true;
            }

            var success = !changes || _dbContext.SaveChanges() > 0;

            if (changes && success)
            {
                var unckecked = _dbContext.ShoppingListItems.Count(i => i.WeekStart == item.WeekStart && i.Checked == false);

                if (unckecked == 0)
                {
                    var updateRequest = new UpdateJourneyProgressRequest
                    {
                        JourneyStepId = JOURNEY_SHOPPING_ID,
                        Completed = true
                    };
                    await _serviceProvider.GetService<UsersService>().UpdateJourneyProgressAsync(userId, updateRequest);
                }
            }

            return success;
        }

        public bool RemoveItem(int userId, int id)
        {
            var item = _dbContext.ShoppingListItems.FirstOrDefault(i => i.Id == id);

            if (item.UserId != userId)
            {
                return false;
            }

            _dbContext.ShoppingListItems.Remove(item);

            return _dbContext.SaveChanges() > 0;
        }

        public ShoppingListItemDto ToDto(ShoppingListItem item)
        {
            return new ShoppingListItemDto
            {
                Id = item.Id,
                Checked = item.Checked,
                Unused = item.Unused,
                ManuallyAdded = item.ManuallyAdded,
                IngredientId = item.IngredientId,
                MeasureTypeId = item.MeasureTypeId,

                Name = item.IngredientId > 0 && item.Ingredient != null ? item.Ingredient.Name : item.IngredientName,
                Quantity = item.Amount,

                Category = item.Ingredient.Category,
                Image = item.Ingredient.Image,
                Measure = item.MeasureType?.Name ?? ""
            };
        }

        public ShoppingListItem FromDto(ShoppingListItemDto dto)
        {
            return new ShoppingListItem
            {
                Id = dto.Id,
                Checked = dto.Checked,
                ManuallyAdded = dto.ManuallyAdded,
                IngredientId = dto.IngredientId,
                MeasureTypeId = dto.MeasureTypeId,
                IngredientName = dto.Name,
                Amount = dto.Quantity,
            };
        }
    }
}
