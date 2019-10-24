using System.Collections.Generic;

using MealsService.Ingredients.Data;

namespace MealsService.Ingredients
{
    public interface IIngredientsRepository
    {
        List<Ingredient> ListIngredients();
        List<IngredientCategory> ListIngredientCategories();
        bool SaveIngredient(Ingredient ingredient);
        bool SaveIngredientCategory(IngredientCategory category);
        bool SetTags(int ingredientId, List<string> tags);
        bool DeleteIngredientById(int ingredientId);
        bool DeleteIngredientCategoryById(int categoryId);
    }
}
