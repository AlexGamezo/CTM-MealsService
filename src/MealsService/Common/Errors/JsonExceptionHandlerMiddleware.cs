using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using MealsService.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace MealsService.Common.Errors
{
    public class JsonExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private ILogger _logger;

        public JsonExceptionHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger("Uncaught Exception");
        }

        public async Task Invoke(HttpContext context)
        {
            //If request doesn't match, skip
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                if (e is ServiceException)
                {
                    var se = (ServiceException)e;
                    context.Response.StatusCode = (int)se.StatusCode;
                    context.Response.ContentType = "application/json";

                    var errResponse = new ErrorResponse(se.Message, se.ErrorCode);
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(errResponse,
                        new JsonSerializerSettings { Formatting = Formatting.Indented }));
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var errResponse = new ErrorResponse(e.Message, 500);
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(errResponse,
                        new JsonSerializerSettings { Formatting = Formatting.Indented }));
                }

                _logger.LogError(e.ToString());
                _logger.LogError(e.StackTrace);
            }
        }
    }
}
