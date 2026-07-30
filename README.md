# PolicyProof

AI-powered compliance analysis that instantly maps your draft response against policy requirements.

## Team

- **Team name:** singleton-slacker
- **Members:** Cliff Manning (@manningfnsb)

## Category

- **Primary:** .NET business app
- **Secondary:** Azure OpenAI/LLM app

## What it does

PolicyProof is an enterprise compliance workflow application for procurement and proposal teams. Organizations spend hours manually cross-referencing RFP requirements against draft responses to check compliance. PolicyProof automates this workflow using Azure OpenAI to analyze both documents, producing a decision-ready compliance matrix with per-requirement status (compliant/partial/non-compliant), evidence quotes, gap descriptions, confidence scores, and suggested fixes — typically in 30–60 seconds.

In short: this is a **business app first**, with LLM capabilities embedded to deliver the business outcome faster and more consistently.

## Architecture

```
┌──────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Browser    │────▶│  ASP.NET Core    │────▶│  Azure OpenAI   │
│  (Upload +   │◀────│  MVC App         │◀────│  (gpt-5-mini)   │
│   Results)   │     │                  │     │                 │
└──────────────┘     │  ┌────────────┐  │     └─────────────────┘
					 │  │ Text       │  │
					 │  │ Extractor  │  │
					 │  ├────────────┤  │
					 │  │ PII Masker │  │
					 │  ├────────────┤  │
					 │  │ Compliance │  │
					 │  │ Analyzer   │  │
					 │  └────────────┘  │
					 └──────────────────┘
```

1. User uploads a requirements document and a draft response
2. **TextExtractorService** extracts text from .txt, .docx, or .pdf
3. **PiiMaskerService** redacts personally identifiable information before sending to the model
4. **ComplianceAnalyzerService** sends masked content to Azure OpenAI with structured prompts
5. Results rendered as an interactive compliance matrix with filtering, expandable details, and CSV export

### Safety & Security

- **Prompt injection defense:** System prompt instructs the model to ignore instructions embedded in uploaded documents
- **No citation, no claim:** Every compliance status requires a direct quote from the source document
- **PII masking:** SSNs, emails, phone numbers, credit cards, and API keys are redacted before sending to Azure OpenAI
- **Structured output:** JSON response format enforced via API parameter

## Tech stack

- **Language:** C# 14
- **Framework:** ASP.NET Core (.NET 10)
- **AI model:** Azure OpenAI — gpt-5-mini
- **Hosting:** Azure App Service
- **Libraries:** PdfPig (PDF extraction), DocumentFormat.OpenXml (DOCX), Bootstrap 5, Bootstrap Icons

## Getting started

### Prerequisites

- .NET 10 SDK
- Azure OpenAI resource with a gpt-5-mini deployment

### Setup

```sh
# Clone the repo
git clone https://github.com/manningfnsb/vslhq26-singleton-slacker.git
cd vslhq26-singleton-slacker

# Configure secrets
cd PolicyProof
dotnet user-secrets set AzureOpenAI:Endpoint "https://YOUR-RESOURCE.openai.azure.com/"
dotnet user-secrets set AzureOpenAI:ApiKey "YOUR-KEY"
dotnet user-secrets set AzureOpenAI:DeploymentName "gpt-5-mini"

# Run
dotnet run
```

### Configuration

| Variable | Description |
|----------|-------------|
| `AzureOpenAI:Endpoint` | Your Azure OpenAI resource endpoint URL |
| `AzureOpenAI:ApiKey` | API key for the resource |
| `AzureOpenAI:DeploymentName` | Model deployment name (e.g., `gpt-5-mini`) |

> ⚠️ Do NOT commit secrets. Use App Service configuration or `dotnet user-secrets` locally.

## Demo

- Video file in this repo: `./demo/demo.mp4`
- Azure Deployment: https://app-i2-policyproof-dna9e8gta2axeydt.canadacentral-01.azurewebsites.net/

## Known limitations

- No authentication or multi-user support — single-user sessions only
- Very large documents may approach Azure OpenAI token limits
- Analysis takes 30–60 seconds depending on document size
- PII masking uses pattern-based detection (not ML) — edge cases may slip through

## License

Apache-2.0
