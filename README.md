# PolicyProof - AI Compliance Checker

**One-line pitch:** Upload a requirements/policy document and a draft response; produce a Red/Yellow/Green compliance matrix with missing requirements, risk flags, suggested fixes, and citations to source text.

## Architecture

`
User uploads 2 files (Requirements + Draft Response)
  |
  v
TextExtractorService (.txt / .docx / .pdf)
  |
  v
PiiMaskerService (SSN, email, phone, CC, API keys)
  |
  v
ComplianceAnalyzerService
  |- Single-pass (< 80K tokens): one Azure OpenAI call
  |- Two-pass chunked (>= 80K tokens):
  |    Pass 1: Extract requirements list
  |    Pass 2: Sliding-window chunks with overlap
  |    Merge: Deduplicate, keep best status per requirement
  |
  v
Compliance Matrix UI (summary + filterable table)
`

## Safety and Security

- **Prompt injection defense**: System prompt explicitly instructs the model to ignore instructions embedded in uploaded documents
- **No citation, no claim**: Every compliance status requires a direct quote from the source document
- **PII masking**: SSNs, emails, phone numbers, credit cards, and API keys/secrets are redacted before sending to Azure OpenAI
- **Structured output**: JSON response format enforced via API parameter

## Quick Start

1. Configure Azure OpenAI credentials:
`ash
cd PolicyProof
dotnet user-secrets set AzureOpenAI:Endpoint https://YOUR-RESOURCE.openai.azure.com/
dotnet user-secrets set AzureOpenAI:ApiKey YOUR-KEY
dotnet user-secrets set AzureOpenAI:DeploymentName gpt-4o
`

2. Run: dotnet run

3. Navigate to https://localhost:5001 and upload your documents.

## Tech Stack

- .NET 10 / ASP.NET Core MVC
- Azure OpenAI (GPT-4o)
- PdfPig (Apache 2.0) for PDF extraction
- DocumentFormat.OpenXml for DOCX extraction
- Bootstrap 5 + Bootstrap Icons

## Demo Script (60-90 seconds)

2. **Upload** (10s): Upload a sample RFP and proposal response.
4. **Results** (20s): Show the compliance matrix. Point out the overall score, Red items with suggested fixes, and evidence citations.

1. **The Problem** (10s): Compliance teams manually check proposals against RFP requirements. Hours of work, things get missed.
2. **Upload** (10s): Upload a sample RFP and proposal response.
3. **Processing** (20-30s): PolicyProof extracts text, masks PII, and sends both documents to Azure OpenAI with a strict compliance auditor prompt.
4. **Results** (20s): Show the compliance matrix with overall score, Red items with suggested fixes, and evidence citations.
5. **Filtering** (10s): Filter to show only Red/Yellow items - these are your action items.
6. **Safety** (10s): We defend against prompt injection, require citations for every claim, and mask PII before it reaches the model.