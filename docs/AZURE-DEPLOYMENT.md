# Azure Deployment Guide

Complete guide for deploying FikaForecast resources to Azure.

## Instructions for AI Agents

**CRITICAL:** When editing this document, AI assistants MUST verify before saving:

- [ ] No real Azure resource names — use `<your-...>` placeholders
- [ ] No real endpoint URLs — use `<your-...>` placeholders
- [ ] No API keys, tokens, or secrets in plain text

## Resource Group and Cost Management

All resources live in a dedicated resource group: `<your-resource-group>`

### Budget Alert

- **Threshold:** $40/month
- **Alerts:** Email at 50% ($20) and 80% ($32)
- **Alert recipients:** Resource group owner

### Estimated Monthly Costs

| Resource | Pricing | Estimated Cost (light use) | Estimated Cost (daily use) |
| --- | --- | --- | --- |
| **AI Foundry resource** | Free (no idle cost) | $0 | $0 |
| **gpt-5.4-mini** (Global Standard) | Pay per token | ~$0.01/brief | ~$0.30/month |
| **gpt-5.4** (Global Standard) | Pay per token | ~$0.05-0.10/brief | ~$1.50-3.00/month |
| **gpt-5.4-nano** (Global Standard) | Pay per token | ~$0.005/brief | ~$0.15/month |
| **Bing Grounding** (S tier) | $14 per 1,000 transactions | ~$1.40-2.80/month | ~$4.20-8.40/month |
| **Storage account** | Minimal | ~$0.01/month | ~$0.01/month |
| **Total** | | **~$2-4/month** | **~$6-12/month** |

> Each brief run triggers ~5-10 Bing search transactions. Heavy comparison testing (all models daily) could reach ~$15-20/month for Bing alone.

### Budget Setup (Portal)

1. Open `<your-resource-group>` in Azure Portal
2. Left menu → **Cost Management** → **Budgets** → **+ Add**
3. Name: `<your-budget-name>`, Amount: `40`, Reset: `Monthly`
4. Add alert at 50% and 80%, enter your email

## Microsoft Foundry (Azure AI Foundry)

> In the Azure Portal, search for **"Microsoft Foundry"** (rebranded from Azure AI Foundry).

### Resource Setup (Portal)

1. Search **"Microsoft Foundry"** in Azure Portal
2. Click **Create a resource**
3. Fill in:
   - **Resource group**: `<your-resource-group>`
   - **Name**: `<your-ai-resource>`
   - **Region**: `Sweden Central`
4. Leave Storage, Network, Identity, Encryption as defaults
5. **Review + create** → **Create**
6. Click **Go to resource** → **Go to Foundry portal**

### Project

A default project is created automatically: `<your-project>`

### Model Deployments

In the Foundry portal → **Model catalog** → search and deploy:

| Model | Deployment Name | Deployment Type | Status |
| --- | --- | --- | --- |
| gpt-4.1 | gpt-4.1 | Global Standard | Deployed |
| gpt-5.4-mini | gpt-5.4-mini | Global Standard | Deployed (used by KanelBrief agents) |
| gpt-5.4 | gpt-5.4 | Global Standard | TODO |
| gpt-5.4-nano | gpt-5.4-nano | Global Standard | TODO |
| DeepSeek | deepseek | Serverless | TODO |

> **Global Standard** = pay per token, no idle cost. Safe to leave deployed.

### Bing Grounding

The News Brief Agent uses Bing Grounding to search for real-time news. LLMs have a knowledge cutoff — without Bing, the model cannot access current events.

**Step 1 — Create Bing Search resource (Azure Portal):**

1. Search **"Grounding with Bing Search"** in Azure Portal
2. Click **Create**
3. Fill in:
   - **Resource group**: `<your-resource-group>`
   - **Name**: `<your-bing-resource>`
   - **Pricing tier**: S ($14 per 1,000 transactions)
4. Check the terms checkbox → **Review + create** → **Create**

**Step 2 — Connect to Foundry (Foundry Portal):**

1. Go to the Foundry portal → **Management center** (bottom-left)
2. Go to **Connected resources** → **+ New connection**
3. Select **Grounding with Bing Search**
4. Pick the Bing resource you created
5. Note the **connection name**

**Step 3 — Save connection name:**

**FikaForecast** — save as user secret:

```bash
cd FikaForecast/FikaForecast.Wpf
dotnet user-secrets set "AzureAIFoundry:BingConnectionName" "<connection-name>"
```

**KanelBrief** — set as Function App environment variable:

Azure Portal → Function App → **Environment variables** → add:

