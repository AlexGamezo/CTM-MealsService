using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MealsService.Infrastructure
{
    public class RequestContextHydrateMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestContextHydrateMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, RequestContext requestContext)
        {
            requestContext.Hydrate(context);

            //If request doesn't match, skip
            await _next(context);
        }
    }
}
