using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MealsService.Responses
{
    public class MealIngredientDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public float Quantity { get; set; }
        public string Measure { get; set; }
    }
}
