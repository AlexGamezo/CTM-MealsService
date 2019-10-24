using System.Collections.Generic;

using MealsService.Recipes.Data;
using MealsService.Recipes.Dtos;

namespace MealsService.Recipes
{
    public interface IRecipeRepository
    {
        List<Recipe> ListRecipes();
        List<Recipe> ListRecipesWithDeleted();
        bool DeleteRecipe(int recipeId);
        bool SaveRecipe(Recipe recipe);
        bool SetDietTypes(int recipeId, List<int> dietTypeIds);
        bool SetRecipeIngredients(int recipeId, List<RecipeIngredientDto> ingredients);
        bool SetRecipeSteps(int recipeId, List<RecipeStep> steps);

    }
}
