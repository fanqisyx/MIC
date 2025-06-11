// Controllers/ClassificationsController.cs
using ImageClassifier.Api.Models;
using ImageClassifier.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageClassifier.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClassificationsController : ControllerBase
    {
        private readonly ClassificationService _classificationService;

        public ClassificationsController(ClassificationService classificationService)
        {
            _classificationService = classificationService;
        }

        public class ClassifyImageDto
        {
            public string ImageIdentifier { get; set; }
            public Guid CategoryId { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<ImageClassification>> ClassifyImage([FromBody] ClassifyImageDto classifyDto)
        {
            if (classifyDto == null || string.IsNullOrWhiteSpace(classifyDto.ImageIdentifier) || classifyDto.CategoryId == Guid.Empty)
            {
                return BadRequest("Image identifier and category ID are required.");
            }
            try
            {
                var classification = await _classificationService.AddOrUpdateClassificationAsync(classifyDto.ImageIdentifier, classifyDto.CategoryId);
                return Ok(classification);
            }
            catch (ArgumentException ex) // Catches invalid Category ID
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log exception (not implemented here)
                Console.WriteLine($"Error in ClassifyImage: {ex.Message}"); // Basic console log
                return StatusCode(500, "An error occurred while classifying the image.");
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<ImageClassification>>> GetAllClassifications()
        {
            return Ok(await _classificationService.GetAllClassificationsAsync());
        }

        [HttpGet("{imageIdentifier}")]
        public async Task<ActionResult<ImageClassification>> GetClassification(string imageIdentifier)
        {
            if (string.IsNullOrWhiteSpace(imageIdentifier))
            {
                return BadRequest("Image identifier is required.");
            }
            var classification = await _classificationService.GetClassificationForImageAsync(imageIdentifier);
            if (classification == null)
            {
                return NotFound($"No classification found for image '{imageIdentifier}'.");
            }
            return Ok(classification);
        }
    }
}
