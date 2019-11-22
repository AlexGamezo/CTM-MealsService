using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NodaTime;

using MealsService.Common.Extensions;
using MealsService.Recipes;
using MealsService.Schedules.Dtos;
using MealsService.ShoppingList.Data;
using MealsService.ShoppingList.Dtos;
using MealsService.Users;
using MealsService.Ingredients;
using MealsService.Ingredients.Data;
using MealsService.Users.Data;

namespace MealsService.ShoppingList
{
    public class ShoppingListService : IShoppingListService
    {
        private const int JOURNEY_SHOPPING_ID = 2;

        private IRecipesService _recipesService;
        private SubscriptionsService _subService;

        private IIngredientsService _ingredientsService;
        
        private ShoppingListRepository _repository;
        private UsersService _usersService;

        public ShoppingListService(IRecipesService recipesService,
            IIngredientsService ingredientsService,
            SubscriptionsService subService,
            ShoppingListRepository repository,
            UsersService usersService)
        {
            _recipesService = recipesService;
            _ingredientsService = ingredientsService;

            _subService = subService;

            _repository = repository;

            _usersService = usersService;
        }

        public async Task<List<ShoppingListItem>> GetShoppingListAsync(int userId, LocalDate weekStart)
        {
            await _subService.VerifyDateInSubscriptionAsync(userId, weekStart);

            var weekStartUnspecified = weekStart.ToDateTimeUnspecified();

            var items = _repository.FetchShoppingListItems(userId, weekStartUnspecified);

            return items;
        }

        public async Task<List<ShoppingListItemDto>> GetGroupedShoppingListAsync(int userId, LocalDate weekStart)
        {
            var ungroupedList = await GetShoppingListAsync(userId, weekStart);
            var ungroupedDtos = ungroupedList.Select(i => ToDto(i)).ToList();

            var unusedDtos = ungroupedDtos.Where(dto => dto.Unused).ToList();
            var checkedDtos = ungroupedDtos.Where(dto => !dto.Unused && dto.Checked).ToList();
            var manuallyAdded = ungroupedDtos.Where(dto => !dto.Unused && dto.ManuallyAdded).ToList();
            var uncheckedDtos = ungroupedDtos.Where(dto => !dto.Unused && !dto.Checked && !dto.ManuallyAdded).ToList();

            var groupedDtos = new List<ShoppingListItemDto>();

            groupedDtos.AddRange(GroupShoppingListItems(checkedDtos));
            groupedDtos.AddRange(GroupShoppingListItems(uncheckedDtos));
            groupedDtos.AddRange(GroupShoppingListItems(manuallyAdded));
            groupedDtos.AddRange(GroupShoppingListItems(unusedDtos));

            return groupedDtos;
        }

        private List<ShoppingListItemDto> GroupShoppingListItems(List<ShoppingListItemDto> dtos)
        {
            var groupedDtos = new List<ShoppingListItemDto>();
            foreach (var dtoGroup in dtos.GroupBy(dto => dto.MeasuredIngredient.IngredientId))
            {
                var ingredientGroup = dtoGroup.Select(dto => dto.MeasuredIngredient).ToList();
                var groupMeasuredIngredient = _ingredientsService.GroupMeasuredIngredients(ingredientGroup);
                groupedDtos.Add(new ShoppingListItemDto
                {
                    Ids = dtoGroup.SelectMany(dto => dto.Ids).ToList(),
                    Checked = dtoGroup.First().Checked,
                    ManuallyAdded = dtoGroup.First().ManuallyAdded,
                    MeasuredIngredient = groupMeasuredIngredient,
                    PreparationIds = dtoGroup.SelectMany(dto => dto.PreparationIds).ToList(),
                    Unused = dtoGroup.First().Unused
                });
            }

            return groupedDtos;
        }

        public async Task<List<ShoppingListItemDto>> GetShoppingListForPreparationAsync(
            int userId, int prepId)
        {
            var prepShoppingItems = _repository.FetchShoppingListItemsForPreparation(prepId);

            /*if (!prepShoppingItems.Any() && regenIfEmpty)
            {
                await GetShoppingListAsync(userId, prep.Date.GetWeekStart().ToLocalDateTime().Date);

                prepShoppingItems = _repository.FetchShoppingListItemsForPreparation(prep.Id);
            }*/

            var ungroupedDtos = prepShoppingItems.Select(i => ToDto(i)).ToList();

            var checkedDtos = ungroupedDtos.Where(dto => dto.Checked).ToList();
            var manuallyAdded = ungroupedDtos.Where(dto => dto.ManuallyAdded).ToList();
            var uncheckedDtos = ungroupedDtos.Where(dto => !dto.Checked && !dto.ManuallyAdded).ToList();

            var groupedDtos = new List<ShoppingListItemDto>();

            groupedDtos.AddRange(GroupShoppingListItems(checkedDtos));
            groupedDtos.AddRange(GroupShoppingListItems(uncheckedDtos));
            groupedDtos.AddRange(GroupShoppingListItems(manuallyAdded));

            return groupedDtos;
        }


        public void ClearShoppingList(int userId, LocalDate weekStart)
        {
            _repository.RemoveShoppingListItemsForDate(userId, weekStart, false);

            //TODO: Clear Cache
        }


        public void ClearManuallyAddedItems(int userId, LocalDate weekStart)
        {
            _repository.RemoveShoppingListItemsForDate(userId, weekStart, true);

            //TODO: Clear Cache
        }