- **Name:** `BING_CONNECTION_NAME`
- **Value:** `<connection-name>`

> Without this setting, the News Brief agent still works but uses LLM training data only (no real-time news search).

### Authentication and Endpoints

Both FikaForecast and KanelBrief use **DefaultAzureCredential** — no API keys needed.

- **Locally**: Azure CLI (`az login`)
- **Azure Functions**: System-assigned Managed Identity

```bash
# Install Azure CLI (one-time)
winget install Microsoft.AzureCLI

# Log in with your Azure account (same account as Azure Portal)
az login
```

| Value | Where to find it |
| --- | --- |
| Project endpoint | Foundry portal → project overview → "Microsoft Foundry project endpoint": `https://<your-ai-resource>.services.ai.azure.com/api/projects/<your-project>` |
| Bing connection | Management center → Connected resources → connection name |

**FikaForecast** — save as user secrets:

```bash
cd FikaForecast/FikaForecast.Wpf
dotnet user-secrets set "AzureAIFoundry:ProjectEndpoint" "https://<your-ai-resource>.services.ai.azure.com/api/projects/<your-project>"
dotnet user-secrets set "AzureAIFoundry:BingConnectionName" "<your-bing-connection>"
```

**KanelBrief** — set as Function App environment variables:

Azure Portal → Function App → **Environment variables** → add:

- `FOUNDRY_PROJECT_ENDPOINT` = `https://<your-ai-resource>.services.ai.azure.com/api/projects/<your-project>`
- `BING_CONNECTION_NAME` = `<your-bing-connection>` (optional — enables real-time news search)

### Foundry — Security (Managed Identity + RBAC)

The Function App authenticates to AI Foundry via Managed Identity.
The Agent Framework needs permission to create and run agents.

**Required roles on the Foundry resource (`<your-ai-resource>`):**

| Role | Why |
| --- | --- |
| Azure AI Developer | General access to AI Foundry project |
| Cognitive Services User | Required for agent create/run operations |

**Setup steps:**

1. Azure Portal → Foundry resource (`<your-ai-resource>`) →
   **Access Control (IAM)** → **+ Add** → **Add role assignment**
2. Search **"Azure AI Developer"** → select → **Next** →
   Assign access to: **Managed identity** → **+ Select members** →
   pick your Function App → **Review + assign**
3. Repeat for **"Cognitive Services User"**

### Persistent Agent Setup (KanelBrief News Brief)

The News Brief agent is created **once** in the Foundry portal as a persistent, named agent.
The backend invokes it by reference on every 4-hour timer tick instead of creating and
deleting an ephemeral agent per run.

**Why persistent?** Foundry continuous evaluation (Groundedness, Custom Evaluator) targets
an agent by name and scores every run automatically. An ephemeral agent that's deleted
after each run has no target for evaluators to attach to and never surfaces in the portal UI.

**Why groundedness matters here?** The agent uses Bing Grounding to cite news from the past
48 hours. Groundedness measures whether the report's claims are actually supported by those
Bing citations — catching hallucinations and drift from real-world data. Without Bing
Grounding (and without the `source` field in the agent output), groundedness scoring has
nothing to verify against.

**Why 48 hours?** At a 4-hour run cadence a 14-day window would be mostly redundant between
consecutive runs. 48 hours is wide enough to absorb Bing indexing lag (paywalled article
previews and smaller outlets can take up to ~24 hours to be searchable) yet narrow enough
that each run reflects new developments rather than rehashing the same fortnight.

**Step 1 — Create the agent (Foundry Portal):**

1. Foundry portal → **Build** → **Agents** → **+ New agent**.
2. Fill in:
   - **Name**: `kanelbrief-news-brief`
   - **Model**: `gpt-5.4-mini` (Global Standard)
   - **Tools**: add **Grounding with Bing Search** → pick `<your-bing-connection>`
   - **Instructions**: paste the prompt in Step 1a below
3. Save.

**Step 1a — News Brief agent instructions (source of truth, paste verbatim):**

