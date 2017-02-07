using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MealsService.Models;

namespace MealsService.Responses
{
    public class MealDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Brief { get; set; }

        public string Image { get; set; }

        public int PrepTime { get; set; }
        public int CookTime { get; set; }

        public string MealType { get; set; }

        public List<MealIngredientDto> Ingredients {get; set; }
    }
}
