using System.Text.Json.Serialization;

namespace PolicyProof.Models;

public class UploadModel
{
    public IFormFile? RequirementsFile { get; set; }
    public IFormFile? ResponseFile { get; set; }
}

public class ComplianceResult
{
    [JsonPropertyName("summary")]
    public ComplianceSummary Summary { get; set; } = new();

    [JsonPropertyName("requirements")]
    public List<ComplianceItem> Requirements { get; set; } = [];
}

public class ComplianceSummary
{
    [JsonPropertyName("total_requirements")]
    public int TotalRequirements { get; set; }

    [JsonPropertyName("green_count")]
    public int GreenCount { get; set; }

    [JsonPropertyName("yellow_count")]
    public int YellowCount { get; set; }

    [JsonPropertyName("red_count")]
    public int RedCount { get; set; }

    [JsonPropertyName("overall_score")]
    public int OverallScore { get; set; }

    [JsonPropertyName("overall_assessment")]
    public string OverallAssessment { get; set; } = string.Empty;
}

public class ComplianceItem
{
    [JsonPropertyName("requirement_id")]
    public string RequirementId { get; set; } = string.Empty;

    [JsonPropertyName("requirement")]
    public string Requirement { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("evidence_quote")]
    public string EvidenceQuote { get; set; } = string.Empty;

    [JsonPropertyName("citation")]
    public string Citation { get; set; } = string.Empty;

    [JsonPropertyName("gap_description")]
    public string GapDescription { get; set; } = string.Empty;

    [JsonPropertyName("suggested_fix")]
    public string SuggestedFix { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = string.Empty;
}

public class AnalysisViewModel
{
    public ComplianceResult Result { get; set; } = new();
    public string RequirementsFileName { get; set; } = string.Empty;
    public string ResponseFileName { get; set; } = string.Empty;
    public TimeSpan AnalysisDuration { get; set; }
    public string? ErrorMessage { get; set; }
}
