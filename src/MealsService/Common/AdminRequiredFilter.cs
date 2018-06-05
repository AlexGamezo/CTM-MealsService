using MealsService.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MealsService.Common
{
    public class AdminRequiredFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!((AuthorizedController) context.Controller).IsAdmin)
            {
                context.HttpContext.Response.StatusCode = 403;
                context.Result = new JsonResult(new ErrorResponse("Must be an admin", 403));
            }
        }
    }
}
