using System.Collections.Generic;
using System.Threading.Tasks;

using MealsService.Recipes.Data;
using MealsService.Recipes.Dtos;

namespace MealsService.Recipes
{
    public interface IUserRecipesService
    {
        Task<List<RecipeVoteDto>> ListRecipeVotesAsync(int userId);
        Task<bool> AddRecipeVoteAsync(int userId, int recipeId, RecipeVote.VoteType vote);
        Task<bool> PopulateRecipeVotesAsync(List<RecipeDto> recipes, int userId);
        Task<RecipeDto> GetRandomRecipeAsync(RandomRecipeRequest request, int userId, bool retry = false);
    }
}