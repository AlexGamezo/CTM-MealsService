using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Caching.Memory;
using UnitsNet;
using UnitsNet.Units;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Common.Extensions;
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
                existingCategory = SaveIngredientCategory(existingCategory);
            }

            return existingCategory;
        }

        public void NormalizeMeasuredIngredient(MeasuredIngredient measuredIngredient)
        {
            var ingredient = GetIngredient(measuredIngredient.IngredientId);

            if (ingredient.IsMeasuredVolume)
            {
                NormalizeVolumeIngredient(measuredIngredient, ingredient);
            }
            else
            {
                NormalizeMassIngredient(measuredIngredient, ingredient);

            }
        }

        public void DenormalizeMeasuredIngredient(MeasuredIngredient measuredIngredient)
        {
            var ingredient = GetIngredient(measuredIngredient.IngredientId);

            if (ingredient.IsMeasuredVolume)
            {
                DenormalizeVolumeIngredient(measuredIngredient);
            }
            else
            {
                DenormalizeMassIngredient(measuredIngredient, ingredient);

            }
        }

        public MeasuredIngredient GroupMeasuredIngredients(List<MeasuredIngredient> ingredients)
        {
            if (!ingredients.Any())
            {
                return null;
            }

            var ingredient = GetIngredient(ingredients[0].IngredientId);
            var groupIngredient = new MeasuredIngredient
            {
                IngredientId = ingredient.Id,
            };

            foreach(var individual in ingredients)
            {
                DenormalizeMeasuredIngredient(individual);
                groupIngredient.Quantity += individual.Quantity;
            }

            NormalizeMeasuredIngredient(groupIngredient);

            return groupIngredient;
        }

        private void NormalizeMassIngredient(MeasuredIngredient measuredIngredient, IngredientDto ingredient)
        {
            if (!string.IsNullOrEmpty(measuredIngredient.Measure))
            {
                DenormalizeMassIngredient(measuredIngredient, ingredient);
            }

            var mass = Mass.FromOunces(measuredIngredient.Quantity);
            var individualQuantity = 0.0;

            if (ingredient.IndividualWeight > 0)
            {
                individualQuantity = (measuredIngredient.Quantity / ingredient.IndividualWeight).RoundToQuarter();
            }

            if (individualQuantity > 0 && individualQuantity <= 5)
            {
                measuredIngredient.Quantity = individualQuantity;
                measuredIngredient.Measure = "whole";
            }
            else if (mass.Pounds >= 1)
            {
                measuredIngredient.Quantity = mass.Pounds.RoundToQuarter();
                measuredIngredient.Measure = "lbs";

            }
            else
            {
                measuredIngredient.Quantity = mass.Ounces;
                measuredIngredient.Measure = "oz";
            }
        }

        private void NormalizeVolumeIngredient(MeasuredIngredient measuredIngredient, IngredientDto ingredient)
        {
            if (!string.IsNullOrEmpty(measuredIngredient.Measure))
            {
                DenormalizeVolumeIngredient(measuredIngredient);
            }

            var volume = Volume.FromUsOunces(measuredIngredient.Quantity);

            if (volume.UsTablespoons < 1)
            {
                measuredIngredient.Quantity = volume.UsTeaspoons.RoundToQuarter();
                measuredIngredient.Measure = "tsp";

            }
            else if (volume.UsCustomaryCups >= 0.3333)
            {
                var deltaQuarter = Math.Abs(volume.UsCustomaryCups - volume.UsCustomaryCups.RoundToQuarter());
                var deltaThird = Math.Abs(volume.UsCustomaryCups - volume.UsCustomaryCups.RoundToThird());

                measuredIngredient.Quantity = deltaQuarter < deltaThird ?
                    volume.UsCustomaryCups.RoundToQuarter() :
                    volume.UsCustomaryCups.RoundToThird();
                measuredIngredient.Measure = "cups";
            }
            else if (volume.UsOunces < 2)
            {
                measuredIngredient.Quantity = volume.UsTablespoons.RoundToQuarter();
                measuredIngredient.Measure = "tbsp";
            }
            else
            {
                measuredIngredient.Quantity = volume.UsOunces;
                measuredIngredient.Measure = "oz";
            }
        }

        private void DenormalizeMassIngredient(MeasuredIngredient measuredIngredient, IngredientDto ingredient)
        {
            var unit = MassUnit.Ounce;
            if (!string.IsNullOrEmpty(measuredIngredient.Measure))
            {
                if (measuredIngredient.Measure == "whole" && ingredient.IndividualWeight > 0)
                {
                    unit = MassUnit.Ounce;
                    measuredIngredient.Quantity *= ingredient.IndividualWeight;
                }
                else if (measuredIngredient.Measure == "lbs")
                    unit = MassUnit.Pound;
                else if (measuredIngredient.Measure == "oz")
                    unit = MassUnit.Ounce;
                else
                    unit = Mass.ParseUnit(measuredIngredient.Measure);
            }

            measuredIngredient.Quantity = Mass.From(measuredIngredient.Quantity, unit).As(MassUnit.Ounce);
            measuredIngredient.Measure = null;
        }

        private void DenormalizeVolumeIngredient(MeasuredIngredient measuredIngredient)
        {
            var unit = VolumeUnit.UsOunce;
            if (!string.IsNullOrEmpty(measuredIngredient.Measure))
            {
                if (measuredIngredient.Measure == "tsp")
                    unit = VolumeUnit.UsTeaspoon;
                else if (measuredIngredient.Measure == "tbsp")
                    unit = VolumeUnit.UsTablespoon;
                else if (measuredIngredient.Measure == "cups")
                    unit = VolumeUnit.UsCustomaryCup;
                else if (measuredIngredient.Measure == "oz")
                    unit = VolumeUnit.UsOunce;
                else
                    unit = Volume.ParseUnit(measuredIngredient.Measure);
            }

            measuredIngredient.Quantity = Volume.From(measuredIngredient.Quantity, unit).As(VolumeUnit.UsOunce);
            measuredIngredient.Measure = null;
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
