using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using MealsService.Ingredients.Data;
using MealsService.Tags;

namespace MealsService.Ingredients
{
    public class IngredientsRepository : IIngredientsRepository
    {
        private ITagsService _tagsService;
        private MealsDbContext _dbContext;

        public IngredientsRepository(MealsDbContext dbContext, ITagsService tagsService)
        {
            _dbContext = dbContext;
            _tagsService = tagsService;
        }

        public List<Ingredient> ListIngredients()
        {
            return _dbContext.Ingredients
                .Include(i => i.IngredientTags)
                .ThenInclude(it => it.Tag)
                //.Include(i => i.IngredientMeasureTypes)
                .Include(i => i.IngredientCategory)
                .ToList();
        }

        public List<IngredientCategory> ListIngredientCategories()
        {
            return _dbContext.IngredientCategories
                .ToList();
        }

        public bool SaveIngredient(Ingredient ingredient)
        {
            if (ingredient.Id > 0)
            {
                var tracked = _dbContext.ChangeTracker.Entries<Ingredient>()
                    .FirstOrDefault(m => m.Entity.Id == ingredient.Id);
                if (tracked != null)
                {
                    _dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                }
                _dbContext.Ingredients.Attach(ingredient);
                _dbContext.Entry(ingredient).State = EntityState.Modified;
            }
            else
            {
                _dbContext.Ingredients.Add(ingredient);
            }

            return _dbContext.SaveChanges() > 0;
        }

        public bool SaveIngredientCategory(IngredientCategory category)
        {
            if (category.Id > 0)
            {
                var tracked = _dbContext.ChangeTracker.Entries<IngredientCategory>()
                    .FirstOrDefault(m => m.Entity.Id == category.Id);
                if (tracked != null)
                {
                    _dbContext.Entry(tracked.Entity).State = EntityState.Detached;
                }
                _dbContext.IngredientCategories.Attach(category);
                _dbContext.Entry(category).State = EntityState.Modified;
            }
            else
            {
                _dbContext.IngredientCategories.Add(category);
            }

            return _dbContext.SaveChanges() > 0;
        }

        public bool SetTags(int ingredientId, List<string> tagStrings)
        {
            var tags = _tagsService.GetOrCreateTags(tagStrings);
            var ingredient = ListIngredients().FirstOrDefault(i => i.Id == ingredientId);

            if(ingredient == null)
            {
                throw Common.Errors.StandardErrors.MissingRequestedItem;
            }

            var changes = false;

            for (var i = 0; i < tags.Count; i++)
            {
                if (ingredient.IngredientTags?.Count > i)
                {
                    if (ingredient.IngredientTags[i].TagId != tags[i].Id)
                    {
                        ingredient.IngredientTags[i].TagId = tags[i].Id;
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
                    ingredient.IngredientTags.Add(new IngredientTag { TagId = tags[i].Id });
                }
            }
            if (tags.Count < ingredient.IngredientTags.Count)
            {
                var countToRemove = ingredient.IngredientTags.Count - tags.Count;
                var toDelete = ingredient.IngredientTags.GetRange(tags.Count, countToRemove);

                changes = true;
                _dbContext.IngredientTags.RemoveRange(toDelete);
            }

            return !changes || _dbContext.SaveChanges() > 0;
        }

        public bool DeleteIngredientById(int ingredientId)
        {
            var ingredient = ListIngredients().FirstOrDefault(i => i.Id == ingredientId);

            if(ingredient != null)
            {
                _dbContext.Remove(ingredient);
                return _dbContext.SaveChanges() > 0;
            }

            return false;
        }
        
        public bool DeleteIngredientCategoryById(int categoryId)
        {
            //var category = ListIngredientCategories().FirstOrDefault(c => c.Id == categoryId);
            var category = _dbContext.IngredientCategories.FirstOrDefault(c => c.Id == categoryId);

            if (category != null)
            {
                _dbContext.IngredientCategories.Remove(category);

                return _dbContext.SaveChanges() > 0;
            }

            return false;
        }
    }
}
