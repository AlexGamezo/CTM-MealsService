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
        }

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
