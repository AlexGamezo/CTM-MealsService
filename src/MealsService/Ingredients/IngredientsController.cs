using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MealsService.Common.Errors;
using MealsService.Requests;
using MealsService.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealsService.Ingredients
{
    [Route("[controller]")]
    public class IngredientsController : Controller
    {
        private IngredientsService _ingredientsService;

        public IngredientsController(IngredientsService ingredientsService)
        {
            _ingredientsService = ingredientsService;
        }

        [HttpGet("categories")]
        public IActionResult ListCategories()
        {
            return Json(new
            {
                categories = _ingredientsService.GetIngredientCategories()
            });
        }

        [HttpGet]
        public IActionResult List([FromQuery]string search = "")
        {
            var ingredients = _ingredientsService.GetIngredients(search);

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
        public IActionResult Create([FromBody] CreateIngredientRequest create)
        {
            var ingredient = _ingredientsService.Create(create);

            return Json(new {ingredient});
        }

        [Authorize]
        [HttpPut("{id:int}")]
        public IActionResult Update([FromBody] UpdateIngredientRequest update)
        {
            var success = _ingredientsService.Update(update);

            return Json(new SuccessResponse(success));
        }

        [Authorize]
        [HttpPost("{ingredientId:int}/image")]
        public async Task<IActionResult> UploadImageAsync(int ingredientId)
        {
            var claims = HttpContext.User.Claims;
            bool isAdmin = false;

            Boolean.TryParse(claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value, out isAdmin);

            if (!isAdmin)
            {
                throw StandardErrors.UnauthorizedRequest;
            }

            var imageFile = HttpContext.Request.Form.Files.FirstOrDefault();
            var allowedContentTypes = new List<string>()
            {
                "image/bmp",
                "image/png",
                "image/jpeg"
            };

            if (imageFile == null || imageFile.Length == 0)
            {
                throw FileUploads.NoFilesUploaded;
            }
            if (!allowedContentTypes.Contains(imageFile.ContentType))
            {
                throw FileUploads.InvalidFileTypeUploaded;
            }
            if (!await _ingredientsService.UpdateIngredientImage(ingredientId, imageFile))
            {
                throw Common.Errors.Recipes.RecipeUpdateFailed;
            }

            return Json(new SuccessResponse());
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var success = _ingredientsService.Delete(id);

            if (!success)
            {
                return Json(new ErrorResponse("Failed to delete ingredient. Are there any recipes using this ingredient?", 400));
            }

            return Json(new SuccessResponse());
        }
    }
}
