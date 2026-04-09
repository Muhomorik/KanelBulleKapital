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
| gpt-5.4-mini | gpt-5.4-mini | Global Standard | TODO (needs SDK migration to Azure.AI.Projects for Bing Grounding) |
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

```bash
cd FikaForecast/FikaForecast.Wpf
dotnet user-secrets set "AzureAIFoundry:BingConnectionName" "<connection-name>"
```

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

**KanelBrief** — set as Function App environment variable:

Azure Portal → Function App → **Environment variables** → add:

- **Name:** `FOUNDRY_PROJECT_ENDPOINT`
- **Value:** `https://<your-ai-resource>.services.ai.azure.com/api/projects/<your-project>`

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
