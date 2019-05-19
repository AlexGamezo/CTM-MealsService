using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MealsService.Ingredients.Data;

namespace MealsService.Ingredients
{
    public class IngredientsRepository
    {
        private IServiceProvider _serviceContainer;

        public IngredientsRepository(IServiceProvider serviceContainer)
        {
            _serviceContainer = serviceContainer;
        }

        public List<Ingredient> ListIngredients()
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            return dbContext.Ingredients
                .Include(i => i.IngredientTags)
                .ThenInclude(it => it.Tag)
                .Include(i => i.IngredientMeasureTypes)
                .Include(i => i.IngredientCategory)
                .ToList(); ;
        }

        public List<IngredientCategory> ListIngredientCategories()
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            return dbContext.IngredientCategories
                .ToList();
        }

        public bool SaveIngredient(Ingredient ingredient)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            if (ingredient.Id > 0)
            {
                var tracked = dbContext.ChangeTracker.Entries<Ingredient>()
                    .FirstOrDefault(m => m.Entity.Id == ingredient.Id);
                if (tracked != null)
                {
                    dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                }
                dbContext.Ingredients.Attach(ingredient);
                dbContext.Entry(ingredient).State = EntityState.Modified;
            }
            else
            {
                dbContext.Ingredients.Add(ingredient);
            }

            return dbContext.SaveChanges() > 0;
        }

        public bool SetTags(Ingredient ingredient, List<int> tags)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();
            var changes = false;

            for (var i = 0; i < tags.Count; i++)
            {
                if (ingredient.IngredientTags?.Count > i)
                {
                    if (ingredient.IngredientTags[i].TagId != tags[i])
                    {
                        ingredient.IngredientTags[i].TagId = tags[i];
                        changes = true;
                    }
                }
                else
                {
                    changes = true;
                    if (ingredient.IngredientTags == null)
                    {
                        ingredient.IngredientTags = new List<IngredientTag>();
                    }
                    ingredient.IngredientTags.Add(new IngredientTag { TagId = tags[i] });
                }
            }
            if (tags.Count < ingredient.IngredientTags.Count)
            {
                var countToRemove = ingredient.IngredientTags.Count - tags.Count;
                var toDelete = ingredient.IngredientTags.GetRange(tags.Count, countToRemove);

                changes = true;
                dbContext.IngredientTags.RemoveRange(toDelete);
            }

            return !changes || dbContext.SaveChanges() > 0;
        }

        public bool SetMeasurementTypes(Ingredient ingredient, List<int> measurements)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();
            var changes = false;

            for (var i = 0; i < measurements.Count; i++)
            {
                if (ingredient.IngredientMeasureTypes?.Count > i)
                {
                    if (ingredient.IngredientMeasureTypes[i].MeasureTypeId != measurements[i])
                    {
                        ingredient.IngredientMeasureTypes[i].MeasureTypeId = measurements[i];
                        changes = true;
                    }
                }
                else
                {
                    changes = true;
                    if (ingredient.IngredientMeasureTypes == null)
                    {
                        ingredient.IngredientMeasureTypes = new List<IngredientMeasureType>();
                    }
                    ingredient.IngredientMeasureTypes.Add(new IngredientMeasureType { MeasureTypeId = measurements[i] });
                }
            }
            if (measurements.Count < ingredient.IngredientMeasureTypes.Count)
            {
                var countToRemove = ingredient.IngredientMeasureTypes.Count - measurements.Count;
                var toDelete = ingredient.IngredientMeasureTypes.GetRange(measurements.Count, countToRemove);

                changes = true;
                dbContext.IngredientMeasureTypes.RemoveRange(toDelete);
            }

            return !changes || dbContext.SaveChanges() > 0;
        }

        public bool SaveIngredientCategory(IngredientCategory category)
        {
            var dbContext = _serviceContainer.GetService<MealsDbContext>();

            if (category.Id > 0)
            {
                var tracked = dbContext.ChangeTracker.Entries<IngredientCategory>()
                    .FirstOrDefault(m => m.Entity.Id == category.Id);
                if (tracked != null)
                {
                    dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                }
                dbContext.IngredientCategories.Attach(category);
                dbContext.Entry(category).State = EntityState.Modified;
            }
            else
            {
                dbContext.IngredientCategories.Add(category);
            }

            return dbContext.SaveChanges() > 0;
        }
    }
}
