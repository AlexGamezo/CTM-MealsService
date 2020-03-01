using System.Collections.Generic;
using System.Threading.Tasks;

using NodaTime;

using MealsService.Schedules.Dtos;
using MealsService.ShoppingList.Data;
using MealsService.ShoppingList.Dtos;

namespace MealsService.ShoppingList
{
    public interface IShoppingListService
    {
        Task<List<ShoppingListItemDto>> GetGroupedShoppingListAsync(int userId, LocalDate weekStart);
        Task<List<ShoppingListItemDto>> GetShoppingListForPreparationAsync(int userId, int prepId);
        void ClearShoppingList(int userId, LocalDate weekStart);
        void ClearManuallyAddedItems(int userId, LocalDate weekStart);

        Task<List<ShoppingListItem>> GetShoppingListAsync(int userId, LocalDate weekStart);

        //Task GenerateShoppingListAsync(int userId, LocalDate weekStart);
        //void HandlePreparationRemoved(int userId, PreparationDto preparation);
        void HandlePreparationsRemoved(int userId, List<PreparationDto> preparations);
        //void HandlePreparationAdded(int userId, PreparationDto preparation);
        void HandlePreparationsAdded(int userId, List<PreparationDto> preparations, LocalDate weekStart);
        
        Task<ShoppingListItem> AddItemAsync(int userId, LocalDate weekStart, ShoppingListItemDto dto);
        Task<bool> UpdateItemAsync(int userId, ShoppingListItemDto request);
        Task<bool> UpdateItemsAsync(int userId, List<ShoppingListItemDto> updatedItems);
        bool RemoveItem(int userId, int id);
        
        ShoppingListItemDto ToDto(ShoppingListItem item);
        ShoppingListItem FromDto(ShoppingListItemDto dto);
    }
}