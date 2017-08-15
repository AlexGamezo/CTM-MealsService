
using System.Collections.Generic;

namespace MealsService.Requests
{
    public class UpdateIngredientRequest
    {
        public int Id;
        public string Name;
        public string Brief;
        public string Description;
        public string Category;
        public List<string> Tags;
    }
}
