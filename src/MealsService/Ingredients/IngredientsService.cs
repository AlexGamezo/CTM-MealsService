using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Configurations;
using MealsService.Requests;
using MealsService.Ingredients.Data;
using MealsService.Tags;
using Microsoft.Extensions.DependencyInjection;


namespace MealsService.Ingredients
{
    public class IngredientsService
    {
        private IngredientsRepository _repository;
        private IMemoryCache _localCache;

        private string _imagesBucketName;
        private string _region;
        private IAmazonS3 _s3Client;

        private TagsService _tagsService;
        private MeasureTypesService _measureTypesService;

        private const int INGREDIENTS_CACHE_TTL_SECONDS = 900;
        private const int INGREDIENT_CATEGORIES_CACHE_TTL_SECONDS = 900;

        public IngredientsService(IServiceProvider serviceProvider, MeasureTypesService measureTypesService, TagsService tagsService, IAmazonS3 s3Client, IOptions<AWSConfiguration> options)
        {
            _tagsService = tagsService;
            _measureTypesService = measureTypesService;

            _repository = new IngredientsRepository(serviceProvider);

            _localCache = serviceProvider.GetService<IMemoryCache>();

            _s3Client = s3Client;
            _imagesBucketName = options.Value.IngredientImagesBucket;
            _region = options.Value.Region;
        }

        public Ingredient GetIngredient(int ingredientId)
        {
            return GetIngredients(new List<int> {ingredientId})
                .FirstOrDefault();
        }
        
        public List<Ingredient> GetIngredients(List<int> ingredientIds)
        {
            return ListIngredients()
                .Where(t => ingredientIds.Contains(t.Id))
                .ToList();
        }

        public List<Ingredient> GetIngredientsByTags(List<string> tags)
        {
            return ListIngredients()
                .Where(i => i.IngredientTags.Any(ig => tags.Contains(ig.Tag.Name)))
                .ToList();
        }

        public List<Ingredient> ListIngredients(bool skipCache = false)
        {
            if (skipCache)
            {
                return ListIngredientsInternal();
            }

            return _localCache.GetOrCreate(CacheKeys.Ingredients.IngredientsList, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(INGREDIENTS_CACHE_TTL_SECONDS);

                return ListIngredientsInternal();
            });
        }

        public List<Ingredient> GetIngredients(string search = "")
        {
            var ingredients = ListIngredients();

            if (search != "")
            {
                ingredients = ingredients.Where(i => i.Name.Contains(search)).ToList();
            }

            return ingredients;
        }

        public List<IngredientCategory> ListIngredientCategories(bool skipCache = false)
        {
            if (skipCache)
            {
                return ListIngredientCategoriesInternal();
            }

            return _localCache.GetOrCreate(CacheKeys.Ingredients.IngredientCategoriesList, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(INGREDIENT_CATEGORIES_CACHE_TTL_SECONDS);
                return ListIngredientCategoriesInternal();
            });
        }

        public Ingredient Create(CreateIngredientRequest request)
        {
            var missingCategory = false;
            var ingredient = new Ingredient
            {
                Name = request.Name,
                Brief = request.Brief,
                Description = request.Description,
            };

            if (request.MeasureTypes.Count == 0)
            {
                request.MeasureTypes.Add(_measureTypesService.DefaultMeasureType.Id);
            }

            ingredient.IngredientCategory = ListIngredientCategories()
                                         .FirstOrDefault(c => c.Name == request.Category) ?? new IngredientCategory { Name = request.Category };
            missingCategory = ingredient.IngredientCategory.Id == 0;

            if (!_repository.SaveIngredient(ingredient))
            {
                throw StandardErrors.CouldNotCreateEntity;
            }

            _repository.SetMeasurementTypes(ingredient, request.MeasureTypes);
            _repository.SetTags(ingredient, _tagsService.GetTags(request.Tags).Select(t => t.Id).ToList());
            
            ClearCacheIngredients();

            if (missingCategory)
            {
                ClearCacheCategories();
            }

            return ingredient;
        }

        public bool Update(UpdateIngredientRequest request)
        {
            var missingCategory = false;
            var ingredient = GetIngredient(request.Id);

            if (request.Category != ingredient.Category)
            {
                var ingredientCategory = ListIngredientCategories()
                    .FirstOrDefault(c => c.Name == request.Category);

                if (ingredientCategory == null)
                {
                    ingredient.IngredientCategory = new IngredientCategory
                    {
                        Name = request.Category
                    };
                    missingCategory = true;
                }
                else
                {
                    ingredient.IngredientCategory = ingredientCategory;
                }
            }

            ingredient.Name = request.Name;
            ingredient.Description = request.Description;
            ingredient.Brief = request.Brief;

            if (_repository.SaveIngredient(ingredient))
            {

                _repository.SetTags(ingredient, _tagsService.GetTags(request.Tags).Select(t => t.Id).ToList());
                _repository.SetMeasurementTypes(ingredient, request.MeasureTypes);

                ClearCacheIngredients();

                if (missingCategory)
                {
                    ClearCacheCategories();
                }

                return true;
            }

            return false;
        }
        
        public async Task<bool> UpdateIngredientImage(int ingredientId, IFormFile avatarFile)
        {
            var foundIngredient = GetIngredient(ingredientId);
            var extension = avatarFile.ContentType.Substring(avatarFile.ContentType.IndexOf("/", StringComparison.InvariantCulture) + 1);
            var avatarFilename = ingredientId + "." + extension;
            var stream = new MemoryStream();

            if (foundIngredient == null)
            {
                return false;
            }

            avatarFile.CopyTo(stream);

            var response = await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _imagesBucketName,
                InputStream = stream,
                Key = avatarFilename,
                CannedACL = S3CannedACL.PublicRead
            });

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                foundIngredient.Image = GetImageUrl(avatarFilename);
                return _repository.SaveIngredient(foundIngredient);
            }
            else
            {
                return false;
            }
        }

        private string GetImageUrl(string filename)
        {
            return $"https://s3-{_region}.amazonaws.com/{_imagesBucketName}/{filename}";
        }

        /*public bool Delete(int id)
        {
            if (id == 0)
            {
                return false;
            }

            if (_dbContext.RecipeIngredients.Any(mi => mi.IngredientId == id))
            {
                return false;
            }
            
            var ingredient = _dbContext.Ingredients.Find(id);
            var tags = _dbContext.IngredientTags.Where(it => it.IngredientId == id).ToList();

            _dbContext.IngredientTags.RemoveRange(tags);
            _dbContext.Ingredients.Remove(ingredient);

            return _dbContext.SaveChanges() > 0;
        }*/

        private List<Ingredient> ListIngredientsInternal()
        {
            return _repository.ListIngredients();
        }

        private List<IngredientCategory> ListIngredientCategoriesInternal()
        {
            return _repository.ListIngredientCategories();
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
