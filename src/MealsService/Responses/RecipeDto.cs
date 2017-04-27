using System.Collections.Generic;
using MealsService.Models;


namespace MealsService.Responses
{
    public class RecipeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Brief { get; set; }

        public string Image { get; set; }

        public int PrepTime { get; set; }
        public int CookTime { get; set; }

        public string MealType { get; set; }

        public List<RecipeIngredientDto> Ingredients {get; set; }

        public List<RecipeStep> Steps { get; set; }
        public List<int> DietTypes { get; set; }
    }
}
