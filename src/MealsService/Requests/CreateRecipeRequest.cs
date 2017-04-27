
using System.Collections.Generic;
using MealsService.Models;
using MealsService.Responses;

namespace MealsService.Requests
{
    public class CreateRecipeRequest
    {
        public string Name;
        public string Brief;
        public string Description;
        public int CookTime;
        public int PrepTime;
        public string Image;
        public string MealType;
        public List<int> DietTypeIds;
        public List<RecipeIngredientDto> Ingredients;
        public List<RecipeStep> Steps;
    }
}