```text
You are a financial market analyst. Analyze global financial market conditions from the past 48 hours and produce a morning market brief.

Cover these sectors: Technology, Energy, Financials, Healthcare, Consumer Discretionary, Industrials.
Focus on the most significant market-moving events and trends from the past 48 hours (indexing lag headroom: some stories may still surface that broke up to ~2 days ago).

1. Determine the overall market mood (RiskOn, RiskOff, or Mixed)
2. Write a brief 1-2 sentence market summary
3. For each significant sector (at least 3-4), provide a sentiment assessment

Sourcing rules:
- Every assessment headline must reference a specific event, data point, or named source from Bing Grounding results (e.g., "Fed held rates at 5.25%", "IEA cut 2026 demand forecast", "NVIDIA reported Q1 earnings of $X").
- Cite the source name and URL in the `source` field. Paywalled articles are fine to cite — use the publication and URL even if only the headline preview was accessible.
- Do NOT invent sources. If Bing returned no relevant results for a sector, label sentiment `Mixed` and set `source` to `null`.
- Avoid hedging language ("appears", "likely", "may", "could", "tends to") — state facts with verifiable sources or omit the claim.

Output formatting rules:
- Do NOT include inline citation markers like 【6:2†source】 or [1] anywhere in `summary`, `headline`, or assessment `summary` fields. The `source` field is the ONLY citation mechanism.
- The `source` field must be an article-level URL from a Bing Grounding result — NOT a homepage, section, or aggregator URL:
  - ✅ "Reuters — https://www.reuters.com/markets/us/intel-earnings-2026-04-24/"
  - ❌ "Reuters — https://www.reuters.com/" (homepage)
  - ❌ "FT — https://www.ft.com/markets" (section)
  - ❌ "Google Finance — https://www.google.com/finance/" (aggregator)
- Pick the source whose content most directly supports the headline's specific claim.

Return ONLY a JSON object with this exact structure:
{
  "mood": "RiskOn|RiskOff|Mixed",
  "summary": "Your market summary with specific data points where available",
  "assessments": [
    {
      "category": "Sector name",
      "headline": "Specific market-moving event or data point",
      "summary": "Brief analysis grounded in the cited source",
      "sentiment": "RiskOn|RiskOff|Mixed",
      "source": "Publication name — https://article.url.example (or null when sentiment is Mixed)"
    }
  ]
}
```

> Because this prompt lives in the portal rather than source code, this doc is the recovery copy. If the agent is accidentally deleted, recreate it by pasting the block above verbatim.

**Step 2 — Enable Groundedness evaluator (built-in):**

1. Foundry portal → **Build → Agents → `kanelbrief-news-brief`** → **Monitor** tab.
2. **Set up continuous evaluation** → enable **Groundedness**. Judge model: `gpt-5.4-mini`. Accept default sampling.
3. Save.

**Step 3 — Enable Custom Evaluator (content quality):**

Groundedness covers "are claims supported by sources?". To also score domain-specific content rules (brevity, source authenticity, category coverage, sentiment-label accuracy), add a custom evaluator:

1. Same Monitor tab → **Custom Evaluator** → **Create**.
2. Paste the contents of [../FikaForecast/FikaForecast.Application/Prompts/evaluation.prompt.txt](../FikaForecast/FikaForecast.Application/Prompts/evaluation.prompt.txt).
3. Save.

Both evaluators run automatically on every future News Brief run — scores appear in the Monitor tab within ~5–10 min of each run.

## Storage Account

Azure Tables for agent results + Durable Functions state. Single account handles both.

### Storage — Create (Portal)

1. Search **"Storage accounts"** → **+ Create**
2. Fill in:
   - **Resource group**: `<your-resource-group>`
   - **Name**: `<your-storage-account>` (lowercase, no dashes)
   - **Region**: `Sweden Central`
   - **Preferred storage type**: Other (Tables and Queues)
   - **Performance**: Standard
   - **Redundancy**: LRS
3. Leave Advanced, Networking, Data Protection, Encryption as defaults
4. **Review + create** → **Create**

### Storage — Create (CLI)

```bash
az storage account create \
  --name <your-storage-account> \
  --resource-group <your-resource-group> \
  --location swedencentral \
  --sku Standard_LRS \
  --kind StorageV2
```

### Storage — Security (Managed Identity + RBAC)

Azure Tables data access uses **Managed Identity** — no connection strings or keys for table operations. Even if the storage account URL leaks, unauthorized requests get `403 Forbidden`.

**How it works:**

- The Function App has a **system-assigned managed identity** enabled
- The identity is granted **"Storage Table Data Contributor"** RBAC role on the storage account
- The app uses `DefaultAzureCredential` + `TableStorageUri` instead of a connection string
- Locally, `UseDevelopmentStorage=true` connects to Azurite (local emulator)

**Setup steps:**

1. **Enable Managed Identity:**
   Azure Portal → Function App (`<your-function-app>`) → **Settings** → **Identity** → System assigned → **On** → **Save**

