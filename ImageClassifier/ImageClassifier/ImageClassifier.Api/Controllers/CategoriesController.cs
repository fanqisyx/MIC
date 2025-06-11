// Controllers/CategoriesController.cs
using ImageClassifier.Api.Models;
using ImageClassifier.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Required for StatusCodes

namespace ImageClassifier.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoriesController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Category>>> GetCategories()
        {
            return Ok(await _categoryService.GetCategoriesAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(Guid id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return Ok(category);
        }

        // DTO for adding a category to avoid requiring ID from client
        public class AddCategoryDto
        {
            public string Name { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<Category>> AddCategory([FromBody] AddCategoryDto categoryDto)
        {
            if (categoryDto == null || string.IsNullOrWhiteSpace(categoryDto.Name))
            {
                return BadRequest("Category name is required.");
            }
            try
            {
                var newCategory = await _categoryService.AddCategoryAsync(categoryDto.Name);
                return CreatedAtAction(nameof(GetCategory), new { id = newCategory.Id }, newCategory);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex) // For duplicate name
            {
                return Conflict(ex.Message);
            }
        }

        // DTO for updating a category
        public class UpdateCategoryDto
        {
            public string Name { get; set; }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto categoryDto)
        {
            if (categoryDto == null || string.IsNullOrWhiteSpace(categoryDto.Name))
            {
                return BadRequest("Category name is required.");
            }
            try
            {
                var updatedCategory = await _categoryService.UpdateCategoryAsync(id, categoryDto.Name);
                if (updatedCategory == null)
                {
                    return NotFound($"Category with ID {id} not found.");
                }
                return Ok(updatedCategory); // Or NoContent()
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex) // For duplicate name
            {
                return Conflict(ex.Message);
            }

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var success = await _categoryService.DeleteCategoryAsync(id);
            if (!success)
            {
                return NotFound($"Category with ID {id} not found.");
            }
            return NoContent();
        }
    }
}
