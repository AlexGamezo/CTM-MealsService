using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

using Amazon.S3;
using Amazon.S3.Model;
using System.Collections.Generic;
using MealsService.Common.Errors;

namespace MealsService.Images
{
    public class ImageService : IImageService
    {
        private string _region;

        private IAmazonS3 _s3Client;

        public Task<bool> DeleteImageAsync(string path)
        {
            throw new NotImplementedException();
        }

        public async Task<string> UploadImageAsync(string filename, string scope, IFormFile fileData)
        {
            var allowedContentTypes = new List<string>()
            {
                "image/bmp",
                "image/png",
                "image/jpeg"
            };

            if (fileData == null || fileData.Length == 0)
            {
                throw FileUploads.NoFilesUploaded;
            }
            if (!allowedContentTypes.Contains(fileData.ContentType))
            {
                throw FileUploads.InvalidFileTypeUploaded;
            }

            var extension = fileData.ContentType.Substring(fileData.ContentType.IndexOf("/", StringComparison.InvariantCulture) + 1);
            var stream = new MemoryStream();

            
            fileData.CopyTo(stream);

            var response = await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = scope,
                InputStream = stream,
                Key = filename,
                CannedACL = S3CannedACL.PublicRead
            });

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return GetImageUrl(filename, scope);
            }

            return null;
        }
        private string GetImageUrl(string filename, string scope)
        {
            return $"https://s3-{_region}.amazonaws.com/{scope}/{filename}";
        }

    }
}
