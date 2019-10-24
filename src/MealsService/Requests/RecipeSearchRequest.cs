using System.Collections.Generic;
using MealsService.Recipes.Data;

namespace MealsService.Requests
{
    public class RecipeSearchRequest
    {
        public string Search { get; set; }
        public int Limit { get; set; }
        public int Skip { get; set; }
        public bool IncludeDeleted { get; set; }

        public List<int> IngredientIds { get; set; }
        public bool AllIngredients { get; set; }
        public MealType MealType { get; set; }
    }
}
