using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.Extensions;

using MealsService.Common.Extensions;
using MealsService.Recipes;
using MealsService.Schedules.Dtos;
using MealsService.Services;
using MealsService.ShoppingList.Data;
using MealsService.ShoppingList.Dtos;
using MealsService.Users;
using MealsService.Users.Data;
using MealsService.Ingredients;

namespace MealsService.ShoppingList
{
    public class ShoppingListService
    {
        //private MealsDbContext _dbContext;

        private const int JOURNEY_SHOPPING_ID = 2;

        private ScheduleService _scheduleService;
        private RecipesService _recipesService;
        private SubscriptionsService _subService;
        private MeasureTypesService _measureTypesService;

        private ShoppingListRepository _repository;
        private IServiceProvider _serviceProvider;

        public ShoppingListService(ScheduleService scheduleService, RecipesService recipesService,
            SubscriptionsService subService, ShoppingListRepository repository, MeasureTypesService measureTypesService,
            IServiceProvider serviceProvider)
        {
            _scheduleService = scheduleService;
            _recipesService = recipesService;
            _serviceProvider = serviceProvider;
            _subService = subService;
            _measureTypesService = measureTypesService;

            _repository = repository;
        }

        public async Task<List<ShoppingListItem>> GetShoppingListAsync(int userId, LocalDate weekStart, bool regenIfEmpty = true)
        {
            await _subService.VerifyDateInSubscriptionAsync(userId, weekStart);

            var weekStartUnspecified = weekStart.ToDateTimeUnspecified();

            var items = _repository.FetchShoppingListItems(userId, weekStartUnspecified);

            if (items.All(i => i.ManuallyAdded) && regenIfEmpty)
            {
                await GenerateShoppingListAsync(userId, weekStart);

                return await GetShoppingListAsync(userId, weekStart, false);
            }

            return items;
        }

        public async Task<List<ShoppingListItem>> GetShoppingListForPreparationAsync(int userId, int preparationId, bool regenIfEmpty = true)
        {
            var prep = await _scheduleService.GetPreparationAsync(userId, preparationId);

            var prepShoppingItems = _repository.FetchShoppingListItemsForPreparation(preparationId);

            if (!prepShoppingItems.Any() && regenIfEmpty)
            {
                await GetShoppingListAsync(userId, prep.Date.GetWeekStart().ToLocalDateTime().Date);

                prepShoppingItems = _repository.FetchShoppingListItemsForPreparation(preparationId);
            }
    
            return prepShoppingItems;
        }


        public void ClearShoppingList(int userId, LocalDate weekStart, bool includeManuals = false)
        {
            _repository.RemoveShoppingListItemsForDate(userId, weekStart, includeManuals);

            //TODO: Clear Cache
        }

        public async Task GenerateShoppingListAsync(int userId, LocalDate weekStart)
        {
            await _subService.VerifyDateInSubscriptionAsync(userId, weekStart);

            var schedule = await _scheduleService.GetScheduleAsync(userId, weekStart, weekStart.PlusDays(6));

            ClearShoppingList(userId, weekStart);

            var scheduledDays = schedule.Where(d => d.Meals != null && d.Meals.Any(s => s.RecipeId > 0)).ToList();
            if (scheduledDays.Any())
            {
                var preparations = scheduledDays.SelectMany(d => d.Meals.Select(m => m.Preparation))
                    .Distinct()
                    .ToList();
                HandlePreparationsAdded(userId, preparations, weekStart);
            }
        }

        public void HandlePreparationRemoved(int userId, PreparationDto preparation)
        {
            var shoppingList = GetShoppingListForPreparationAsync(userId, preparation.Id).Result;

            foreach(var item in shoppingList)
            {
                if(item.Checked)
                {
                    if(UpdateUnusedItem(userId, item.IngredientId, item.MeasureTypeId, LocalDate.FromDateTime(preparation.Date), item.Amount))
                    {
                        _repository.RemoveShoppingListItemsById(new List<int> { item.Id });
                    }
                    else
                    {
                        item.PreparationId = 0;
                        _repository.SaveItem(item);
                    }
                }
                else
                {
                    _repository.RemoveShoppingListItemsById(new List<int> { item.Id });
                }
            }
        }

