using System.Collections.Generic;

using MealsService.Recipes.Data;

namespace MealsService.Recipes
{

    public interface IUserRecipeRepository
    {
        List<RecipeVote> GetUserVotes(int userId);
        bool SaveVote(RecipeVote vote);
        List<RecipeVote> GetRecipeVotes(int recipeId);
    }
}
