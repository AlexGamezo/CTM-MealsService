
using System.Collections.Generic;
using MealsService.Ingredients.Data;

namespace MealsService.ShoppingList.Dtos
{
    public class ShoppingListItemDto
    {
        public List<int> Ids { get; set; }

        public MeasuredIngredient MeasuredIngredient { get; set; }
        
        public bool ManuallyAdded { get; set; }
        public bool Checked { get; set; }
        public bool Unused { get; set; }
        public List<int> PreparationIds { get; set; }
    }
}
