
using MealsService.Recipes.Data;

namespace MealsService.Recipes.Dtos
{
    public class RecipeIngredientDto
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public string Name { get; set; }
        public float Quantity { get; set; }
        public string Measure { get; set; }
        public int MeasureTypeId { get; set; }
        public string Category { get; set; }
        public string Image { get; set; }
    }

    public static class RecipeIngredientExtensions
    {
        public static RecipeIngredientDto ToDto(this RecipeIngredient recipeIngredient)
        {
            if (recipeIngredient == null)
            {
                return null;
            }

            return new RecipeIngredientDto
            {
                Id = recipeIngredient.Id,
                IngredientId = recipeIngredient.IngredientId,
                Quantity = recipeIngredient.Amount,
                Measure = recipeIngredient.MeasureType?.Name ?? recipeIngredient.AmountType,
                MeasureTypeId = recipeIngredient.MeasureTypeId,
                Name = recipeIngredient.Ingredient.Name,
                Image = recipeIngredient.Ingredient.Image,
                Category = recipeIngredient.Ingredient.Category
            };
        }

        public static RecipeIngredient FromDto(this RecipeIngredientDto recipeIngredientDto)
        {
            if (recipeIngredientDto == null)
            {
                return null;
            }

            return new RecipeIngredient
            {
                Id = recipeIngredientDto.Id,
                IngredientId = recipeIngredientDto.IngredientId,
                Amount = recipeIngredientDto.Quantity,
                AmountType = recipeIngredientDto.Measure,
                MeasureTypeId = recipeIngredientDto.MeasureTypeId,
            };
        }
    }
}
