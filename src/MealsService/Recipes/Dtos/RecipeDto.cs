using System.Collections.Generic;
using System.Linq;
using MealsService.Recipes.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MealsService.Recipes.Dtos
{
    public class RecipeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Brief { get; set; }
        public string Slug { get; set; }

        public string Image { get; set; }

        public int PrepTime { get; set; }
        public int CookTime { get; set; }
        public int NumServings { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MealType MealType { get; set; }

        public string Source { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public RecipeVote.VoteType Vote { get; set; }

        public List<RecipeIngredientDto> Ingredients {get; set; }

        public List<RecipeStep> Steps { get; set; }
        public List<int> DietTypes { get; set; }

        public int Priority { get; set; }

        public bool IsDeleted { get; set; }
    }

    public static class RecipeExtensions
    {
        public static RecipeDto ToDto(this Recipe recipe)
        {
            if (recipe == null)
            {
                return null;
            }

            return new RecipeDto
            {
                Id = recipe.Id,
                Name = recipe.Name,
                Brief = recipe.Brief,
                Description = recipe.Description,
                Image = recipe.Image,
                CookTime = recipe.CookTime,
                PrepTime = recipe.PrepTime,
                NumServings = recipe.NumServings,
                MealType = recipe.MealType,
                Source = recipe.Source,
                Ingredients = recipe.RecipeIngredients?.Select(i => i.ToDto())
                    .OrderByDescending(i => !string.IsNullOrEmpty(i.Image))
                    .ToList(),
                Steps = recipe.Steps
                    .OrderBy(s => s.Order)
                    .ToList(),
                DietTypes = recipe.RecipeDietTypes?.Select(mdt => mdt.DietTypeId).ToList(),
                Vote = recipe.Votes != null && recipe.Votes.Any() ? recipe.Votes.First().Vote : RecipeVote.VoteType.UNKNOWN,
                Slug = recipe.Slug,
                Priority = recipe.Priority,
                IsDeleted = recipe.Deleted,
            };
        }

        public static Recipe FromDto(this RecipeDto recipeDto)
        {
            if (recipeDto == null)
            {
                return null;
            }

            return new Recipe
            {

                Id = recipeDto.Id,
                Name = recipeDto.Name,
                Brief = recipeDto.Brief,
                Description = recipeDto.Description,
                Image = recipeDto.Image,
                CookTime = recipeDto.CookTime,
                PrepTime = recipeDto.PrepTime,
                NumServings = recipeDto.NumServings,
                MealType = recipeDto.MealType,
                Source = recipeDto.Source,
                RecipeIngredients = recipeDto.Ingredients?.Select(i => i.FromDto())
                    .ToList(),
                Steps = recipeDto.Steps
                    .OrderBy(s => s.Order)
                    .ToList(),
                RecipeDietTypes = recipeDto.DietTypes?.Select(mdt => new RecipeDietType{ RecipeId = recipeDto.Id, DietTypeId = mdt}).ToList(),
                Slug = recipeDto.Slug,
                Priority = recipeDto.Priority,
                Deleted = recipeDto.IsDeleted
            };
        }
    }
}
