using System.Collections.Generic;
using MealsService.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MealsService.Requests;

namespace MealsService.Services
{
    public class IngredientsService
    {
        private MealsDbContext _dbContext;

        public IngredientsService(MealsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Ingredient GetIngredient(int ingredient)
        {
            return _dbContext.Ingredients.FirstOrDefault(t => t.Id == ingredient);
        }

        public bool Create(CreateIngredientRequest request)
        {
            var ingredient = new Ingredient
            {
                Name = request.Name,
                Brief = request.Brief,
                Description = request.Description,
            };

            var ingredientCategory = _dbContext.IngredientCategories
                .FirstOrDefault(c => c.Name == request.Category);

            if (ingredientCategory == null)
            {
                ingredient.Category = new IngredientCategory
                {
                    Name = request.Category
                };
            }
            else
            {
                ingredient.Category = ingredientCategory;
            }

            _dbContext.Ingredients.Add(ingredient);

            return _dbContext.SaveChanges() > 0;
        }

        public bool Update(UpdateIngredientRequest request)
        {
            var ingredient = _dbContext.Ingredients.FirstOrDefault(t => t.Id == request.Id);

            ingredient.Name = request.Name;
            ingredient.Description = request.Description;
            ingredient.Brief = request.Brief;

            if (request.Category != ingredient.Category.Name)
            {
                var ingredientCategory = _dbContext.IngredientCategories
                    .FirstOrDefault(c => c.Name == request.Category);

                if (ingredientCategory == null)
                {
                    ingredient.Category = new IngredientCategory
                    {
                        Name = request.Category
                    };
                }
                else
                {
                    ingredient.Category = ingredientCategory;
                }
            }

            return _dbContext.Entry(ingredient).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0;
        }

        public IEnumerable<Ingredient> GetIngredients(string search = "")
        {
            IQueryable<Ingredient> ingredients = _dbContext.Ingredients;

            if (search != "")
            {
                ingredients = ingredients.Where(i => i.Name.Contains(search));
            }
            
            return ingredients.ToList();
        }

        //TODO: Add cache layer
        public IEnumerable<IngredientCategory> GetIngredientCategories()
        {
            return _dbContext.IngredientCategories.ToList();
        }
    }
}
