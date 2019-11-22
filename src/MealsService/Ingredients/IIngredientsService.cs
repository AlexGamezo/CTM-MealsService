using System.Collections.Generic;

using MealsService.Ingredients.Data;
using MealsService.Ingredients.Dtos;

namespace MealsService.Ingredients
{
    public interface IIngredientsService
    {
        IngredientDto GetIngredient(int ingredientId);
        List<IngredientDto> GetIngredients(List<int> ingredientIds);
        List<IngredientDto> ListIngredients();
        List<IngredientCategory> ListIngredientCategories();

        IngredientCategory GetCategoryByName(string name);

        List<IngredientDto> GetIngredientsByTags(List<string> tags);
        List<IngredientDto> SearchIngredients(string search);

        Ingredient SaveIngredient(IngredientDto ingredient);
        
        bool SetIngredientTags(int ingredientId, List<string> tags);

        IngredientCategory SaveIngredientCategory(IngredientCategory category);

        bool DeleteIngredient(int ingredientId);
        bool DeleteIngredientCategory(int categoryId);
        bool SetIngredientCategory(int id, string category);
        IngredientCategory GetOrCreateCategoryByName(string category);

        void NormalizeMeasuredIngredient(MeasuredIngredient ingredient);
        MeasuredIngredient GroupMeasuredIngredients(List<MeasuredIngredient> ingredients);
        void DenormalizeMeasuredIngredient(MeasuredIngredient measuredIngredient);
    }
}