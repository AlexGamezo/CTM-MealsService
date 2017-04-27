using MealsService.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MealsService.Responses;
using MealsService.Services;

namespace MealsService.Controllers
{
    [Route("[controller]")]
    public class RecipesController : Controller
    {
        private RecipesService _recipesService;

        public RecipesController(RecipesService recipesService)
        {
            _recipesService = recipesService;
        }

        [HttpPost("list")]
        public IActionResult List([FromBody]ListRecipesRequest request)
        {
            var recipes = _recipesService.ListRecipes(request);
            
            return Json(new SuccessResponse<object>(new 
            {
                recipes
            }));
        }

        [HttpGet("{id:int}")]
        public IActionResult Get(int id)
        {
            var recipe = _recipesService.GetRecipe(id);

            if(recipe != null)
            {
                return Json(new SuccessResponse<RecipeDto>(recipe));
            }
            else
            {
                Response.StatusCode = 404;
                return Json(new ErrorResponse("Could not find the recipe", 404));
            }
        }


        [Authorize]
        [HttpPost]
        public IActionResult Create([FromBody]UpdateRecipeRequest request)
        {
            RecipeDto createdRecipe = _recipesService.UpdateRecipe(0, request);

            if (createdRecipe != null)
            {
                return Json(new SuccessResponse<RecipeDto>(createdRecipe));
            }
            else
            {
                Response.StatusCode = 400;
                return Json(new ErrorResponse("Failed to create recipe", 400));
            }
            
        }


        [Authorize]
        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody]UpdateRecipeRequest request)
        {
            var updatedRecipe = _recipesService.UpdateRecipe(id, request);

            return Json(new SuccessResponse<RecipeDto>(updatedRecipe));
        }


        [Authorize]
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var success = _recipesService.Remove(id);

            return Json(new SuccessResponse(success));
        }
    }
}
