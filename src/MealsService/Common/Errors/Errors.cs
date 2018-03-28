using System.Net;

namespace MealsService.Common.Errors
{
    public static class StandardErrors
    {
        public static ServiceException UnauthorizedRequest = new ServiceException("Unauthorized request", 401, HttpStatusCode.Unauthorized);
        public static ServiceException ForbiddenRequest = new ServiceException("Forbidden request", 403, HttpStatusCode.Forbidden);
        public static ServiceException MissingRequestedItem = new ServiceException("Requested item not found", 404, HttpStatusCode.NotFound);
    }

    public static class FileUploads
    {
        public static ServiceException NoFilesUploaded = new ServiceException("No files were uploaded", 1001, HttpStatusCode.BadRequest);
        public static ServiceException InvalidFileTypeUploaded = new ServiceException("Allowed types are PNG, JPEG, BMP", 1001, HttpStatusCode.UnsupportedMediaType);
    }

    public static class Recipes
    {
        public static ServiceException RecipeUpdateFailed = new ServiceException("Could not update recipe image", 2001);
    }
}
