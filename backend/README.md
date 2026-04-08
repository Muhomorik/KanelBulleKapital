# KanelBrief Backend

Cloud-native agent-based financial news analysis pipeline demonstrating **Microsoft Agent Framework** and **Azure AI Foundry** proficiency.

## 🏗️ Architecture

### Project Structure

```
backend/
├── KanelBrief.Core/
│   ├── Models/              # Domain models (Azure-free POCOs)
│   ├── Repositories/        # IAgentRunRepository interface
│   └── KanelBrief.Core.csproj
├── KanelBrief.Functions/
│   ├── Agents/              # Agent implementations
│   ├── Api/                 # Read APIs for retrieving runs
│   ├── Orchestration/       # Pipeline orchestration (timers)
│   ├── Repositories/        # Azure Tables implementation
│   ├── Program.cs           # DI setup & Azure configuration
│   └── KanelBrief.Functions.csproj
├── KanelBrief.Core.Tests/   # Core domain tests
├── KanelBrief.Functions.Tests/ # Function & repository tests
└── README.md                # This file
```

### Data Flow

```
Timer Trigger (Daily)
  ↓
NewsBriefAgent (HTTP) → saves NewsBriefRun
  ↓
[Stored in Azure Tables]
  ↓
Timer Trigger (Weekly)
  ↓
WeeklySummaryAgent (HTTP) → aggregates week's briefs → saves WeeklySummaryRun
  ↓
SubstitutionChainAgent (HTTP) → analyzes rotations → saves SubstitutionChainRun
  ↓
OpportunityScanAgent (HTTP) → evaluates opportunities → saves OpportunityScanRun
  ↓
AgentRunsApi (GET endpoints) → retrieve runs by date/ID
```

## ✅ Completed

### 1. Microsoft Agent Framework Integration (Phase 1)

- **NewsBriefAgent**: Uses `AIProjectClient.AsAIAgent()` to analyze news articles with gpt-4o-mini
  - Agent generates mood assessment and category-level sentiment analysis
  - Parses JSON response and populates structured run data
  - Fallback mechanism if LLM call fails
- **WeeklySummaryAgent**: Aggregates daily briefs into weekly themes
  - Analyzes week's sentiment trends via agent
  - Extracts recurring market themes with confidence levels
- **SubstitutionChainAgent**: Identifies capital rotation paths
  - Analyzes sentiment transitions to find fleeing/flowing-to sectors
  - Returns rotation chains with mechanisms
- **OpportunityScanAgent**: Evaluates rotation opportunities
  - Scores opportunities by signal strength (Strong/Moderate/Weak)
  - Includes risk caveats and rationale
- **DI Setup**: `AIProjectClient` registered in Program.cs (singleton)
  - Configured with Foundry project endpoint (from `FOUNDRY_PROJECT_ENDPOINT` config)
  - Uses `DefaultAzureCredential` for Azure authentication
- **Error Handling**: Each agent has fallback placeholder data if LLM call fails
- **Model**: gpt-4o-mini (from available Foundry models)

### 2. DateTimeOffset for Timezone Awareness

- Replaced all `DateTime.UtcNow` with `DateTimeOffset.UtcNow`
- Added `CreatedAt` field to `AgentRunBase` to preserve timezone info
- Ensures accurate timestamps across regions (webapp may run in different timezones)
- `RunDate` remains as `string "yyyy-MM-dd"` for Azure Tables PartitionKey compatibility

### 3. Enum Values Aligned with Domain Model

- **MarketSentiment**: RiskOn, RiskOff, Mixed (not Bullish/Bearish/Neutral)
- **ConfidenceLevel**: High, Medium, Low
- **SignalStrength**: Strong, Moderate, Weak (not Medium)
- Agent instructions updated to reflect correct enum values
- Parser methods handle enum conversions with sensible defaults

### 4. Core Domain Models
- **NewsBriefRun**: Daily market news analysis with sentiment & category assessments
- **WeeklySummaryRun**: Aggregated market themes and mood for a given week
- **SubstitutionChainRun**: Identified capital rotation paths (from/to sectors)
- **OpportunityScanRun**: Actionable investment opportunities with signal strength & risk caveats

All models in `KanelBrief.Core/Models/` are **Azure-free** (plain POCOs using standard enums and types).

