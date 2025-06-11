// Models/ImageClassification.cs
using System;

namespace ImageClassifier.Api.Models
{
    public class ImageClassification
    {
        public string ImageIdentifier { get; set; } // e.g., filename
        public Guid CategoryId { get; set; }
        public DateTime ClassifiedAt { get; set; }
    }
}
