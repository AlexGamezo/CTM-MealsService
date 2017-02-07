using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MealsService.Responses
{
    public class ErrorResponse
    {
        public bool Error { get; set; } = true;
        public string Message { get; set; }
        public int ErrorCode { get; set; }

        public ErrorResponse(string message, int errorCode)
        {
            Message = message;
            ErrorCode = errorCode;
        }
    }
}
