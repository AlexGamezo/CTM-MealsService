using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

using MealsService.Responses;
using MealsService.Tags;
using MealsService.Tags.Data;

namespace MealsService.Controllers
{
    [Route("[controller]")]
    public class TagsController : Controller
    {
        private TagsService _tagsService;

        public TagsController(TagsService tagsService)
        {
            _tagsService = tagsService;
        }

        [HttpGet]
        public IActionResult List(string search = "")
        {
            List<Tag> tags = null;

            if (string.IsNullOrEmpty(search))
            {
                tags = _tagsService.SearchTags(search);
            }
            else
            {
                tags = _tagsService.ListTags();
            }

            return Json(new SuccessResponse<object>( new
            {
                tags
            }));
        }

        [HttpPost]
        public IActionResult Create([FromBody]Tag update)
        {
            var success = _tagsService.UpdateTag(update);

            return Json(new SuccessResponse(success));
        }

        [HttpPut, Route("{id:int}")]
        public IActionResult Update(int id, [FromBody]Tag update)
        {
            var success = _tagsService.UpdateTag(update);

            return Json(new SuccessResponse(success));
        }

        [HttpDelete, Route("{id:int}")]
        public IActionResult Delete(int id)
        {
            var success = _tagsService.DeleteTagById(id);

            return Json(new SuccessResponse(success));
        }
    }
}