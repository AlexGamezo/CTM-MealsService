
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace MealsService.Images
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(string filename, string scope, IFormFile file);
        Task<bool> DeleteImageAsync(string path);
    }
}
