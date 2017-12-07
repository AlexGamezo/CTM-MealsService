using System;
using System.Collections.Generic;
using System.Linq;
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

        public List<ShoppingListItem> GetShoppingList(int userId, DateTime weekStart)
        {
            var items = _dbContext.ShoppingListItems.Where(i => i.UserId == userId && i.WeekStart == weekStart)
                .Include(s => s.Ingredient)
                    .ThenInclude(i => i.IngredientCategory)
                .Include(s => s.MeasureType)
                .ToList();

            if (!items.Any())
            {
                GenerateShoppingList(userId, weekStart);

                items = _dbContext.ShoppingListItems.Where(i => i.UserId == userId && i.WeekStart == weekStart)
                    .Include(s => s.Ingredient)
                    .ToList();
            }

            return items;
        }

        public void ClearShoppingList(int userId, DateTime weekStart)
        {
            var items = _dbContext.ShoppingListItems.Where(i => i.WeekStart == weekStart).ToList();

            _dbContext.ShoppingListItems.RemoveRange(items);
            _dbContext.SaveChanges();
        }

        public void GenerateShoppingList(int userId, DateTime weekStart)
        {
            var schedule = _scheduleService.GetSchedule(userId, weekStart);

            //MealId => List<ScheduleSlot>
            var mealIds = schedule
                .SelectMany(d => d.ScheduleSlots.Where(s => s.MealId > 0))
                .GroupBy(id => id.MealId);

            //MealId => Recipe
            var meals = _recipesService
                .FindRecipes(mealIds.Select(g => g.Key), userId)
                .ToDictionary(r => r.Id);

            //MealIngredientId => MealId
            var mealIngredientMap = meals.Values.SelectMany(m =>
                m.MealIngredients.Select(mi => new KeyValuePair<int, int>(mi.Id, m.Id)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var listItems = new List<ShoppingListItem>();

            //IngredientId => List<MealIngredient>
            var ingredients = meals.Values
                .SelectMany(m => m.MealIngredients)
                .GroupBy(mi => mi.IngredientId);


            foreach (var group in ingredients)
            {
                //List of MealIngredients, grouped by MeasureType
                foreach (var measureGroup in group.GroupBy(i => i.MeasureTypeId))
                {
                    var listItem = new ShoppingListItem
                    {
                        UserId = userId,
                        IngredientId = group.Key,
                        MeasureTypeId = measureGroup.Key,
                        Amount = measureGroup.Sum(mi => mi.Amount),
                        IngredientName = measureGroup.First().Ingredient.Name,
                        ScheduleSlots = measureGroup.SelectMany(mi =>
                        {
                            var mealId = mealIngredientMap[mi.Id];
                            var slot = mealIds.FirstOrDefault(kvp => kvp.Key == mealId);
                            return slot.Select(g =>
                                new ShoppingListItemScheduleSlot
                                {
                                    ScheduleSlotId = g.Id
                                });
                        }).ToList(),
                        WeekStart = weekStart
                    };

                    listItems.Add(listItem);
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
