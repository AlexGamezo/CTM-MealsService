using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MealsService.Diets.Data;
using MealsService.Responses;

namespace MealsService.Diets
{
    [Route("[controller]")]
    public class DietTypesController : Controller
    {
        private DietTypeService _dietTypeService;

        public DietTypesController(DietTypeService dietTypeService)
        {
            _dietTypeService = dietTypeService;
        }

        [HttpGet]
        public IActionResult List()
        {
            var dietTypes = _dietTypeService.ListDietTypes();

            return Json(new
            {
                dietTypes
            });
        }

        [Authorize]
        [HttpPost]
        public IActionResult Create([FromBody] DietType create)
        {
            var success = _dietTypeService.CreateDietType(create);

            return Json(new SuccessResponse(success));
        }

        [Authorize]
        [HttpPost("{id:int}")]
        public IActionResult Update([FromBody] DietType update)
        {
            var success = _dietTypeService.UpdateDietType(update);

            return Json(new SuccessResponse(success));
        }
    }
}
