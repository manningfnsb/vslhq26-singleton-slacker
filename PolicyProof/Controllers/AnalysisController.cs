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

    public AnalysisController(ITextExtractorService textExtractor, IComplianceAnalyzerService analyzer, ILogger<AnalysisController> logger)
    {
        _textExtractor = textExtractor;
        _analyzer = analyzer;
        _logger = logger;
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed");
            vm.ErrorMessage = $"Analysis failed: {ex.Message}";
        }

        return View("Results", vm);
    }
}
