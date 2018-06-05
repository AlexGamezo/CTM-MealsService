using MealsService.Common;
using MealsService.Ingredients.Data;
using MealsService.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealsService.Ingredients
{
    [Route("[controller]")]
    public class MeasureTypesController : AuthorizedController
    {
        private MeasureTypesService _service;

        public MeasureTypesController(MeasureTypesService mtService)
        {
            _service = mtService;
        }

        [Route(""), HttpGet]
        public IActionResult ListAll()
        {
            var measureTypes = _service.ListAvailableTypes();

            return Json(new SuccessResponse<object>(new
            {
                measureTypes
            }));
        }

        [AdminRequiredFilter, Authorize]
        [Route(""), HttpPost]
        public IActionResult Create([FromBody]MeasureType request)
        {
            if (request.Id != 0)
            {
                request.Id = 0;
            }

            var measureType = _service.Create(request);

            return Json(new SuccessResponse<object>(new
            {
                measureType
            }));
        }

        [AdminRequiredFilter, Authorize]
        [Route("{id:int}"), HttpPut]
        public IActionResult Update(int id, [FromBody] MeasureType request)
        {
            request.Id = id;

            if (!_service.Update(request))
            {
                Response.StatusCode = 400;
                return Json(new ErrorResponse("Could not update the measure type. Check your request is valid", 400));
            }

            return Json(new SuccessResponse());
        }

        [AdminRequiredFilter, Authorize]
        [Route("{id:int}"), HttpDelete]
        public IActionResult Delete(int id)
        {
            if (!_service.Delete(id))
            {
                Response.StatusCode = 400;
                return Json(new ErrorResponse("Could not delete the measure type. Check your request is valid", 400));
            }

            return Json(new SuccessResponse());
        }
    }
}
