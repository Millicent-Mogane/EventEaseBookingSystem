using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EventEaseBookingSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _configuration;

        public ImageController(
            BlobServiceClient blobServiceClient,
            IConfiguration configuration)
        {
            _blobServiceClient = blobServiceClient;
            _configuration = configuration;
        }

        [HttpPost("UploadImage")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please select an image file."
                });
            }

            // Restrict to image types
            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/gif",
                "image/webp"
            };

            if (!allowedContentTypes.Contains(
                file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(new
                {
                    message = "Only JPEG, PNG, GIF, and WebP images are allowed."
                });
            }

            // Restrict file size to 5 MB
            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                return BadRequest(new
                {
                    message = "The image must be smaller than 5 MB."
                });
            }

            // Get container name from appsettings.json
            var containerName =
                _configuration["AzureStorage:ContainerName"];

            if (string.IsNullOrWhiteSpace(containerName))
            {
                return StatusCode(500, new
                {
                    message = "Azure Blob container name is not configured."
                });
            }

            // Get container
            var containerClient =
                _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync();

            // Get the original file extension
            var extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            // Generate a unique blob name
            // This prevents two uploaded files with the same name
            // from accidentally overwriting each other.
            var blobName =
                $"{Guid.NewGuid()}{extension}";

            // Get blob reference
            var blobClient =
                containerClient.GetBlobClient(blobName);

            // Upload with the correct Content-Type
            using (var stream = file.OpenReadStream())
            {
                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = file.ContentType
                    }
                };

                await blobClient.UploadAsync(
                    stream,
                    uploadOptions);
            }

            // Return response
            return Ok(new
            {
                message = "Image uploaded successfully!",
                fileName = blobName,
                originalFileName = file.FileName,
                contentType = file.ContentType,
                url = blobClient.Uri.ToString()
            });
        }
    }
}