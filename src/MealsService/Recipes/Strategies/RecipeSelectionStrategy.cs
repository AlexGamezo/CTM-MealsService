using System.Collections.Generic;
using MealsService.Recipes.Data;

namespace MealsService.Recipes.Strategies
{
    public abstract class RecipeSelectionStrategy
    {
        public RecipeSelectionStrategy()
        {

        }

        public Recipe SelectRecipe()
        {
            return null;
        }
    }
}
