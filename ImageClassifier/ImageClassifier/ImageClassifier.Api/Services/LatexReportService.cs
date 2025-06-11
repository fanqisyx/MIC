// ImageClassifier/ImageClassifier/ImageClassifier.Api/Services/LatexReportService.cs
using ImageClassifier.Api.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics; // For Process
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging; // For logging

namespace ImageClassifier.Api.Services
{
    public class LatexReportService
    {
        private readonly ClassificationService _classificationService;
        private readonly CategoryService _categoryService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<LatexReportService> _logger;

        public LatexReportService(ClassificationService classificationService,
                                  CategoryService categoryService,
                                  IWebHostEnvironment environment,
                                  ILogger<LatexReportService> logger) // Added ILogger
        {
            _classificationService = classificationService;
            _categoryService = categoryService;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ReportData> PrepareReportDataAsync(GenerateReportRequest request)
        {
            // ... (Content from previous successful step - unchanged)
            var allClassifications = await _classificationService.GetAllClassificationsAsync();
            var allCategories = await _categoryService.GetCategoriesAsync();
            var classifiedImageFiles = allClassifications.Select(c => c.ImageIdentifier).Distinct().ToList();
            var mainUploadsFolderPath = Path.Combine(_environment.ContentRootPath, "Uploads"); // Renamed for clarity
            var allUploadedImageFiles = Directory.Exists(mainUploadsFolderPath)
                                        ? Directory.GetFiles(mainUploadsFolderPath).Select(Path.GetFileName).ToList()
                                        : new List<string>();
            int totalImages = allUploadedImageFiles.Count;
            int classifiedCount = classifiedImageFiles.Count;
            var reportCategoryStats = new List<ReportCategoryStatistic>();
            foreach (var category in allCategories)
            {
                var imagesInCateg = allClassifications.Where(c => c.CategoryId == category.Id).ToList();
                var count = imagesInCateg.Count;
                var sampleIdentifiers = imagesInCateg
                                        .Select(c => c.ImageIdentifier)
                                        .Distinct()
                                        .Take(Math.Clamp(request.SamplesPerCategory, 1, 25))
                                        .ToList();
                reportCategoryStats.Add(new ReportCategoryStatistic
                {
                    CategoryName = category.Name,
                    Count = count,
                    Percentage = totalImages > 0 ? ((double)count / totalImages) * 100 : 0,
                    SampleImageIdentifiers = sampleIdentifiers
                });
            }
            return new ReportData
            {
                ReportDate = DateTime.UtcNow,
                Title = request.Title,
                SamplesPerCategory = request.SamplesPerCategory,
                TotalImages = totalImages,
                ClassifiedImages = classifiedCount,
                UnclassifiedImages = totalImages - classifiedCount,
                CategoryStats = reportCategoryStats
            };
        }

        public string GenerateTexString(ImageClassifier.Api.Models.ReportData data)
        {
            var sb = new StringBuilder();
            // Preamble (unchanged)
            sb.AppendLine(@"\documentclass[a4paper]{article}");
            sb.AppendLine(@"\usepackage[utf8]{inputenc}");
            sb.AppendLine(@"\usepackage{geometry}");
            sb.AppendLine(@"\geometry{a4paper, margin=1in}");
            sb.AppendLine(@"\usepackage{graphicx}");
            sb.AppendLine(@"\usepackage{tabularx}");
            sb.AppendLine(@"\usepackage{ctex}");
            sb.AppendLine(@"\usepackage{noto}");
            sb.AppendLine(@"\usepackage{longtable}");
            sb.AppendLine(@"\usepackage{array}");
            sb.AppendLine(@"\usepackage[T1]{fontenc}"); // Good for outputting modern fonts and for copy-paste from PDF
            sb.AppendLine(@"\usepackage{grffile}"); // Handles dots/spaces in filenames for \includegraphics
            sb.AppendLine(@"\usepackage{hyperref}");
            sb.AppendLine(@"\hypersetup{colorlinks=true, linkcolor=blue, urlcolor=blue, pdftitle={" + EscapeLatex(data.Title) +"}, pdfauthor={Image Classification Software}}");

            sb.AppendLine(@"\title{" + EscapeLatex(data.Title) + @"}");
            sb.AppendLine(@"\author{Image Classification Software}");
            sb.AppendLine(@"\date{" + data.ReportDate.ToString("MMMM dd, yyyy") + @"}");

            sb.AppendLine(@"\begin{document}");
            sb.AppendLine(@"\maketitle");
            sb.AppendLine(@"\begin{abstract}");
            sb.AppendLine(EscapeLatex("This report summarizes the image classification results."));
            sb.AppendLine(@"\end{abstract}");
            sb.AppendLine(@"\clearpage");
            sb.AppendLine(@"\section*{Summary Statistics}");
            sb.AppendLine(@"\begin{itemize}");
            sb.AppendLine(@"    \item Total Images: " + data.TotalImages);
            sb.AppendLine(@"    \item Classified Images: " + data.ClassifiedImages);
            sb.AppendLine(@"    \item Unclassified Images: " + data.UnclassifiedImages);
            sb.AppendLine(@"\end{itemize}");
            sb.AppendLine(@"\section*{Category Details}");
            sb.AppendLine(@"\begin{longtable}{|l|r|r|}");
            sb.AppendLine(@"\hline");
            sb.AppendLine(@"\textbf{Category Name} & \textbf{Count} & \textbf{Percentage (\%)} \\");
            sb.AppendLine(@"\hline");
            sb.AppendLine(@"\endfirsthead");
            sb.AppendLine(@"\hline");
            sb.AppendLine(@"\multicolumn{3}{|r|}{Continued on next page} \\");
            sb.AppendLine(@"\hline");
            sb.AppendLine(@"\endfoot");
            sb.AppendLine(@"\hline");
            sb.AppendLine(@"\endlastfoot");
            foreach (var stat in data.CategoryStats)
            {
                sb.AppendLine(EscapeLatex(stat.CategoryName) + " & " + stat.Count + " & " + stat.Percentage.ToString("F1") + @" \\");
            }
            sb.AppendLine(@"\end{longtable}");
            sb.AppendLine(@"\clearpage");

            // Update Sample Images Section to include \includegraphics
            sb.AppendLine(@"\section*{Sample Images}");
            if (data.SamplesPerCategory > 0)
            {
                foreach (var stat in data.CategoryStats)
                {
                    if (stat.SampleImageIdentifiers.Any())
                    {
                        sb.AppendLine(@"\subsection*{" + EscapeLatex("Category: " + stat.CategoryName) + @"}");
                        foreach (var imgFilename in stat.SampleImageIdentifiers)
                        {
                            // Path for \includegraphics should be relative to where .tex file is compiled
                            // We will copy images to an 'Uploads' subdirectory in the temp compile directory
                            string imagePathForLatex = EscapeLatex("Uploads/" + imgFilename);
                            sb.AppendLine(@"\begin{figure}[h!]"); // [h!] to suggest placement here
                            sb.AppendLine(@"  \centering");
                            // Use \detokenize if grffile is not enough for some complex filenames, but usually grffile handles it.
                            sb.AppendLine(@"  \includegraphics[width=0.4\textwidth,height=0.4\textheight,keepaspectratio]{" + imagePathForLatex + @"}");
                            sb.AppendLine(@"  \caption*{" + EscapeLatex(imgFilename) + @"}"); // caption* for no "Figure X:"
                            sb.AppendLine(@"\end{figure}");
                            sb.AppendLine(@"\vspace{0.5cm}");
                        }
                        sb.AppendLine(@"\clearpage");
                    }
                }
            }
            else
            {
                sb.AppendLine(EscapeLatex("Sample image display was not requested (0 samples per category)."));
            }

            sb.AppendLine(@"\end{document}");
            return sb.ToString();
        }

        private string EscapeLatex(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace(@"\", @"\textbackslash{}")
                       .Replace("{", @"\{").Replace("}", @"\}")
                       .Replace("_", @"\_").Replace("^", @"\^{}")
                       .Replace("&", @"\&").Replace("%", @"\%")
                       .Replace("$", @"\$").Replace("#", @"\#")
                       .Replace("~", @"\textasciitilde{}");
        }

        // New method for PDF compilation
        public async Task<string> CompileTexToPdfAsync(string texContent, List<string> requiredImageFilenames)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "ImageClassifierReports", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);
            var texFilePath = Path.Combine(tempDirectory, "report.tex");
            var pdfFilePath = Path.Combine(tempDirectory, "report.pdf");
            var logFilePath = Path.Combine(tempDirectory, "report.log");

            await File.WriteAllTextAsync(texFilePath, texContent, Encoding.UTF8);
            _logger.LogInformation("LaTeX file written to: {TexFilePath}", texFilePath);

            // Copy required images to a subdirectory within the tempDirectory
            var tempUploadsDir = Path.Combine(tempDirectory, "Uploads");
            Directory.CreateDirectory(tempUploadsDir);
            var mainUploadsDir = Path.Combine(_environment.ContentRootPath, "Uploads");

            foreach (var filename in requiredImageFilenames.Distinct())
            {
                var sourceImagePath = Path.Combine(mainUploadsDir, filename);
                var destImagePath = Path.Combine(tempUploadsDir, filename);
                if (File.Exists(sourceImagePath))
                {
                    File.Copy(sourceImagePath, destImagePath, true);
                }
                else
                {
                    _logger.LogWarning("Required image not found for report: {SourceImagePath}", sourceImagePath);
                    // Optionally, create a placeholder image or log this in the .tex file
                }
            }
            _logger.LogInformation("Required images copied to temporary Uploads folder: {TempUploadsDir}", tempUploadsDir);


            var processStartInfo = new ProcessStartInfo
            {
                FileName = "pdflatex",
                Arguments = $"-interaction=nonstopmode -output-directory=\"{tempDirectory}\" \"{texFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = tempDirectory // Set working directory
            };

            _logger.LogInformation("Running pdflatex with command: {FileName} {Arguments}", processStartInfo.FileName, processStartInfo.Arguments);
            _logger.LogInformation("Working directory: {WorkingDirectory}", processStartInfo.WorkingDirectory);


            using (var process = Process.Start(processStartInfo))
            {
                if (process == null)
                {
                    _logger.LogError("Failed to start pdflatex process. Ensure LaTeX is installed and in PATH.");
                    throw new InvalidOperationException("Failed to start pdflatex process. Ensure LaTeX is installed and in PATH.");
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(); // Use async version

                _logger.LogInformation("pdflatex Output: {Output}", output);
                if (process.ExitCode != 0)
                {
                    _logger.LogError("pdflatex failed with exit code {ExitCode}. Error: {Error}. Log file at {LogFilePath}", process.ExitCode, error, logFilePath);
                    // Attempt to read the log file for more detailed errors
                    if (File.Exists(logFilePath)) {
                        string latexLog = await File.ReadAllTextAsync(logFilePath);
                        _logger.LogError("LaTeX Log File Content: {LatexLog}", latexLog);
                         throw new InvalidOperationException($"pdflatex failed. Exit Code: {process.ExitCode}. Check logs at {logFilePath}. Error stream: {error}. Detailed log: {latexLog}");
                    }
                    throw new InvalidOperationException($"pdflatex failed. Exit Code: {process.ExitCode}. Check logs at {logFilePath}. Error stream: {error}");
                }

                // Run pdflatex a second time for cross-references if necessary (often good practice)
                 _logger.LogInformation("Running pdflatex a second time for cross-references.");
                using (var process2 = Process.Start(processStartInfo)) { // Reuse start info
                    if (process2 == null) throw new InvalidOperationException("Failed to start pdflatex (2nd run).");
                    await process2.WaitForExitAsync();
                     _logger.LogInformation("pdflatex second run completed with exit code {ExitCode}.", process2.ExitCode);
                }


                if (File.Exists(pdfFilePath))
                {
                    _logger.LogInformation("PDF successfully generated: {PdfFilePath}", pdfFilePath);
                    return pdfFilePath;
                }

                _logger.LogError("PDF file not found after pdflatex execution, even though exit code was 0. Log: {LogFilePath}", logFilePath);
                if (File.Exists(logFilePath)) {
                        string latexLog = await File.ReadAllTextAsync(logFilePath);
                        _logger.LogError("LaTeX Log File Content (PDF not found case): {LatexLog}", latexLog);
                }
                throw new InvalidOperationException("PDF file not found after pdflatex execution. Check logs.");
            }
        }
    }
}

// Modification for Program.cs to include ILogger (Conceptual - this needs to be done in Program.cs, not here)
// In Program.cs, logging is typically configured by default.
// For example, builder.Logging.AddConsole(); is common.
// Ensure LatexReportService can receive ILogger<LatexReportService> via DI.
// This subtask will only modify LatexReportService.cs.
// The subtask runner should be aware that ILogger<LatexReportService> is now a dependency.
