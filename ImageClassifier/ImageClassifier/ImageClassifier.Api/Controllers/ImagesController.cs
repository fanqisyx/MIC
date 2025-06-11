using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting; // Required for IWebHostEnvironment
using System; // Required for Path
using System.Collections.Generic; // Required for List
using System.IO; // Required for Path and FileStream
using System.Linq; // Required for Linq operations like Select
using System.Threading.Tasks; // Required for Task
// Required for IFormFile and StatusCodes
using Microsoft.AspNetCore.Http;


namespace ImageClassifier.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public ImagesController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImages(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var uploadedFilesData = new List<object>();
            var uploadsFolderPath = Path.Combine(_environment.ContentRootPath, "Uploads");

            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    // Ensure filename is unique to prevent overwrites
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

                    try
                    {
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        uploadedFilesData.Add(new { FileName = uniqueFileName, Size = file.Length, Path = filePath });
                    }
                    catch (Exception ex)
                    {
                        // Log error (not implemented in this subtask)
                        // Potentially return a partial success or a more specific error
                        return StatusCode(StatusCodes.Status500InternalServerError, $"Error uploading file {file.FileName}: {ex.Message}");
                    }
                }
            }

            if (uploadedFilesData.Count == 0)
            {
                return BadRequest("No valid files were processed.");
            }

            return Ok(new { Message = $"{uploadedFilesData.Count} file(s) uploaded successfully.", Files = uploadedFilesData });
        }
    }
}
