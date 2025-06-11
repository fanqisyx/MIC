// ImageClassifier/ImageClassifier/ImageClassifier.Api/Models/ReportModels.cs
using System;
using System.Collections.Generic;

namespace ImageClassifier.Api.Models
{
    public class ReportCategoryStatistic
    {
        public string CategoryName { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public List<string> SampleImageIdentifiers { get; set; }
    }

    public class ReportData
    {
        public DateTime ReportDate { get; set; }
        public int TotalImages { get; set; }
        public int ClassifiedImages { get; set; }
        public int UnclassifiedImages { get; set; }
        public List<ReportCategoryStatistic> CategoryStats { get; set; }
        public string Title { get; set; }
        public int SamplesPerCategory { get; set; }
    }

    public class GenerateReportRequest
    {
        public string Title { get; set; } = "Image Classification Report";
        public int SamplesPerCategory { get; set; } = 3; // Default, can be 1-25, validated in controller
    }
}
