
namespace MealsService.ShoppingList.Dtos
{
    public class ShoppingListItemDto
    {
        public int Id { get; set; }
        
        public int IngredientId { get; set; }
        public string Name { get; set; }
        public float Quantity { get; set; }
        public string Measure { get; set; }
        public int MeasureTypeId { get; set; }
        public string Category { get; set; }
        public string Image { get; set; }

        public bool ManuallyAdded { get; set; }
        public bool Checked { get; set; }
        public bool Unused { get; set; }
        public int PreparationId { get; set; }
    }
}
