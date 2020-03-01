using System.Collections.Generic;

using MealsService.Recipes.Data;
using MealsService.Recipes.Dtos;
using MealsService.Requests;

namespace MealsService.Recipes
{
    public interface IRecipesService
    {
        List<RecipeDto> ListRecipes(RecipeListRequest request = null);
        List<RecipeDto> SearchRecipes(RecipeSearchRequest request);
        RecipeDto GetRecipe(int recipeId);
        RecipeDto GetRecipeBySlug(string slug);

        List<Recipe> FindRecipesById(IEnumerable<int> ids);
        Recipe FindRecipeById(int id);
        List<Recipe> FindRecipesBySlug(IEnumerable<string> slugs);
        Recipe FindRecipeBySlug(string slug);
        RecipeDto SaveRecipe(RecipeDto recipe);
        bool DeleteRecipe(int recipeId);
    }
}