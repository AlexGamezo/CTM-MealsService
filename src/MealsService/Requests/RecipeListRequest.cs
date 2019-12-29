using System.Collections.Generic;

namespace MealsService.Requests
{
    public class RecipeListRequest
    {
        public int Limit = 10;
        public int Offset = 0;

        public bool IncludeDeleted = false;

        public List<int> RecipeIds = new List<int>();

        public int UserId;
    }
}
