
namespace MealsService.Recipes.Dtos
{
    public class RecipeIngredientDto
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public string Name { get; set; }
        public float Quantity { get; set; }
        public string Measure { get; set; }
        public string Category { get; set; }
        public string Image { get; set; }
    }
}
