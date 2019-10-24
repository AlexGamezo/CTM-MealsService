using MealsService.Ingredients.Data;
using System.Collections.Generic;

namespace MealsService.Ingredients.Dtos
{
    public class IngredientDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Brief { get; set; }

        public string Description { get; set; }

        public string Image { get; set; }
        public string Category { get; set; }

        public string MeasurementType { get; set; }

        public List<string> Tags { get; set; }
    }

    public static class IngredientDtoExtensions
    {
        public static IngredientDto ToDto(this Ingredient ingredient)
        {
            return new IngredientDto
            {
                Id = ingredient.Id,
                Name = ingredient.Name.ToLowerInvariant(),
                Brief = ingredient.Brief,
                Description = ingredient.Description,
                Image = ingredient.Image,
                Category = ingredient.Category,
                MeasurementType = ingredient.MeasurementType,
                Tags = ingredient.Tags
            };
        }

        public static Ingredient FromDto(this IngredientDto ingredientDto)
        {
            return new Ingredient
            {
                Id = ingredientDto.Id,
                Name = ingredientDto.Name.ToLowerInvariant(),
                Brief = ingredientDto.Brief,
                Description = ingredientDto.Description,
                Image = ingredientDto.Image,
                MeasurementType = ingredientDto.MeasurementType,
                //Individual Sets
                /*Category = ingredientDto.Category,
                Tags = ingredientDto.Tags*/
            };
        }
    }
}
