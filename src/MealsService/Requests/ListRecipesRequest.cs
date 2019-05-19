
using System.Collections.Generic;
using MealsService.Ingredients.Data;
using MealsService.Recipes.Data;

namespace MealsService.Requests
{
    public class ListRecipesRequest
    {
        public string Search = "";
        public int Limit = 10;
        public int Offset = 0;

        public MealType MealType = MealType.Any;

        public MeasureSystem MeasureSystem;

        public List<int> IngredientIds = new List<int>();
        public bool AllIngredients = false;

        public List<int> RecipeIds = new List<int>();

        public bool IncludeDeleted = false;

        public int UserId;
    }
}
