using MealsService.Ingredients.Data;
using MealsService.Recipes.Data;

namespace MealsService.Recipes.Dtos
{
    public class RecipeIngredientDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public MeasuredIngredient MeasuredIngredient { get; set; }
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
                Name = recipeIngredient.Ingredient.Name,
                MeasuredIngredient = new MeasuredIngredient
                {
                    IngredientId = recipeIngredient.IngredientId, Quantity = recipeIngredient.Amount
                }
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
                IngredientId = recipeIngredientDto.MeasuredIngredient.IngredientId,
                Amount = (float)recipeIngredientDto.MeasuredIngredient.Quantity
            };
        }
    }
}
