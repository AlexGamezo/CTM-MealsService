using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.ShoppingList.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;

namespace MealsService.ShoppingList
{
    public class ShoppingListRepository
    {
        private IServiceProvider _serviceContainer;

        public ShoppingListRepository(IServiceProvider serviceContainer)
        {
            _serviceContainer = serviceContainer;
        }

        public List<ShoppingListItem> FetchShoppingListItems(int userId, DateTime weekStartUnspecified)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            var items = dbContext.ShoppingListItems
                .Where(i => i.UserId == userId && i.WeekStart == weekStartUnspecified)
                .Include(s => s.Ingredient)
                .ThenInclude(i => i.IngredientCategory)
                .Include(s => s.MeasureType)
                .ToList();
            return items;
        }

        public List<ShoppingListItem> FetchShoppingListItemsById(List<int> shopItemIds)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            return dbContext.ShoppingListItems
                .Where(i => shopItemIds.Contains(i.Id))
                .Include(s => s.Ingredient)
                .ThenInclude(i => i.IngredientCategory)
                .Include(s => s.MeasureType)
                .ToList();
        }

        public List<ShoppingListItem> FetchShoppingListItemsForPreparation(int preparationId)
        {
            return FetchShoppingListItemsForPreparations(new List<int> {preparationId});
        }

        public List<ShoppingListItem> FetchShoppingListItemsForPreparations(List<int> preparationIds)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            var prepShoppingItems = dbContext.ShoppingListItems
                .Where(i => i.PreparationId != null && preparationIds.Contains(i.PreparationId.Value))
                .Include(s => s.Ingredient)
                    .ThenInclude(i => i.IngredientCategory)
                .Include(s => s.MeasureType)
                .ToList();
            return prepShoppingItems;
        }

        public List<ShoppingListItem> GetShoppingListItems(int userId, LocalDate weekStart, bool includeManuals)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            var weekStartUnspecified = weekStart.ToDateTimeUnspecified();

            return dbContext.ShoppingListItems
                .Where(i => i.UserId == userId && i.WeekStart == weekStartUnspecified && (includeManuals || !i.ManuallyAdded))
                .Include(s => s.Ingredient)
                    .ThenInclude(i => i.IngredientCategory)
                .Include(s => s.MeasureType)
                .ToList();
        }

        public void RemoveShoppingListItemsForDate(int userId, LocalDate weekStartUnspecified, bool includeManuals)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();
            var items = GetShoppingListItems(userId, weekStartUnspecified, includeManuals);

            dbContext.ShoppingListItems.RemoveRange(items);
            dbContext.SaveChanges();
        }

        public void RemoveShoppingListItemsById(List<int> itemIds)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            dbContext.RemoveRange(dbContext.ShoppingListItems.Where(i => itemIds.Contains(i.Id)));
        }

        internal bool SaveItem(ShoppingListItem item)
        {
            return SaveItems(new List<ShoppingListItem> { item });
        }

        internal bool SaveItems(List<ShoppingListItem> items)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            foreach (var item in items)
            {
                if (item.Id > 0)
                {
                    var tracked = dbContext.ChangeTracker.Entries<ShoppingListItem>()
                        .FirstOrDefault(m => m.Entity.Id == item.Id);
                    if (tracked != null)
                    {
                        dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                    }
                    dbContext.ShoppingListItems.Attach(item);
                    dbContext.Entry(item).State = EntityState.Modified;
                }
                else
                {
                    dbContext.ShoppingListItems.Add(item);
                }
            }

            return dbContext.SaveChanges() > 0;
        }
    }
}
