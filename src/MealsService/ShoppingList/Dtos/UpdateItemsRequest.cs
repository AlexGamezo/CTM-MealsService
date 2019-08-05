using System.Collections.Generic;

namespace MealsService.ShoppingList.Dtos
{
    public class UpdateItemsRequest
    {
        public List<ShoppingListItemDto> Items { get; set; }
    }
}
