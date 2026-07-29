
using System.ClientModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using PolicyProof.Models;

namespace PolicyProof.Services;

public interface IComplianceAnalyzerService
{
    Task<ComplianceResult> AnalyzeAsync(string requirementsText, string responseText);
}

public class ComplianceAnalyzerService : IComplianceAnalyzerService
{
    private readonly ChatClient _chatClient;
    private readonly IPiiMaskerService _piiMasker;
    private readonly ILogger<ComplianceAnalyzerService> _logger;
    private const int MaxTokenThreshold = 80000;
    private const int CharsPerToken = 4;
    private const int ChunkSize = 32000;
    private const int ChunkOverlap = 2000;

    public ComplianceAnalyzerService(IConfiguration config, IPiiMaskerService piiMasker, ILogger<ComplianceAnalyzerService> logger)
    {
        _piiMasker = piiMasker;
        _logger = logger;
        var endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured");
        var apiKey = config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey not configured");
        var deployment = config["AzureOpenAI:DeploymentName"] ?? "gpt-4o";
        var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        _chatClient = client.GetChatClient(deployment);
    }

    public async Task<ComplianceResult> AnalyzeAsync(string requirementsText, string responseText)
    {
        var maskedReqs = _piiMasker.MaskPii(requirementsText);
        var maskedResp = _piiMasker.MaskPii(responseText);
        var combinedLength = maskedReqs.Length + maskedResp.Length;
        var estimatedTokens = combinedLength / CharsPerToken;

        if (estimatedTokens > MaxTokenThreshold)
        {
            _logger.LogInformation("Documents exceed token threshold ({Tokens} est). Using two-pass chunked analysis.", estimatedTokens);
            return await TwoPassAnalysis(maskedReqs, maskedResp);
        }

        return await SinglePassAnalysis(maskedReqs, maskedResp);
    }

    private async Task<ComplianceResult> SinglePassAnalysis(string reqs, string resp)
    {
        var userContent = "=== REQUIREMENTS DOCUMENT ===\n" + reqs + "\n\n=== DRAFT RESPONSE ===\n" + resp + "\n\nAnalyze compliance now. Return JSON only.";
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(Prompts.SystemPrompt),
            new UserChatMessage(userContent)
        };
        var options = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() };
        var response = await _chatClient.CompleteChatAsync(messages, options);
        var json = response.Value.Content[0].Text;
        return ParseResult(json);
    }

    private async Task<ComplianceResult> TwoPassAnalysis(string reqs, string resp)
    {
        // Pass 1: Extract structured requirements list
        var extractMessages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a requirements extraction specialist. Extract every discrete requirement from the document. Return JSON: {\"requirements\": [{\"id\": \"REQ-001\", \"text\": \"...\"}]}. Be exhaustive."),
            new UserChatMessage(reqs)
        };
        var jsonOptions = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() };
        var extractResponse = await _chatClient.CompleteChatAsync(extractMessages, jsonOptions);
        var reqsJson = extractResponse.Value.Content[0].Text;

        // Pass 2: Check each chunk of the response against requirements
        var chunks = ChunkText(resp, ChunkSize, ChunkOverlap);
        var allItems = new List<ComplianceItem>();

        foreach (var chunk in chunks)
        {
            var chunkContent = "=== REQUIREMENTS ===\n" + reqsJson + "\n\n=== RESPONSE CHUNK ===\n" + chunk + "\n\nAnalyze which requirements are addressed in this chunk. Return JSON only.";
            var chunkMessages = new List<ChatMessage>
            {
                new SystemChatMessage(Prompts.ChunkAnalysisPrompt),
                new UserChatMessage(chunkContent)
            };
            var chunkResponse = await _chatClient.CompleteChatAsync(chunkMessages, jsonOptions);
            var chunkResult = ParseResult(chunkResponse.Value.Content[0].Text);
            allItems.AddRange(chunkResult.Requirements);
        }

        return MergeResults(allItems);
    }

    private static List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        var pos = 0;
        while (pos < text.Length)
        {
            var end = Math.Min(pos + chunkSize, text.Length);
            chunks.Add(text[pos..end]);
            pos += chunkSize - overlap;
        }
        return chunks;
    }

    private static ComplianceResult MergeResults(List<ComplianceItem> items)
    {
        var grouped = items.GroupBy(i => i.RequirementId);
        var merged = grouped.Select(g => g.OrderBy(i => StatusRank(i.Status)).First()).ToList();
        return new ComplianceResult
        {
            Requirements = merged,
            Summary = new ComplianceSummary
            {
                TotalRequirements = merged.Count,
                GreenCount = merged.Count(i => i.Status == "Green"),
                YellowCount = merged.Count(i => i.Status == "Yellow"),
                RedCount = merged.Count(i => i.Status == "Red"),
                OverallScore = merged.Count > 0 ? (int)(merged.Count(i => i.Status == "Green") * 100.0 / merged.Count) : 0,
                OverallAssessment = "Chunked analysis complete."
            }
        };
    }

    private static int StatusRank(string status) => status switch
    {
        "Green" => 1, "Yellow" => 2, "Red" => 3, _ => 4
    };

    private static ComplianceResult ParseResult(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ComplianceResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new ComplianceResult();
        }
        catch (JsonException)
        {
            return new ComplianceResult();
        }
    }
}
