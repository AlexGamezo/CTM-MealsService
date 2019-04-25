using MealsService.Recipes.Data;

namespace MealsService.Recipes.Dtos
{
    public class RecipeVoteResponse
    {
        public int RecipeId { get; set; }
        public RecipeVote.VoteType Vote { get; set; }

        public bool JourneyUpdated { get; set; }
    }
}
