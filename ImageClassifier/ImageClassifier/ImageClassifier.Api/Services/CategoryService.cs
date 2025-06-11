// Services/CategoryService.cs
using ImageClassifier.Api.Models;
using Microsoft.Extensions.Hosting; // For IWebHostEnvironment
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading; // Required for SemaphoreSlim

namespace ImageClassifier.Api.Services
{
    public class CategoryService
    {
        private readonly string _filePath;
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);


        public CategoryService(IWebHostEnvironment environment)
        {
            // It's generally better to store data outside the deployment directory
            // but for simplicity in this example, we place it in ContentRootPath.
            // Consider using AppData or a dedicated data directory for real applications.
            var dataFolderPath = Path.Combine(environment.ContentRootPath, "Data");
            if (!Directory.Exists(dataFolderPath))
            {
                Directory.CreateDirectory(dataFolderPath);
            }
            _filePath = Path.Combine(dataFolderPath, "categories.json");

            // Initialize file if it doesn't exist
            if (!File.Exists(_filePath))
            {
                // Ensure to use the lock here as well to prevent race conditions if multiple services are instantiated.
                _fileLock.Wait();
                try
                {
                    // Double check after acquiring lock
                    if (!File.Exists(_filePath))
                    {
                        File.WriteAllText(_filePath, "[]");
                    }
                }
                finally
                {
                    _fileLock.Release();
                }
            }
        }

        private async Task<List<Category>> ReadCategoriesFromFileAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new List<Category>();
                }
                var json = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<List<Category>>(json) ?? new List<Category>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task WriteCategoriesToFileAsync(List<Category> categories)
        {
            await _fileLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(categories, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_filePath, json);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await ReadCategoriesFromFileAsync();
        }

        public async Task<Category> GetCategoryByIdAsync(Guid id)
        {
            var categories = await ReadCategoriesFromFileAsync();
            return categories.FirstOrDefault(c => c.Id == id);
        }

        public async Task<Category> AddCategoryAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Category name cannot be empty.", nameof(name));
            }

            var categories = await ReadCategoriesFromFileAsync();
            if (categories.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Category with name '{name}' already exists.");
            }

            var category = new Category { Id = Guid.NewGuid(), Name = name };
            categories.Add(category);
            await WriteCategoriesToFileAsync(categories);
            return category;
        }

        public async Task<Category> UpdateCategoryAsync(Guid id, string newName)
        {
             if (string.IsNullOrWhiteSpace(newName))
            {
                throw new ArgumentException("New category name cannot be empty.", nameof(newName));
            }

            var categories = await ReadCategoriesFromFileAsync();
            var existingCategory = categories.FirstOrDefault(c => c.Id == id);
            if (existingCategory == null)
            {
                return null; // Or throw NotFoundException
            }

            // Check if another category with the new name already exists (excluding the current one)
            if (categories.Any(c => c.Id != id && c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Another category with name '{newName}' already exists.");
            }

            existingCategory.Name = newName;
            await WriteCategoriesToFileAsync(categories);
            return existingCategory;
        }

        public async Task<bool> DeleteCategoryAsync(Guid id)
        {
            var categories = await ReadCategoriesFromFileAsync();
            var categoryToRemove = categories.FirstOrDefault(c => c.Id == id);
            if (categoryToRemove == null)
            {
                return false; // Or throw NotFoundException
            }
            categories.Remove(categoryToRemove);
            await WriteCategoriesToFileAsync(categories);
            return true;
        }
    }
}
