using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using MealsService.Configurations;
using MealsService.Requests;
using MealsService.Ingredients.Data;
using MealsService.Tags;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Tag = MealsService.Tags.Data.Tag;

namespace MealsService.Ingredients
{
    public class IngredientsService
    {
        private MealsDbContext _dbContext;

        private string _imagesBucketName;
        private string _region;
        private IAmazonS3 _s3Client;

        private TagsService _tagsService;

        public IngredientsService(MealsDbContext dbContext, TagsService tagsService, IAmazonS3 s3Client, IOptions<AWSConfiguration> options)
        {
            _dbContext = dbContext;
            _tagsService = tagsService;

            _s3Client = s3Client;
            _imagesBucketName = options.Value.IngredientImagesBucket;
            _region = options.Value.Region;
        }

        public Ingredient GetIngredient(int ingredient)
        {
            return _dbContext.Ingredients
                .Include(i => i.IngredientTags)
                    .ThenInclude(it => it.Tag)
                .Include(i => i.IngredientCategory)
                .FirstOrDefault(t => t.Id == ingredient);
        }

        public Ingredient Create(CreateIngredientRequest request)
        {
            var ingredient = new Ingredient
            {
                Name = request.Name,
                Brief = request.Brief,
                Description = request.Description,
            };

            var ingredientCategory = _dbContext.IngredientCategories
                .FirstOrDefault(c => c.Name == request.Category);

            ingredient.IngredientCategory = ingredientCategory ?? new IngredientCategory { Name = request.Category };

            var tags = _tagsService.ListTags();

            ingredient.IngredientTags.AddRange(request.Tags.Select(
                tag => new IngredientTag{
                    Tag = tags.FirstOrDefault(t => t.Name == tag.ToLower()) ?? new Tag { Name = tag.ToLower() }
                }
            ));

            _dbContext.Ingredients.Add(ingredient);

            if (_dbContext.SaveChanges() > 0)
            {
                return ingredient;
            }

            return null;
        }

        public bool Update(UpdateIngredientRequest request)
        {
            var ingredient = GetIngredient(request.Id);

            ingredient.Name = request.Name;
            ingredient.Description = request.Description;
            ingredient.Brief = request.Brief;

            if (request.Category != ingredient.Category)
            {
                var ingredientCategory = _dbContext.IngredientCategories
                    .FirstOrDefault(c => c.Name == request.Category);

                if (ingredientCategory == null)
                {
                    ingredient.IngredientCategory = new IngredientCategory
                    {
                        Name = request.Category
                    };
                }
                else
                {
                    ingredient.IngredientCategory = ingredientCategory;
                }
            }

            //TODO: Clean up to check for new/removed tags only, instead of mass replace
            var tags = _tagsService.ListTags().Where(t => request.Tags.Contains(t.Name))
                .ToList();

            _dbContext.IngredientTags.RemoveRange(ingredient.IngredientTags);
            ingredient.IngredientTags.Clear();

            var ingredientTags = request.Tags.Select(
                tag => new IngredientTag
                {
                    Ingredient = ingredient,
                    Tag = tags.FirstOrDefault(t => t.Name == tag.ToLower()) ?? new Tag { Name = tag.ToLower() }
                }
            );
            ingredient.IngredientTags.AddRange(ingredientTags);

            return _dbContext.SaveChanges() > 0;
        }
        
        public async Task<bool> UpdateIngredientImage(int ingredientId, IFormFile avatarFile)
        {
            var foundIngredient = _dbContext.Ingredients.FirstOrDefault(p => p.Id == ingredientId);
            var extension = avatarFile.ContentType.Substring(avatarFile.ContentType.IndexOf("/") + 1);
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
                return _dbContext.Entry(foundIngredient).State == EntityState.Unchanged || _dbContext.SaveChanges() == 1;
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

        public bool Delete(int id)
        {
            if (id == 0)
            {
                return false;
            }

            if (_dbContext.MealIngredients.Any(mi => mi.IngredientId == id))
            {
                return false;
            }
            
            var ingredient = _dbContext.Ingredients.Find(id);
            var tags = _dbContext.IngredientTags.Where(it => it.IngredientId == id).ToList();

            _dbContext.IngredientTags.RemoveRange(tags);
            _dbContext.Ingredients.Remove(ingredient);

            return _dbContext.SaveChanges() > 0;
        }

        //TODO: Add cache layer
        public IEnumerable<Ingredient> GetIngredients(string search = "")
        {
            IQueryable<Ingredient> ingredients = _dbContext.Ingredients.Include(i => i.IngredientCategory)
                .Include(i => i.IngredientTags).ThenInclude(it => it.Tag);

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
