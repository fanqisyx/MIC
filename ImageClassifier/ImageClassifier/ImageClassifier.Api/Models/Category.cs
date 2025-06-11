using System;

namespace ImageClassifier.Api.Models
{
    public class Category
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        // Potentially add other properties later, e.g., Color
    }
}
