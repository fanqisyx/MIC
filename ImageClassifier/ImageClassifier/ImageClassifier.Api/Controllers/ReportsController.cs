// ImageClassifier/ImageClassifier/ImageClassifier.Api/Controllers/ReportsController.cs
using ImageClassifier.Api.Models;
using ImageClassifier.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Text;
using System.IO; // For File, Path
using System.Linq; // For SelectMany, Distinct
using System.Collections.Generic; // For List
using Microsoft.Extensions.Logging; // For ILogger

namespace ImageClassifier.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly LatexReportService _latexReportService;
        private readonly ILogger<ReportsController> _logger; // Added ILogger

        public ReportsController(LatexReportService latexReportService, ILogger<ReportsController> logger) // Added ILogger
        {
            _latexReportService = latexReportService;
            _logger = logger;
        }

        [HttpPost("generate-tex")]
        public async Task<IActionResult> GenerateTexReport([FromBody] GenerateReportRequest request)
        {
            if (request == null)
            {
                request = new GenerateReportRequest();
            }
            if (request.SamplesPerCategory < 0 || request.SamplesPerCategory > 25) {
                return BadRequest("Samples per category must be between 0 and 25.");
            }
            if (string.IsNullOrWhiteSpace(request.Title)) {
                request.Title = "Image Classification Report";
            }

            var reportData = await _latexReportService.PrepareReportDataAsync(request);
            var texString = _latexReportService.GenerateTexString(reportData);
            return Content(texString, "application/x-latex", Encoding.UTF8);
        }

        // New endpoint for PDF generation
        [HttpPost("generate-pdf")]
        public async Task<IActionResult> GeneratePdfReport([FromBody] GenerateReportRequest request)
        {
            if (request == null)
            {
                request = new GenerateReportRequest();
            }
            if (request.SamplesPerCategory < 0 || request.SamplesPerCategory > 25) {
                return BadRequest("Samples per category must be between 0 and 25.");
            }
            if (string.IsNullOrWhiteSpace(request.Title)) {
                request.Title = "Image Classification Report";
            }

            _logger.LogInformation("Received request to generate PDF report with title: {Title}, SamplesPerCategory: {SamplesPerCategory}", request.Title, request.SamplesPerCategory);

            try
            {
                var reportData = await _latexReportService.PrepareReportDataAsync(request);

                // Extract all unique image filenames required for the report
                List<string> requiredImageFilenames = new List<string>();
                if (reportData.SamplesPerCategory > 0 && reportData.CategoryStats != null)
                {
                    requiredImageFilenames = reportData.CategoryStats
                                               .Where(cs => cs.SampleImageIdentifiers != null)
                                               .SelectMany(cs => cs.SampleImageIdentifiers)
                                               .Distinct()
                                               .ToList();
                }
                _logger.LogInformation("Extracted {Count} unique image filenames for the report.", requiredImageFilenames.Count);

                var texString = _latexReportService.GenerateTexString(reportData);
                var pdfPath = await _latexReportService.CompileTexToPdfAsync(texString, requiredImageFilenames);

                if (string.IsNullOrEmpty(pdfPath) || !System.IO.File.Exists(pdfPath)) // System.IO.File for clarity
                {
                    _logger.LogError("PDF path is null or file does not exist after compilation: {PdfPath}", pdfPath);
                    return StatusCode(500, "PDF generation failed or file not found.");
                }

                _logger.LogInformation("PDF generated successfully at {PdfPath}. Preparing to stream.", pdfPath);

                var memoryStream = new MemoryStream();
                using (var stream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) // Allow ReadWrite share for robustness
                {
                    await stream.CopyToAsync(memoryStream);
                }
                memoryStream.Position = 0;

                // It's good practice to delete the temporary directory after use.
                // This can be tricky if the file is still being streamed.
                // A more robust solution might involve a background cleanup task.
                // For now, let's try to delete it. The FileShare.ReadWrite might help.
                try
                {
                    var tempDir = Path.GetDirectoryName(pdfPath);
                    if (tempDir != null && Directory.Exists(tempDir)) {
                        _logger.LogInformation("Attempting to delete temporary directory: {TempDir}", tempDir);
                        Directory.Delete(tempDir, true); // true for recursive delete
                        _logger.LogInformation("Successfully deleted temporary directory: {TempDir}", tempDir);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete temporary report directory {TempDir}. It may need manual cleanup.", Path.GetDirectoryName(pdfPath));
                }

                var pdfFileName = $"Report_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
                return File(memoryStream, "application/pdf", pdfFileName);

            }
            catch (InvalidOperationException ex) // Catch specific exceptions from LatexReportService
            {
                _logger.LogError(ex, "Error during PDF generation (InvalidOperation): {ErrorMessage}", ex.Message);
                return StatusCode(500, $"Error generating PDF: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during PDF generation: {ErrorMessage}", ex.Message);
                return StatusCode(500, "An unexpected error occurred while generating the PDF report.");
            }
        }
    }
}

// Modification for Program.cs to include ILogger<ReportsController> (Conceptual)
// This subtask will only modify ReportsController.cs.
// The subtask runner should be aware that ILogger<ReportsController> is now a dependency.