2. **Assign RBAC role:**
   Azure Portal → Storage Account (`<your-storage-account>`) → **Access Control (IAM)** →
   **+ Add** → **Add role assignment** → search **"Storage Table Data Contributor"** →
   select it → **Next** → Assign access to: **Managed identity** →
   **+ Select members** → pick your Function App → **Review + assign**

3. **Add `TableStorageUri` app setting:**
   Azure Portal → Function App → **Settings** → **Environment variables** → add:
   - **Name:** `TableStorageUri`
   - **Value:** `https://<your-storage-account>.table.core.windows.net`

> **Note:** `AzureWebJobsStorage` still uses a connection string for Functions runtime
> internals (timer triggers, Durable Tasks, blob leases). Only table *data* access uses
> Managed Identity. A future improvement is migrating `AzureWebJobsStorage` to
> identity-based connections as well.

### Tables Created by the System

Tables are created automatically on first write via `CreateIfNotExistsAsync()` in the Azure.Data.Tables SDK — no manual setup needed:

| Table | Purpose |
| --- | --- |
| `NewsBriefRuns` | News Brief agent results |
| `WeeklySummaryRuns` | Weekly Summary agent results |
| `SubstitutionChainRuns` | Substitution Chain agent results |
| `OpportunityScanRuns` | Opportunity Scan agent results |
| `LatestRuns` | Dashboard accelerator (latest run per model) |

Durable Functions also auto-creates its own internal tables/queues in the same account on first orchestration run.

### Schema & Migrations

Azure Tables is schemaless — there are no migrations. Each row is a bag of key-value properties.

- **Add a property** — just start writing it. Old rows return `null`, new rows have it.
- **Remove a property** — stop writing it. Old rows keep it, new rows don't.
- **Change a property type** — old rows have the old type, new rows the new one. Read code must handle both.

Agent results are write-once, read-many — old runs are never updated. If the output format changes, new runs simply have different properties. Read code handles missing properties with null checks.

### Storage — Cost

~$0.05/month at this workload volume. Effectively free.

## Function App (KanelBrief)

Azure Functions (Flex Consumption) — runs AI agents, Durable orchestrations, and read API endpoints.

### Functions — Create (Portal)

1. Search **"Function App"** → **+ Create**
2. Fill in:
   - **Resource group**: `<your-resource-group>`
   - **Name**: `<your-function-app>`
   - **Region**: `Sweden Central`
   - **Hosting plan**: Flex Consumption
   - **Runtime stack**: .NET 9 (Isolated)
   - **Instance size**: 2048 MB (default)
   - **Zone redundancy**: Disabled
3. **Storage**: select `<your-storage-account>`
4. **Durable Functions**: Enable — Azure managed (Durable Task Scheduler), Consumption SKU
5. **Monitoring**: Application Insights — enable (free grant: 5 GB/month)
6. **Networking, Deployment, Authentication**: defaults
7. **Review + create** → **Create**

### Functions — Create (CLI)

```bash
az functionapp create \
  --name <your-function-app> \
  --resource-group <your-resource-group> \
  --storage-account <your-storage-account> \
  --runtime dotnet-isolated \
  --runtime-version 9.0 \
  --functions-version 4 \
  --os-type Linux \
  --location swedencentral
```

### Functions — Cost

Flex Consumption: effectively $0 within free grant (250,000 GB-s + 1,000,000 executions/month). KanelBrief uses ~10% of the free grant.

### Sync Endpoints — Bearer Token

The `/api/sync/*` endpoints (News Brief, Weekly Summary, Substitution Chain,
Opportunity Scan range queries) are protected by a symmetric bearer token.
The FikaForecast WPF desktop app uses these to pull runs into its local SQLite DB.

**Setup:**

1. Generate a 32-byte hex token on your local machine:

   ```bash
   # bash / WSL
   openssl rand -hex 32
   ```

   ```powershell
   # PowerShell equivalent
   -join ((1..64) | ForEach-Object { '{0:x}' -f (Get-Random -Max 16) })
   ```

   Store the value in a password manager — you'll paste it into both Azure and the WPF app.

2. Azure Portal → Function App (`<your-function-app>`) → **Settings** → **Environment variables** → **+ Add**:
   - **Name:** `SYNC_AUTH_TOKEN`
   - **Value:** `<your-sync-auth-token>`
   - **Apply** → **Confirm** (Function App restarts automatically).

3. In the WPF app: **Settings → Sync** → paste the same token into the "Auth token" field, and set **Base URL** to `https://<your-function-app>.azurewebsites.net`.

**Security notes:**