### 2. Repository Pattern & DI
- **IAgentRunRepository**: Interface in Core with 12 methods (save/get by ID/get by date)
- **AgentRunRepository**: Azure Tables implementation in Functions
  - JSON serialization for nested types (CategoryAssessment, RotationChain, etc.)
  - Enum fields converted to strings
  - CamelCase naming policy for JSON
  - Composite key: PartitionKey (RunDate) + RowKey (RunId)
- **DI Registration** in Program.cs:
  - TableServiceClient (singleton)
  - 4 TableClient instances (one per run type)
  - IAgentRunRepository as scoped service

### 3. Agent Functions
All agents are HTTP-triggered with identical structure:

#### NewsBriefAgent
- **Route**: `POST /api/news-brief`
- **Input**: NewsArticle array (Title, Content, Category)
- **Output**: NewsBriefRun with mood & category assessments
- **Placeholder**: GenerateSummary() & GenerateAssessments() return hardcoded values

#### WeeklySummaryAgent
- **Route**: `POST /api/weekly-summary`
- **Input**: WeeklySummaryRequest (WeekStart, WeekEnd, DailyBriefRunIds)
- **Output**: WeeklySummaryRun with aggregated themes & market mood
- **Placeholder**: GenerateThemes() returns sample tech theme

#### SubstitutionChainAgent
- **Route**: `POST /api/substitution-chain`
- **Input**: SubstitutionChainRequest (WeeklySummaryRunId)
- **Output**: SubstitutionChainRun with capital rotation paths
- **Placeholder**: GenerateChains() returns Energy→Technology example

#### OpportunityScanAgent
- **Route**: `POST /api/opportunity-scan`
- **Input**: OpportunityScanRequest (SubstitutionChainRunId)
- **Output**: OpportunityScanRun with investment targets
- **Placeholder**: GenerateTargets() returns sample opportunity with Strong signal

### 4. Pipeline Orchestration
**DailyPipelineOrchestrator** with timer triggers:
- **DailyNewsBriefTimer** (`0 8 * * *`): Executes daily at 8 UTC
- **WeeklyAggregationTimer** (`0 9 * * 1`): Executes Mondays at 9 UTC
  - Fetches all NewsBriefRuns from previous week
  - TODO: Chain agents (Weekly Summary → Substitution Chain → Opportunity Scan)

### 5. Read API
**AgentRunsApi** with 8 GET endpoints:
- `/runs/news-briefs?date=yyyy-MM-dd` → List all runs for date
- `/runs/news-briefs/{runDate}/{runId}` → Get specific run
- `/runs/weekly-summaries?date=yyyy-MM-dd` → List weekly summaries
- `/runs/weekly-summaries/{runDate}/{runId}` → Get specific summary
- `/runs/substitution-chains?date=yyyy-MM-dd` → List substitution chains
- `/runs/substitution-chains/{runDate}/{runId}` → Get specific chain
- `/runs/opportunity-scans?date=yyyy-MM-dd` → List opportunity scans
- `/runs/opportunity-scans/{runDate}/{runId}` → Get specific scan

Returns:
- **400 Bad Request** if date query parameter missing
- **404 Not Found** if run doesn't exist
- **200 OK** with JSON run data (CamelCase serialization)

## 🚧 Remaining Steps

### Phase 2: Unit Testing

- [x] **Cron Schedule Tests**: Verify timer triggers fire at correct times using Cronos library
- [ ] **AgentRunRepository Tests**: Mock TableClient, test Save/Get operations, verify JSON serialization
- [ ] **Agent Function Tests**: Mock IAgentRunRepository, test HTTP request parsing, verify run creation
- [ ] **Model Validation Tests**: Verify enums, date formatting, required fields

**Tech Stack**: NUnit + AutoFixture + AutoMoq (per CLAUDE.md guidelines)

### Phase 3: CI/CD Pipeline

- [x] **PR Checks** (pr-checks.yml): Run on PR to main/develop
  - Build backend with .NET 9.0
  - Run all unit tests
- [x] **Deploy Backend** (deploy-backend.yml): Run on push to main
  - Build + test backend
  - Publish to Azure Functions (Flex Consumption via One Deploy)
