using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.Models;
using MealsService.Recipes;
using MealsService.Services;
using MealsService.ShoppingList.Data;
using MealsService.ShoppingList.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MealsService.ShoppingList
{
    public class ShoppingListService
    {
        private MealsDbContext _dbContext;

        private ScheduleService _scheduleService;
        private RecipesService _recipesService;

        public ShoppingListService(MealsDbContext dbContext, ScheduleService scheduleService, RecipesService recipesService)
        {
            _dbContext = dbContext;

            _scheduleService = scheduleService;
            _recipesService = recipesService;
        }

        public List<ShoppingListItem> GetShoppingList(int userId, DateTime weekStart, bool regenIfEmpty = true)
        {
            var items = _dbContext.ShoppingListItems
                .Where(i => i.UserId == userId && i.WeekStart == weekStart)
                .Include(s => s.Ingredient)
                    .ThenInclude(i => i.IngredientCategory)
                .Include(s => s.MeasureType)
                .ToList();

            if (items.All(i => i.ManuallyAdded) && regenIfEmpty)
            {
                GenerateShoppingList(userId, weekStart);

                return GetShoppingList(userId, weekStart, false);
            }

            return items;
        }

        public void ClearShoppingList(int userId, DateTime weekStart, bool includeManuals = false)
        {
            var items = _dbContext.ShoppingListItems
                .Where(i => i.WeekStart == weekStart && (includeManuals || !i.ManuallyAdded))
                .ToList();

            _dbContext.ShoppingListItems.RemoveRange(items);
            _dbContext.SaveChanges();
        }

        public void GenerateShoppingList(int userId, DateTime weekStart)
        {
            var schedule = _scheduleService.GetSchedule(userId, weekStart);

            ClearShoppingList(userId, weekStart);
            if (schedule.Any(d => d.ScheduleSlots.Any(s => s.MealId > 0)))
            {
                HandleSlotsAdded(userId, schedule.SelectMany(d => d.ScheduleSlots).ToList(), weekStart);
            }
        }

        public void HandleSlotsRemoved(int userId, List<ScheduleSlot> slots)
        {
            if (slots.All(s => s.MealId == 0))
            {
                return;
            }

            var slotIds = slots.Select(s => s.Id).ToList();
            var recipes = _recipesService.GetRecipes(slots.Select(s => s.MealId).ToList()).ToDictionary(r => r.Id);
            var slotRecipes = slots.Where(s => s.MealId != 0).ToDictionary(s => s.Id, s => recipes[s.MealId]);

            var associations = _dbContext.ShoppingListItemScheduleSlots
                .Include(i => i.ShoppingListItem)
                .Where(i => slotIds.Contains(i.ScheduleSlotId))
                .ToList();

            var unusedIngredients = new List<ShoppingListItem>();
            
            foreach (var assoc in associations)
            {
                if (!slotRecipes.ContainsKey(assoc.ScheduleSlotId))
                {
                    continue;
                }

                var ingredient = slotRecipes[assoc.ScheduleSlotId].Ingredients
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
                                WeekStart = assoc.ShoppingListItem.WeekStart,
                                Unused = true
                            };
                            unusedIngredients.Add(unused);
                        }

                        unused.Amount += ingredient.Quantity;
                    }

                    assoc.ShoppingListItem.Amount -= ingredient.Quantity;
                    
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

            if (associations.Any())
            {
                _dbContext.RemoveRange(associations);
            }

            _dbContext.SaveChanges();
        }

        public void HandleSlotsAdded(int userId, List<ScheduleSlot> slots, DateTime weekStart)
        {
            //Make sure the shopping list has been generated before making changes to it
            GetShoppingList(userId, weekStart);

            var mealIds = slots.Where(s => s.MealId > 0)
                .GroupBy(id => id.MealId).ToList();

            //MealId => Recipe
            var meals = _recipesService.FindRecipes(mealIds.Select(g => g.Key), userId).ToDictionary(r => r.Id);

            //MealIngredientId => MealId
            var mealIngredientMap = meals.Values
                .SelectMany(m => m.MealIngredients.Select(mi => new KeyValuePair<int, int>(mi.Id, m.Id)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var listItems = new List<ShoppingListItem>();

            //IngredientId => List<MealIngredient>
            var ingredients = meals.Values.SelectMany(m => m.MealIngredients).GroupBy(mi => mi.IngredientId);

            foreach (var group in ingredients)
            {
                //List of MealIngredients, grouped by MeasureType
                foreach (var measureGroup in group.GroupBy(i => i.MeasureTypeId))
                {
                    var items = _dbContext.ShoppingListItems.Where(s =>
                            s.UserId == userId && s.IngredientId == group.Key && s.MeasureTypeId == measureGroup.Key &&
                            s.WeekStart == weekStart)
                        .OrderByDescending(i => i.Unused)
                        .ToList();

                    var item = items.FirstOrDefault(i => !i.Unused && !i.Checked);
                    var checkedItem = items.FirstOrDefault(i => !i.Unused && i.Checked);
                    var unused = items.FirstOrDefault(i => i.Unused);
                    var amountNeeded = measureGroup.Sum(mi => mi.Amount);

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
                                WeekStart = weekStart,
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
                                WeekStart = weekStart
                            };

                            listItems.Add(item);
                        }
                        else if (checkedItem != null)
                        {
                            item = checkedItem;
                        }
                    }
                    
                    if (item.ScheduleSlots == null)
                    {
                        item.ScheduleSlots = new List<ShoppingListItemScheduleSlot>();
                    }
                    //It's already been added
                    if (item.Id != checkedItem?.Id)
                    {
                        item.Amount += measureGroup.Sum(mi => mi.Amount);
                    }

                    item.ScheduleSlots.AddRange(measureGroup.SelectMany(mi =>
                    {
                        var mealId = mealIngredientMap[mi.Id];
                        var slot = mealIds.FirstOrDefault(kvp => kvp.Key == mealId);
                        return slot.Select(g =>
                            new ShoppingListItemScheduleSlot
                            {
                                ScheduleSlotId = g.Id
                            });
                    }));
                }
            }

            _dbContext.ShoppingListItems.AddRange(listItems);
            _dbContext.SaveChanges();
        }

        public ShoppingListItem AddItem(int userId, DateTime weekStart, ShoppingListItemDto dto)
        {
            var item = FromDto(dto);

            item.WeekStart = weekStart;

            _dbContext.ShoppingListItems.Add(item);

            if (_dbContext.SaveChanges() > 0)
            {
                return item;
            }

            return null;
        }

        public bool UpdateItem(int userId, ShoppingListItemDto request)
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

            return !changes || _dbContext.SaveChanges() > 0;
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
