using System.Collections.Generic;

namespace MealsService.Requests
{
    public class RecipeListRequest
    {
        public int Limit { get; set; } = 10;
        public int Offset { get; set; } = 0;

        public bool IncludeDeleted { get; set; } = false;

        public List<int> RecipeIds { get; set; } = new List<int>();

        public int UserId { get; set; }
    }
}
