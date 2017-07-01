
using MealsService.Requests;
using MealsService.Responses;
using MealsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealsService.Controllers
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
            var success = _ingredientsService.Create(create);

            return Json(new SuccessResponse(success));
        }

        [Authorize]
        [HttpPut("{id:int}")]
        public IActionResult Update([FromBody] UpdateIngredientRequest update)
        {
            var success = _ingredientsService.Update(update);

            return Json(new SuccessResponse(success));
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var success = _ingredientsService.Delete(id);

            if (!success)
            {
                return Json(new ErrorResponse(
                    "Failed to delete ingredient. Are there any recipes using this ingredient?", 400));
            }

            return Json(new SuccessResponse());
        }
    }
}
