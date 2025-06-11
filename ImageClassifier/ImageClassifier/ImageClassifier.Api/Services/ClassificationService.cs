// Services/ClassificationService.cs
using ImageClassifier.Api.Models;
using Microsoft.Extensions.Hosting; // For IWebHostEnvironment
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading; // For SemaphoreSlim
using System.Threading.Tasks;

namespace ImageClassifier.Api.Services
{
    public class ClassificationService
    {
        private readonly string _filePath;
        private readonly CategoryService _categoryService; // To validate CategoryId if needed
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public ClassificationService(IWebHostEnvironment environment, CategoryService categoryService)
        {
            _categoryService = categoryService;
            var dataFolderPath = Path.Combine(environment.ContentRootPath, "Data");
            if (!Directory.Exists(dataFolderPath))
            {
                Directory.CreateDirectory(dataFolderPath);
            }
            _filePath = Path.Combine(dataFolderPath, "classifications.json");

            if (!File.Exists(_filePath))
            {
                // Ensure to use the lock here as well
                _fileLock.Wait();
                try
                {
                    if (!File.Exists(_filePath)) // Double check after acquiring lock
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

        private async Task<List<ImageClassification>> ReadClassificationsFromFileAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_filePath)) return new List<ImageClassification>();
                var json = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<List<ImageClassification>>(json) ?? new List<ImageClassification>();
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task WriteClassificationsToFileAsync(List<ImageClassification> classifications)
        {
            await _fileLock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(classifications, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_filePath, json);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<ImageClassification> AddOrUpdateClassificationAsync(string imageIdentifier, Guid categoryId)
        {
            // Optional: Validate categoryId exists
            var categoryExists = await _categoryService.GetCategoryByIdAsync(categoryId);
            if (categoryExists == null)
            {
                throw new ArgumentException("Invalid Category ID.", nameof(categoryId));
            }

            var classifications = await ReadClassificationsFromFileAsync();
            var existingClassification = classifications.FirstOrDefault(c => c.ImageIdentifier.Equals(imageIdentifier, StringComparison.OrdinalIgnoreCase));

            if (existingClassification != null)
            {
                existingClassification.CategoryId = categoryId;
                existingClassification.ClassifiedAt = DateTime.UtcNow;
            }
            else
            {
                existingClassification = new ImageClassification
                {
                    ImageIdentifier = imageIdentifier,
                    CategoryId = categoryId,
                    ClassifiedAt = DateTime.UtcNow
                };
                classifications.Add(existingClassification);
            }

            await WriteClassificationsToFileAsync(classifications);
            return existingClassification;
        }

        public async Task<List<ImageClassification>> GetAllClassificationsAsync()
        {
            return await ReadClassificationsFromFileAsync();
        }

        public async Task<ImageClassification> GetClassificationForImageAsync(string imageIdentifier)
        {
            var classifications = await ReadClassificationsFromFileAsync();
            return classifications.FirstOrDefault(c => c.ImageIdentifier.Equals(imageIdentifier, StringComparison.OrdinalIgnoreCase));
        }
    }
}