- [ ] **Deploy Frontend** (deploy-frontend.yml): Run on push to main (deferred — no frontend yet)
  - Build + test frontend
  - Deploy to Azure Static Web Apps
- [x] **Branch Protection**: Protect `main` branch (require PR, CI/CD checks pass, restrict push)

**Tech Stack**: GitHub Actions (reference: [SemanticKernel repo workflows](https://github.com/Muhomorik/SemanticKernel-FundDocsQnA-dotnet-nextjs/tree/main/.github/workflows))

### Phase 4: Azure Deployment

- [ ] Secure Azure Tables access (Managed Identity + RBAC, disable shared keys)
- [ ] Set up Azure AI Foundry deployments (models, endpoints)
- [ ] Store secrets in Azure Key Vault:
  - AI Foundry API keys & endpoints
- [ ] Test on Azure Functions (Flex Consumption)

### Phase 5: Frontend Integration

- [ ] Wire Next.js frontend to read API endpoints
- [ ] Display run history (filter by date/type)
- [ ] Show real-time agent execution status
- [ ] Visualize market themes & rotation paths

## 🔧 Local Development

### Prerequisites
- .NET 9.0 SDK
- Azure Storage Emulator (Azurite) or Azure Storage account
- Visual Studio / VS Code with C# extension

### Build
```bash
cd backend
dotnet build
```

### Test
```bash
dotnet test
```

### Run (Local Azure Functions)
```bash
cd KanelBrief.Functions
func start
```

Default endpoints:
- News Brief: `POST http://localhost:7071/api/news-brief`
- Weekly Summary: `POST http://localhost:7071/api/weekly-summary`
- Substitution Chain: `POST http://localhost:7071/api/substitution-chain`
- Opportunity Scan: `POST http://localhost:7071/api/opportunity-scan`
- Get Runs: `GET http://localhost:7071/runs/{type}?date=yyyy-MM-dd`

## 📋 Configuration

### appsettings.json
```json
{
  "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;...",
  "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
}
```

### User Secrets (Local Dev)
```bash
dotnet user-secrets set "AzureWebJobsStorage" "UseDevelopmentStorage=true"
```

### Environment Variables (Azure Functions)
Set in Azure Portal → Function App → Configuration:
- `AzureWebJobsStorage`: Connection string to Azure Tables
- `AI_FOUNDRY_API_KEY`: Azure AI Foundry API key
- `AI_FOUNDRY_ENDPOINT`: Azure AI Foundry endpoint

## 🎯 Key Design Decisions

1. **Azure-Free Core**: Domain models in Core have zero Azure dependencies. This enables unit testing without Azure mocks and allows repository swapping.

2. **Repository Abstraction**: IAgentRunRepository interface shields agents from storage implementation details. Easy to swap Azure Tables for Cosmos DB, SQL, or in-memory storage.

3. **Placeholder LLM Logic**: Agent functions currently return hardcoded data with clear TODO markers. This compiles and demonstrates the pattern while deferring LLM integration (which requires Agent Framework API stabilization).

4. **Timer-Based Orchestration**: Azure Functions TimerTrigger schedules daily briefs and weekly aggregation. Weekly timer orchestrator fetches daily runs and chains remaining agents.

5. **Composite Keys**: Azure Tables uses RunDate (PartitionKey) + RunId (RowKey) for efficient date-based queries.

6. **JSON Serialization**: Nested complex types (assessments, chains, targets) stored as JSON strings in table columns, deserialized on retrieval.

## 📚 References

- [Microsoft Agent Framework](https://learn.microsoft.com/en-us/azure/ai-foundry/concepts/agents) (preview/stable)
- [Azure AI Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/)
- [Azure Tables Storage](https://learn.microsoft.com/en-us/azure/storage/tables/table-storage-overview)
- [Azure Functions Dependency Injection](https://learn.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection)
- [CLAUDE.md](../CLAUDE.md) - Project conventions & testing guidelines

## 🚀 Next Steps

1. **Run tests locally** to verify repository & agent logic
2. **Integrate Agent Framework** with Azure AI Foundry (after API stabilizes)
3. **Deploy to Azure** and configure Key Vault secrets
4. **Connect frontend** to read API endpoints
5. **Monitor & iterate** via Application Insights
