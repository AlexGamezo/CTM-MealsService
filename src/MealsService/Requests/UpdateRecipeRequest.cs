
using System.Collections.Generic;

using MealsService.Recipes.Dtos;
using MealsService.Recipes.Data;

namespace MealsService.Requests
{
    public class UpdateRecipeRequest
    {
        public string Name;
        public string Slug;

        public string Brief;
        public string Description;

        public int CookTime;
        public int PrepTime;
        public int NumServings;

        public string Image;

        public string MealType;
        public List<int> DietTypeIds;
        public List<RecipeIngredientDto> Ingredients;
        public List<RecipeStep> Steps;
        public string Source;
    }
}
