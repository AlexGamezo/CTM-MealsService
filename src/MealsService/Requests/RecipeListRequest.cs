namespace MealsService.Requests
{
    public class RecipeListRequest
    {
        public int Limit = 10;
        public int Offset = 0;

        public bool IncludeDeleted = false;

        public int UserId;
    }
}
