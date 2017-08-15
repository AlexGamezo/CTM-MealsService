
using MealsService.Recipes.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MealsService.Recipes.Dtos
{
    public class RecipeVoteDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public RecipeVote.VoteType Vote { get; set; }
        public int RecipeId { get; set; }
    }
}
