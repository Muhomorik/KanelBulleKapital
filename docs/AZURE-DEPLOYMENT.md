# Azure Deployment Guide

Complete guide for deploying the full-stack application (Backend API + Frontend) to Azure with zero-cost hosting.

## Resource Group and Cost Management

All FikaForecast resources live in a dedicated resource group for cost isolation and tracking.

### Budget Alert

- **Threshold:** $40/month
- **Alert recipients:** Resource group owner
- **Action:** Email notification at 80% and 100% of budget

### Setup

```bash
# Create resource group
az group create --name rg-fikaforecast --location swedencentral

# Create budget with alert
az consumption budget create \
  --budget-name fikaforecast-monthly \
  --resource-group rg-fikaforecast \
  --amount 40 \
  --time-grain Monthly \
  --category Cost \
  --start-date 2026-04-01 \
  --end-date 2027-04-01

# Add alert at 80%
az consumption budget create \
  --budget-name fikaforecast-monthly \
  --resource-group rg-fikaforecast \
  --amount 40 \
  --time-grain Monthly \
  --category Cost \
  --notifications "{\"80_percent\":{\"enabled\":true,\"operator\":\"GreaterThanOrEqualTo\",\"threshold\":80,\"contactEmails\":[\"your-email@example.com\"]}}"
```

### Cost Tagging

Tag all resources for cost attribution:

```bash
az tag create --resource-id <resource-id> --tags project=FikaForecast environment=production
```

## Azure AI Foundry

### Project Setup

1. Go to [Azure AI Foundry](https://ai.azure.com)
2. Create a new project in the `rg-fikaforecast` resource group
3. Select **Sweden Central** region (closest, supports all needed models)

### Model Deployments

Deploy these models from the Foundry catalog:

| Model | Deployment Name | Purpose | Estimated Cost per Brief |
| --- | --- | --- | --- |
| GPT-5.1-mini | gpt-51-mini | Fast, cheap baseline | ~$0.01 |
| GPT-5 | gpt-5 | Flagship quality benchmark | ~$0.05-0.10 |
| Phi-4 | phi-4 | Budget option, Microsoft showcase | ~$0.001 |
| DeepSeek | deepseek | Open-source heavyweight | ~$0.01 |

> Cost estimates are approximate and depend on news volume and search depth per run. Track actual costs via the per-run token tracking in the app + Azure Cost Analysis.

### Deploy a Model

```bash
# Example: deploy GPT-5.1-mini
az ai model deployment create \
  --resource-group rg-fikaforecast \
  --workspace-name fikaforecast-ai \
  --name gpt-51-mini \
  --model-id azureai://openai/gpt-5.1-mini \
  --sku-name Standard
```

### Bing Grounding Connection

The News Brief Agent uses Bing Grounding to search for real-time news (LLMs have a knowledge cutoff and cannot access current events without a search tool).

1. In Azure AI Foundry portal, go to **Connected resources**
2. Add a **Bing Search** connection
3. Create a Bing Search resource in the `rg-fikaforecast` resource group if needed
4. Note the connection name for use in agent configuration

## Application Insights

For monitoring agent runs, token usage, and latency:

```bash
az monitor app-insights component create \
  --app fikaforecast-insights \
  --location swedencentral \
  --resource-group rg-fikaforecast \
  --kind web
```

## Existing Services (Shared Backend)

These services are already deployed and shared with [SemanticKernel-FundDocsQnA](https://github.com/Muhomorik/SemanticKernel-FundDocsQnA-dotnet-nextjs):

- **Azure App Service F1** (free tier) -- Backend API
- **Azure Static Web Apps** -- Frontend
- **Azure Key Vault** -- Secrets
- **Application Insights** -- Monitoring

## Additional Resources

- [Azure AI Foundry Documentation](https://learn.microsoft.com/azure/ai-studio/)
- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure Static Web Apps Documentation](https://docs.microsoft.com/azure/static-web-apps/)
- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Azure Key Vault Quickstart](https://docs.microsoft.com/azure/key-vault/general/quick-create-cli)
- [GitHub Actions for Azure](https://docs.microsoft.com/azure/developer/github/github-actions)
