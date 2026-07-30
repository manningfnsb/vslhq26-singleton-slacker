using System.ClientModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PolicyProof.Models;
using PolicyProof.Services;

namespace PolicyProof.Controllers;

public class AnalysisController : Controller
{
    private readonly ITextExtractorService _textExtractor;
    private readonly IComplianceAnalyzerService _analyzer;
    private readonly ILogger<AnalysisController> _logger;
    private readonly IWebHostEnvironment _env;

    public AnalysisController(ITextExtractorService textExtractor, IComplianceAnalyzerService analyzer, ILogger<AnalysisController> logger, IWebHostEnvironment env)
    {
        _textExtractor = textExtractor;
        _analyzer = analyzer;
        _logger = logger;
        _env = env;
    }

    [HttpGet]
    public IActionResult Upload() => View();

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Analyze(UploadModel model)
    {
        var vm = new AnalysisViewModel();

        if (model.RequirementsFile == null || model.ResponseFile == null)
        {
            vm.ErrorMessage = "Please upload both a requirements document and a draft response.";
            return View("Results", vm);
        }

        vm.RequirementsFileName = model.RequirementsFile.FileName;
        vm.ResponseFileName = model.ResponseFile.FileName;

        // Validate file extensions
        var allowedExtensions = new[] { ".txt", ".pdf", ".docx", ".md" };
        var reqExt = Path.GetExtension(model.RequirementsFile.FileName).ToLowerInvariant();
        var respExt = Path.GetExtension(model.ResponseFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(reqExt) || !allowedExtensions.Contains(respExt))
        {
            vm.ErrorMessage = "Unsupported file format. Please upload .txt, .pdf, .docx, or .md files.";
            return View("Results", vm);
        }

        // Validate file size (not empty)
        if (model.RequirementsFile.Length == 0 || model.ResponseFile.Length == 0)
        {
            vm.ErrorMessage = "One or both files are empty. Please upload files with content.";
            return View("Results", vm);
        }

        try
        {
            var sw = Stopwatch.StartNew();
            var reqsText = await _textExtractor.ExtractTextAsync(model.RequirementsFile);
            var respText = await _textExtractor.ExtractTextAsync(model.ResponseFile);

            if (string.IsNullOrWhiteSpace(reqsText) || string.IsNullOrWhiteSpace(respText))
            {
                vm.ErrorMessage = "Could not extract text from one or both files. Please check file format.";
                return View("Results", vm);
            }

            vm.Result = await _analyzer.AnalyzeAsync(reqsText, respText);
            sw.Stop();
            vm.AnalysisDuration = sw.Elapsed;

            if (vm.Result.Requirements == null || vm.Result.Requirements.Count == 0)
            {
                vm.ErrorMessage = "The AI could not identify any requirements in your documents. Please ensure the requirements document contains clearly defined requirements (e.g., REQ-001, Section 1.1, etc.).";
                vm.Result = null!;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Analysis timed out for {ReqFile} + {RespFile}", model.RequirementsFile.FileName, model.ResponseFile.FileName);
            vm.ErrorMessage = "Analysis timed out. Your documents may be too large for a single pass. Try splitting them into smaller sections or using shorter documents.";
        }
        catch (ClientResultException ex) when (ex.Status == 429)
        {
            _logger.LogWarning(ex, "Rate limited by Azure OpenAI");
            vm.ErrorMessage = "The AI service is currently busy. Please wait a moment and try again.";
        }
        catch (ClientResultException ex) when (ex.Status >= 500)
        {
            _logger.LogError(ex, "Azure OpenAI service error");
            vm.ErrorMessage = "The AI service encountered an error. Please try again in a few minutes.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed for {ReqFile} + {RespFile}", model.RequirementsFile.FileName, model.ResponseFile.FileName);
            vm.ErrorMessage = ex.Message switch
            {
                var m when m.Contains("timeout", StringComparison.OrdinalIgnoreCase) =>
                    "Analysis timed out. Your documents may be too large. Try using smaller files.",
                var m when m.Contains("content filter", StringComparison.OrdinalIgnoreCase) =>
                    "The AI service flagged content in your documents. Please review and remove any potentially sensitive content, then try again.",
                _ => "Something went wrong during analysis. Please try again or use different documents."
            };
        }

        return View("Results", vm);
    }

    [HttpPost]
    public async Task<IActionResult> TrySample()
    {
        var vm = new AnalysisViewModel
        {
            RequirementsFileName = "sample-requirements.txt",
            ResponseFileName = "sample-response.txt"
        };

        var samplesPath = Path.Combine(_env.WebRootPath, "samples");
        var reqsText = await System.IO.File.ReadAllTextAsync(Path.Combine(samplesPath, "sample-requirements.txt"));
        var respText = await System.IO.File.ReadAllTextAsync(Path.Combine(samplesPath, "sample-response.txt"));

        try
        {
            var sw = Stopwatch.StartNew();
            vm.Result = await _analyzer.AnalyzeAsync(reqsText, respText);
            sw.Stop();
            vm.AnalysisDuration = sw.Elapsed;

            if (vm.Result.Requirements == null || vm.Result.Requirements.Count == 0)
            {
                vm.ErrorMessage = "The AI could not identify any requirements in the sample documents.";
                vm.Result = null!;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sample analysis failed");
            vm.ErrorMessage = "Something went wrong during sample analysis. Please try again.";
        }

        return View("Results", vm);
    }
}
