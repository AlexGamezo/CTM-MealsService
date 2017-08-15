
using System.Collections.Generic;

namespace MealsService.Requests
{
    public class CreateIngredientRequest
    {
        public string Name;
        public string Brief;
        public string Description;
        public string Category;
        public List<string> Tags;
    }
}