- The token is stored as a plain App Setting. Values are hidden from users with `Reader` role but visible with `Contributor`+.
- Rotate by replacing the App Setting value and updating the WPF-side token; rollover is manual (single shared secret).
- If `SYNC_AUTH_TOKEN` is missing or empty at runtime, sync endpoints return **503** — they **fail closed**, never falling through to anonymous access.
- The middleware never logs the raw header or token; unauthorized attempts log only the function name.
- No GitHub Secret is needed — the token is runtime configuration, not used at build/deploy time.

**CORS:** Leave CORS restricted to your frontend origin
(e.g. `http://localhost:3000` locally, your Static Web App domain in prod).
The WPF app calls the API directly (no browser), so it bypasses CORS.

## Static Web App (SmorgasBoard)

Next.js frontend dashboard — displays agent pipeline results from KanelBrief.

### SWA — Create (Portal)

1. Search **"Static Web Apps"** → **+ Create**
2. Fill in:
   - **Resource group**: `<your-resource-group>`
   - **Name**: `<your-static-web-app>`
   - **Plan type**: Free
   - **Region**: `West Europe` (SWA regions differ from regular Azure — pick closest available)
   - **Deployment source**: Other (connect repo later)
   - **Deployment authorization**: Deployment token
3. **Enterprise-grade edge**: Disabled
4. **Review + create** → **Create**

### SWA — Create (CLI)

```bash
az staticwebapp create \
  --name <your-static-web-app> \
  --resource-group <your-resource-group> \
  --location westeurope \
  --sku Free
```

### SWA — CORS

The Function App must allow cross-origin requests from the SWA domain.

Azure Portal → Function App → **API → CORS** → add:

- `https://<your-static-web-app>.azurestaticapps.net`

> **Note:** The free SWA tier does not support linked backends.
> The frontend calls the Function App directly via `NEXT_PUBLIC_API_URL`.

### SWA — Cost

Free tier: $0.

## CI/CD (GitHub Actions)

Automated build, test, and deployment via GitHub Actions.

### Workflows

| Workflow | Trigger | What it does |
| --- | --- | --- |
| `pr-checks.yml` | PR to `main` or `develop` | Build + test backend |
| `deploy-backend.yml` | Push to `main` (PR merge) | Build + test + deploy to Azure Functions |
| `deploy-frontend.yml` | Push to `main` (paths: `frontend/**`) | Build Next.js + deploy to Azure Static Web Apps |

### Branch Protection (main)

Configured via GitHub → Repository → Settings → Rules → Rulesets → `Protect main`:

- **Restrict deletions** — prevent deleting `main`
- **Require a pull request before merging** (required approvals: 0, dismiss stale approvals)
- **Require status checks to pass** — `Backend - Build & Test`
- **Block force pushes**

### GitHub Secrets

See [SECRETS-MANAGEMENT.md](SECRETS-MANAGEMENT.md#github-actions-secrets-cicd) for required secrets.

### Publish Profile Setup

> **Important (Flex Consumption):** SCM Basic Auth must be enabled for publish profiles to work.
> Azure Portal → Function App → **Settings** → **Configuration** → **General settings** → **SCM Basic Auth Publishing Credentials** → **On** → **Apply**.

1. Azure Portal → Function App (`<your-function-app>`) → **Overview**
2. Click **Get publish profile** (downloads an XML file)
3. Copy the entire XML content
4. GitHub → Repository → Settings → Secrets → Actions → **New repository secret**
5. Name: `AZURE_FUNCTION_PUBLISH_PROFILE`, Value: paste the XML

### Flex Consumption Deployment Notes

- Uses **One Deploy** (not Kudu zip deploy)
- Set `sku: flexconsumption` in the GitHub Action
- Set `remote-build: false` for .NET (project is pre-compiled via `dotnet publish`)
- `remote-build: true` is only needed for interpreted languages (Node.js, Python)

### Manual Deploy

You can trigger a deploy manually from GitHub → Actions → **Deploy Backend** → **Run workflow**.

## Existing Services (Shared Backend)

These services are already deployed and shared with [SemanticKernel-FundDocsQnA](https://github.com/Muhomorik/SemanticKernel-FundDocsQnA-dotnet-nextjs):

- **Azure App Service F1** (free tier) -- Backend API
- **Azure Static Web Apps** -- Frontend
- **Azure Key Vault** -- Secrets
- **Application Insights** -- Monitoring

## Additional Resources

- [Microsoft Foundry Documentation](https://learn.microsoft.com/azure/ai-studio/)
- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Azure Key Vault Quickstart](https://docs.microsoft.com/azure/key-vault/general/quick-create-cli)
