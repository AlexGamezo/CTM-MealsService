
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

    public static class Errors
    {
        public static class Authorization
        {
            public static ErrorResponse UnauthorizedRequest = new ErrorResponse("Unauthorized request", 403);
        }

        public static class FileUploads
        {
            public static ErrorResponse NoFilesUploaded = new ErrorResponse("No files were uploaded", 500);
            public static ErrorResponse InvalidFileTypeUploaded = new ErrorResponse("Allowed types are PNG, JPEG, BMP", 500);
        }

        public static class Recipes
        {
            public static ErrorResponse RecipeUpdateFailed = new ErrorResponse("Could not update recipe image", 500);
        }
    }
}
