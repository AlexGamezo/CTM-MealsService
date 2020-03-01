using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using MealsService.Common;
using MealsService.Common.Errors;
using MealsService.Configurations;
using MealsService.Images;
using MealsService.Requests;
using MealsService.Responses;
using System.Collections.Generic;
using MealsService.Ingredients.Dtos;

namespace MealsService.Ingredients
{
    [Route("[controller]")]
    public class IngredientsController : AuthorizedController
    {
        private IIngredientsService _ingredientsService;
        private IImageService _imageService;
        private IOptions<AWSConfiguration> _awsOptions;

        public IngredientsController(IIngredientsService ingredientsService, IImageService imageService, IOptions<AWSConfiguration> options)
        {
            _ingredientsService = ingredientsService;
            _imageService = imageService;
            _awsOptions = options;

        }

        [HttpGet("categories")]
        public IActionResult ListCategories()
        {
            return Json(new
            {
                categories = _ingredientsService.ListIngredientCategories()
            });
        }

        [HttpGet]
        public IActionResult List([FromQuery]string search = "")
        {
            var ingredients = _ingredientsService.SearchIngredients(search);

            return Json(new { ingredients });
        }

        [HttpGet]
        [Route("{id:int}")]
        public IActionResult GetIngredient(int id)
        {
            var ingredient = _ingredientsService.GetIngredient(id);

            return Json(new { ingredient });
        }

        [HttpGet]
        [Route("{ids}")]
        public IActionResult GetIngredients(string ids)
        {
            var idList = ids.Split(',').Select(id =>
            {
                int.TryParse(id, out var parsed);
                return parsed;
            }).Where(id => id > 0).ToList();

            var ingredients = _ingredientsService.GetIngredients(idList);

            return Json(new { ingredients });
        }

        [Authorize]
        [HttpPost]
        public IActionResult Create([FromBody] IngredientDto dto)
        {
            var ingredient = _ingredientsService.SaveIngredient(dto);

            return Json(new { ingredient });
        }

        [Authorize]
        [HttpPut("{id:int}")]
        public IActionResult Update([FromBody] IngredientDto dto)
        {
            var ingredient = _ingredientsService.SaveIngredient(dto);

            return Json(new { ingredient });
        }

        [Authorize]
        [HttpPost("{ingredientId:int}/image")]
        public async Task<IActionResult> UploadImageAsync(int ingredientId)
        {
            if (!IsAdmin)
            {
                throw StandardErrors.UnauthorizedRequest;
            }

            var foundIngredient = _ingredientsService.GetIngredient(ingredientId);

            if (foundIngredient == null)
            {
                throw StandardErrors.MissingRequestedItem;
            }

            foundIngredient.Image = await _imageService.UploadImageAsync(ingredientId.ToString(), _awsOptions.Value.IngredientImagesBucket, HttpContext.Request.Form.Files.FirstOrDefault());

            _ingredientsService.SaveIngredient(foundIngredient);

            return Json(new SuccessResponse());
        }

        [Authorize]
        [HttpPut("{ingredientId:int}/tags")]
        public IActionResult SetTags(int ingredientId, [FromBody]List<string> tags)
        {
            if (!IsAdmin)
            {
                throw StandardErrors.UnauthorizedRequest;
            }

            _ingredientsService.SetIngredientTags(ingredientId, tags);

            return Json(new SuccessResponse());
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            if(!IsAdmin)
            {
                throw StandardErrors.UnauthorizedRequest;
            }

            if (!_ingredientsService.DeleteIngredient(id))
            {
                //TODO: should be thrown by service
                return Json(new ErrorResponse("Failed to delete ingredient. Are there any recipes using this ingredient?", 400));
            }

            return Json(new SuccessResponse());
        }
    }
}
