
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MealsService.Requests;
using MealsService.Responses;
using MealsService.Services;
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

            return Json(new
            {
                ingredients
            });
        }

        [Authorize]
        [HttpPost]
        public IActionResult Create([FromBody] CreateIngredientRequest create)
        {
            var ingredient = _ingredientsService.Create(create);

            if (ingredient == null)
            {
                Response.StatusCode = 500;
                return Json(new ErrorResponse("Could not create ingredient", 500));
            }

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
                HttpContext.Response.StatusCode = 403;
                return Json(Errors.Authorization.UnauthorizedRequest);
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
                HttpContext.Response.StatusCode = 400;
                return Json(Errors.FileUploads.NoFilesUploaded);
            }
            if (!allowedContentTypes.Contains(imageFile.ContentType))
            {
                HttpContext.Response.StatusCode = 400;
                return Json(Errors.FileUploads.InvalidFileTypeUploaded);
            }
            if (!await _ingredientsService.UpdateIngredientImage(ingredientId, imageFile))
            {
                HttpContext.Response.StatusCode = 400;
                return Json(Errors.Recipes.RecipeUpdateFailed);
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