        public void HandlePreparationsRemoved(int userId, List<PreparationDto> preparations)
        {
            foreach(var prep in preparations)
            {
                HandlePreparationRemoved(userId, prep);
            }
            //If these preparations had no recipe assigned, there's nothing to remove/update
            /*var filledPreparations = preparations.Where(s => s.RecipeId > 0).ToList();
            if (!filledPreparations.Any())
            {
                return;
            }

            var recipeIds = filledPreparations.Select(s => s.RecipeId).ToList();

            var recipes = _recipesService.GetRecipes(recipeIds).ToDictionary(r => r.Id);
            var prepRecipes = filledPreparations.ToDictionary(s => s.Id, s => recipes[s.RecipeId]);
            var prepIds = filledPreparations.Select(p => p.Id).ToList();

            var associations = _dbContext.ShoppingListItemPreparations
                .Include(i => i.ShoppingListItem)
                .Where(i => prepIds.Contains(i.PreparationId))
                .ToList();

            var preparationItems = _repository.FetchShoppingListItemsForPreparations(prepIds);
            
            //TODO: Load any unused ingredients only tracked
            var removedIds = new List<int>();
            
            foreach (var item in preparationItems)
            {
                var preparation = preparations.First(p => p.Id == item.PreparationId);
                
                var recipe = prepRecipes[item.PreparationId];
                var ingredientScale = (float)preparation.NumServings / recipe.NumServings;
                var ingredient = recipe.Ingredients.FirstOrDefault(i => i.IngredientId == item.IngredientId);

                if (ingredient == null)
                {
                    continue;
                }

                var amount = ingredient.Quantity * ingredientScale;

                if (item.Checked)
                {
                    UpdateUnusedItem(userId, ingredient.IngredientId, ingredient.MeasureTypeId, item.NodaWeekStart, amount);
                }

                item.Amount -= ingredient.Quantity * ingredientScale;
                
                if (item.Amount < 0.001)
                {
                    removedIds.Add(item.Id);
                }
            }

            _repository.RemoveShoppingListItemsById(removedIds);

            //Need to remove, in case Preparation is being regenerated
            if (associations.Any())
            {
                _dbContext.RemoveRange(associations);
            }

            _dbContext.SaveChanges();*/
        }

        private bool UpdateUnusedItem(int userId, int ingId, int measureId, LocalDate weekStart, float amount)
        {
            var weekStartUnspecified = weekStart.ToDateTimeUnspecified();
            var unused = _repository.GetShoppingListItems(userId, weekStart, false)
                .FirstOrDefault(i => i.PreparationId == 0 && i.IngredientId == ingId);
            
            if (unused != null)
            {
                //TODO: This should convert the amount to target measurement type
                unused.Amount += amount;

                return true;
            }

            return false;
        }

        public void HandlePreparationAdded(int userId, PreparationDto preparation)
        {
            var recipe = _recipesService.FindRecipeById(preparation.RecipeId);

            var weekStart = LocalDate.FromDateTime(preparation.Date).GetWeekStart();

            foreach (var ingredient in recipe.RecipeIngredients)
            {
                var amount = preparation.NumServings * ingredient.Amount;

                var remainingAmount = ConsumeUnusedItems(userId, ingredient.IngredientId, weekStart, ingredient.MeasureTypeId, amount, preparation.Id);

                if(remainingAmount > 0)
                {
                    var item = GenerateShoppingListItem(userId, ingredient.IngredientId, weekStart, ingredient.MeasureTypeId, remainingAmount);
                    item.PreparationId = preparation.Id;

                    _repository.SaveItem(item);
                }
            }

            //TODO: Clear cache
        }

        private ShoppingListItem GenerateShoppingListItem(int userId, int ingredientId, LocalDate weekStart, int measureTypeId, float amount)
        {
            return new ShoppingListItem
            {
                UserId = userId,
                IngredientId = ingredientId,
                Amount = amount,
                MeasureTypeId = measureTypeId,
                NodaWeekStart = weekStart
            };
        }

        private float ConsumeUnusedItems(int userId, int ingredientId, LocalDate weekStart, int measureTypeId, float amount, int preparationId)
        {
            var unusedItems = GetUnusedItemsForIngredient(userId, weekStart, ingredientId);

            var remainingAmount = amount;

            foreach(var item in unusedItems)
            {
                remainingAmount = ConsumeUnusedItem(item, remainingAmount, preparationId);

                if(remainingAmount == 0)
                {
                    break;
                }
            }

            return remainingAmount;
        }

