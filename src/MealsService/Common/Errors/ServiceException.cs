using System;
using System.Net;

namespace MealsService.Common.Errors
{
    public class ServiceException : Exception
    {
        public ServiceException(string message, int errorCode, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }

        public HttpStatusCode StatusCode { get; set; }
        public int ErrorCode { get; set; }
    }
}