        public void HandlePreparationRemoved(int userId, PreparationDto preparation)
        {
            var shoppingList = _repository.FetchShoppingListItemsForPreparation(preparation.Id);
            if (!shoppingList.Any())
            {
                return;
            }

            foreach(var item in shoppingList)
            {
                if(item.Checked)
                {
                    if(UpdateUnusedItem(userId, item.IngredientId, LocalDate.FromDateTime(preparation.Date), item.Amount))
                    {
                        _repository.RemoveShoppingListItemsById(new List<int> { item.Id });
                    }
                    else
                    {
                        item.PreparationId = null;
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

        private bool UpdateUnusedItem(int userId, int ingId, LocalDate weekStart, double amount)
        {
            var unused = _repository.GetShoppingListItems(userId, weekStart, false)
                .FirstOrDefault(i => i.PreparationId == null && i.IngredientId == ingId);
            
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

                var remainingAmount = ConsumeUnusedItems(userId, ingredient.IngredientId, weekStart, amount, preparation.Id);

                if(remainingAmount > 0)
                {
                    var item = GenerateShoppingListItem(userId, ingredient.IngredientId, weekStart, remainingAmount);
                    item.PreparationId = preparation.Id;

                    _repository.SaveItem(item);
                }
            }

            //TODO: Clear cache
        }

        private ShoppingListItem GenerateShoppingListItem(int userId, int ingredientId, LocalDate weekStart, double amount)
        {
            return new ShoppingListItem
            {
                UserId = userId,
                IngredientId = ingredientId,
                Amount = amount,
                NodaWeekStart = weekStart
            };
        }

        private double ConsumeUnusedItems(int userId, int ingredientId, LocalDate weekStart, double amount, int preparationId)
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

        private double ConsumeUnusedItem(ShoppingListItem item, double remainingAmount, int preparationId)
        {
            if (remainingAmount >= item.Amount)
            {
                remainingAmount -= item.Amount;

                item.PreparationId = preparationId;
                _repository.SaveItem(item);
                
                return remainingAmount;
            }
            else
            {
                var partOfUnused = new ShoppingListItem
                {
                    UserId = item.UserId,
                    Amount = remainingAmount,
                    IngredientId = item.IngredientId,
                    Checked = true,
                    ManuallyAdded = item.ManuallyAdded,
                    WeekStart = item.WeekStart,
                    PreparationId = preparationId,
                };
                item.Amount -= remainingAmount;
                _repository.SaveItems(new List<ShoppingListItem> {partOfUnused, item});

                return 0;
            }
        }

        private List<ShoppingListItem> GetUnusedItemsForIngredient(int userId, LocalDate weekStart, int ingredientId)
        {
            return GetUnusedItems(userId, weekStart).Where(i => i.IngredientId == ingredientId).ToList();
        }

        private List<ShoppingListItem> GetUnusedItems(int userId, LocalDate weekStart)
        {
            return _repository.GetShoppingListItems(userId, weekStart, false).Where(i => i.PreparationId == null).ToList();
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
            var item = _repository.FetchShoppingListItemsById(request.Ids).FirstOrDefault();
            
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
                if (item.PreparationId == null && !item.Checked)
                {
                    _repository.RemoveShoppingListItemsById(new List<int> { item.Id });
                }

                var uncheckedItems = _repository.FetchShoppingListItems(userId, item.WeekStart).Count(i => i.Checked == false);

                if (uncheckedItems == 0)
                {
                    var updateRequest = new UpdateJourneyProgressRequest
                    {
                        JourneyStepId = JOURNEY_SHOPPING_ID,
                        Completed = true
                    };
                    await _usersService.UpdateJourneyProgressAsync(userId, updateRequest);
                }
            }

            return success;
        }

        public async Task<bool> UpdateItemsAsync(int userId, List<ShoppingListItemDto> updatedItems)
        {
            var items = _repository.FetchShoppingListItemsById(updatedItems.SelectMany(i => i.Ids).ToList());

            if (items.Any(i => i.UserId != userId))
            {
                return false;
            }

            var changes = false;

            foreach (var item in items)
            {
                var updatedItem = updatedItems.First(i => i.Ids.Contains(item.Id));
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
                _repository.RemoveShoppingListItemsById(items.Where(item => item.PreparationId == null && !item.Checked).Select(i => i.Id).ToList());

                var weekStart = items.Select(i => i.WeekStart).First();
                var uncheckedItems = _repository.FetchShoppingListItems(userId, weekStart).Count(i => i.Checked == false);

                if (uncheckedItems == 0)
                {
                    var updateRequest = new UpdateJourneyProgressRequest
                    {
                        JourneyStepId = JOURNEY_SHOPPING_ID,
                        Completed = true
                    };
                    await _usersService.UpdateJourneyProgressAsync(userId, updateRequest);
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
            var measuredIngredient = new MeasuredIngredient
            {
                IngredientId = item.IngredientId,
                Quantity = item.Amount
            };
            _ingredientsService.NormalizeMeasuredIngredient(measuredIngredient);

            return new ShoppingListItemDto
            {
                Ids = new List<int> {item.Id},
                Checked = item.Checked,
                Unused = item.PreparationId == null,
                ManuallyAdded = item.ManuallyAdded,
                MeasuredIngredient = measuredIngredient,
                PreparationIds = item.PreparationId.HasValue ? new List<int>{item.PreparationId.Value} : new List<int>()
            };
        }

        public ShoppingListItem FromDto(ShoppingListItemDto dto)
        {
            return new ShoppingListItem
            {
                Id = dto.Ids.First(),
                Checked = dto.Checked,
                ManuallyAdded = dto.ManuallyAdded,
                IngredientId = dto.MeasuredIngredient.IngredientId,
                Amount = dto.MeasuredIngredient.Quantity,

                PreparationId = dto.PreparationIds.Any() ? dto.PreparationIds.First() : (int?)null
            };
        }
    }
}