        private float ConsumeUnusedItem(ShoppingListItem item, float remainingAmount, int preparationId)
        {
            throw new NotImplementedException();
        }

        private List<ShoppingListItem> GetUnusedItemsForIngredient(int userId, LocalDate weekStart, int ingredientId)
        {
            return GetUnusedItems(userId, weekStart).Where(i => i.IngredientId == ingredientId).ToList();
        }

        private List<ShoppingListItem> GetUnusedItems(int userId, LocalDate weekStart)
        {
            return _repository.GetShoppingListItems(userId, weekStart, false).Where(i => i.PreparationId == 0).ToList();
        }

        public void HandlePreparationsAdded(int userId, List<PreparationDto> preparations, LocalDate weekStart)
        {
            foreach(var prep in preparations)
            {
                HandlePreparationAdded(userId, prep);
            }

            //Make sure the shopping list has been generated before making changes to it
        /*
            if (pregenShoppingList)
            {
                await GetShoppingListAsync(userId, weekStart);
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
                                NodaWeekStart = weekStart,
                                PreparationId = g.Id
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
        */
        }

        /*public async Task<ShoppingListItem> AddBoughtItemAsync(int userId, LocalDate weekStart, ShoppingListItemDto dto)
        {
            var item = FromDto(dto);

            var list = await GetShoppingListAsync(userId, weekStart, false);

            //Prefer checking off items not manually added
            var foundItems = list.Where(i => i.IngredientId == dto.IngredientId && !i.Checked)
                .OrderBy(i => i.ManuallyAdded)
                .ToList();

            foreach (var found in foundItems)
            {
                if (found.Amount >= item.Amount)
                {
                    found.Checked = true;
                    item.Amount -= found.Amount;
                    break;
                }

                found.Amount -= item.Amount;
                item.Amount = 0;
            }

            item.NodaWeekStart = weekStart;

            if (item.Amount >= 0.00001 && _repository.SaveItem(item))
            {
                return item;
            }

            return null;
        }*/

        public async Task<ShoppingListItem> AddItemAsync(int userId, LocalDate weekStart, ShoppingListItemDto dto)
        {
            await _subService.VerifyDateInSubscriptionAsync(userId, weekStart);

            var item = FromDto(dto);

            item.NodaWeekStart = weekStart;

            if (_repository.SaveItem(item))
            {
                return item;
            }

            return null;
        }

        public async Task<bool> UpdateItemAsync(int userId, ShoppingListItemDto request)
        {
            var item = _repository.FetchShoppingListItemsById(new List<int> { request.Id }).FirstOrDefault();
            
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

            var success = !changes || _repository.SaveItem(item);

            if (changes && success)
            { 
                var uncheckedItems = _repository.FetchShoppingListItems(userId, item.WeekStart).Count(i => i.Checked == false);

                if (uncheckedItems == 0)
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

        public async Task<bool> UpdateItemsAsync(int userId, List<ShoppingListItemDto> updatedItems)
        {
            var items = _repository.FetchShoppingListItemsById(updatedItems.Select(i => i.Id).ToList());

            if (items.Any(i => i.UserId != userId))
            {
                return false;
            }

            var changes = false;

            foreach (var item in items)
            {
                var updatedItem = updatedItems.First(i => i.Id == item.Id);
                if (item.Checked != updatedItem.Checked)
                {
                    item.Checked = updatedItem.Checked;
                    changes = true;
                }
                if (item.ManuallyAdded != updatedItem.ManuallyAdded)
                {
                    item.ManuallyAdded = updatedItem.ManuallyAdded;
                    changes = true;
                }
            }

            var success = !changes || _repository.SaveItems(items);

            if (changes && success)
            {
                var weekStart = items.Select(i => i.WeekStart).First();
                var uncheckedItems = _repository.FetchShoppingListItems(userId, weekStart).Count(i => i.Checked == false);

                if (uncheckedItems == 0)
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
            var item = _repository.FetchShoppingListItemsById(new List<int> { id }).FirstOrDefault();

            if (item == null || item.UserId != userId)
            {
                return false;
            }

            _repository.RemoveShoppingListItemsById(new List<int> { id });

            return true;
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
                Measure = item.MeasureType?.Name ?? "",
                PreparationId = item.PreparationId ?? 0
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

                PreparationId = dto.PreparationId > 0 ? dto.PreparationId : (int?)null
            };
        }
    }
}
