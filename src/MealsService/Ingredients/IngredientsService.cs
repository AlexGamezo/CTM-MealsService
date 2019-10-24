using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Caching.Memory;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Ingredients.Data;
using MealsService.Ingredients.Dtos;

namespace MealsService.Ingredients
{
    public class IngredientsService : IIngredientsService
    {
        private IIngredientsRepository _repository;
        private IMemoryCache _localCache;

        private const int INGREDIENTS_CACHE_TTL_SECONDS = 900;
        private const int INGREDIENT_CATEGORIES_CACHE_TTL_SECONDS = 900;

        public IngredientsService(IMemoryCache memoryCache, IIngredientsRepository repository)
        {
            _repository = repository;
            _localCache = memoryCache;
        }

        public IngredientDto GetIngredient(int ingredientId)
        {
            return ListIngredients().FirstOrDefault(i => i.Id == ingredientId);
        }

        public List<IngredientDto> GetIngredients(List<int> ingredientIds)
        {
            return ListIngredients()
                .Where(t => ingredientIds.Contains(t.Id))
                .ToList();
        }

        public List<IngredientDto> GetIngredientsByTags(List<string> tags)
        {
            var lowerCasedTags = tags.Select(t => t.ToLowerInvariant()).ToList();
            return ListIngredients()
                .Where(i => i.Tags.Any(ig => lowerCasedTags.Contains(ig)))
                .ToList();
        }

        public List<IngredientDto> ListIngredients()
        {
            return _localCache.GetOrCreate(CacheKeys.Ingredients.IngredientsList, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(INGREDIENTS_CACHE_TTL_SECONDS);

                return _repository.ListIngredients().Select(i => i.ToDto()).ToList();
            });
        }

        public List<IngredientDto> SearchIngredients(string search)
        {
            var ingredients = ListIngredients();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLowerInvariant();
                ingredients = ingredients.Where(i => i.Name.Contains(search)).ToList();
            }

            return ingredients;
        }

        public List<IngredientCategory> ListIngredientCategories()
        {
            return _localCache.GetOrCreate(CacheKeys.Ingredients.IngredientCategoriesList, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(INGREDIENT_CATEGORIES_CACHE_TTL_SECONDS);
                return _repository.ListIngredientCategories();
            });
        }

        public Ingredient SaveIngredient(IngredientDto ingredientDto)
        {
            var ingredient = ingredientDto.FromDto();

            if (!_repository.SaveIngredient(ingredient))
            {
                return null;
            }

            if (ingredient != null)
            {
                if (ingredientDto.Tags != null)
                {
                    SetIngredientTags(ingredient.Id, ingredientDto.Tags);
                }

                if (ingredientDto.Category != null)
                {
                    SetIngredientCategory(ingredient.Id, ingredientDto.Category);
                }
            }

            ClearCacheIngredients();
            return ingredient;
        }

        public bool SetIngredientCategory(int id, string category)
        {
            var ingredient = _repository.ListIngredients().FirstOrDefault(i => i.Id == id);
            if (ingredient == null)
            {
                throw StandardErrors.MissingRequestedItem;
            }

            ingredient.IngredientCategory = GetOrCreateCategoryByName(category);
            ingredient.CategoryId = ingredient.IngredientCategory.Id; 
            
            if (_repository.SaveIngredient(ingredient))
            {
                ClearCacheIngredients();
                return true;
            }

            return false;
        }

        public IngredientCategory GetOrCreateCategoryByName(string category)
        {
            var existingCategory = GetCategoryByName(category);
            if (existingCategory == null)
            {
                existingCategory = new IngredientCategory
                {
                    Name = category.ToLowerInvariant()
                };
                if (_repository.SaveIngredientCategory(existingCategory))
                {
                    ClearCacheCategories();
                }
            }

            return existingCategory;
        }

        public IngredientCategory GetCategoryByName(string category)
        {
            return ListIngredientCategories().FirstOrDefault(c => c.Name == category.ToLowerInvariant());
        }

        public IngredientCategory SaveIngredientCategory(IngredientCategory category)
        {
            if (_repository.SaveIngredientCategory(category))
            {
                ClearCacheCategories();
            }

            return category;
        }

        public bool DeleteIngredient(int ingredientId)
        {
            if (ingredientId == 0)
            {
                throw StandardErrors.MissingRequestedItem;
            }

            if (_repository.DeleteIngredientById(ingredientId))
            {
                ClearCacheIngredients();
                return true;
            }

            return false;
        }

        public bool DeleteIngredientCategory(int categoryId)
        {
            if (categoryId == 0)
            {
                throw StandardErrors.MissingRequestedItem;
            }

            if (_repository.DeleteIngredientCategoryById(categoryId))
            {
                ClearCacheCategories();
                return true;
            }

            return false;
        }

        public bool SetIngredientTags(int ingredientId, List<string> tags)
        {
            if (_repository.SetTags(ingredientId, tags))
            {
                ClearCacheIngredients();
            }

            return false;
        }

        private void ClearCacheIngredients()
        {
            _localCache.Remove(CacheKeys.Ingredients.IngredientsList);
        }

        private void ClearCacheCategories()
        {
            _localCache.Remove(CacheKeys.Ingredients.IngredientCategoriesList);
        }
    }
}
