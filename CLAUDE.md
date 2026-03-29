# CLAUDE.md

## 🎭 Communication Style

This is a personal hobby project. Be warm, friendly, and human — like a coding buddy, not a corporate assistant. Humor is welcome and encouraged. Use casual language, share enthusiasm about cool solutions, crack a joke when the moment calls for it, and celebrate wins together. Skip the robotic "I'll help you with that" phrasing — just be yourself and have fun building things.

## Project Goals

This project is a **skills portfolio** — the primary goal is to demonstrate proficiency in:

- **Microsoft Agent Framework** (Azure AI Agent Service)
- **Azure AI Foundry**

### ⚠️ Microsoft Agent Framework — Knowledge Freshness Warning

The Microsoft Agent Framework and Azure AI Foundry are newly released and evolving rapidly. Claude's training data may be outdated for these topics. **Before answering questions or generating code related to the Agent Framework or AI Foundry, always check the latest Microsoft Learn documentation via MCP docs tools.** Do not rely solely on training knowledge — verify against current docs first.

## Key Technologies

### Desktop (existing)

| Technology | Version | Purpose |
| ------------ | --------- | --------- |
| .NET | 9.0 | Framework |
| WPF | - | Desktop UI |
| Entity Framework Core | 9.0 | SQLite persistence |
| DevExpressMvvm | 24.1.6 | MVVM framework |
| Autofac | 9.0.0 | Dependency injection |
| Rx.NET | 6.1.0 | Reactive programming |
| MahApps.Metro | 2.4.11 | Modern UI toolkit |
| WebView2 | 1.0.2903 | Embedded Chromium browser |
| Magick.NET | 14.10.2 | Privacy filter image processing |
| NLog | 6.0.7 | Logging |

### Frontend

| Technology | Purpose |
| ------------ | --------- |
| Next.js | Web frontend |

### Cloud / AI

| Technology | Purpose |
| ------------ | --------- |
| Azure Functions | Serverless compute (individual task handlers) |
| Microsoft Agent Framework | AI agent orchestration |
| Azure AI Foundry | AI model hosting & management |

### Additional Guidance

- Keep costs low: When suggesting infrastructure, prioritize free/low-cost options (Azure free tier, free APIs). Only suggest paid upgrades if strictly necessary and mention the cost impact.
- **Azure App Service F1 (free tier) is already in use** for the backend — do not suggest removing or replacing it. Use Azure Functions for new serverless workloads instead.
- **Do not use or reference Semantic Kernel** — it is deprecated. Use Microsoft Agent Framework instead.

## Project Overview

**Project Hosting:**

- **Repository:** GitHub (personal, public)
- **Deployment:** Azure (private infrastructure)
- **Services:** Azure App Service F1 (backend), Azure Static Web Apps (frontend), Azure Functions (serverless), Azure AI Foundry, Application Insights, Key Vault

### Shared Backend

This project shares a backend with [SemanticKernel-FundDocsQnA-dotnet-nextjs](https://github.com/Muhomorik/SemanticKernel-FundDocsQnA-dotnet-nextjs).

**Backend stack:** ASP.NET Core 9 Web API with a hybrid Q&A system:

- **RAG Pipeline** — Semantic search over PDF fund documents (OpenAI embeddings)
- **Function Calling** — Structured queries against Azure SQL via AI plugins
- **LLM:** OpenAI gpt-4.1-mini (default) or Groq llama-3.3-70b-versatile (free alternative)
- **Embeddings:** OpenAI text-embedding-3-small
- **Vector Storage:** InMemory (default) or Azure Cosmos DB (persistent)
- **Database:** Azure SQL (optional — enables function calling; without it, RAG-only mode)
- **Monitoring:** Application Insights
- **Secrets:** Azure Key Vault (production), `dotnet user-secrets` (local)

**Key endpoints:** `POST /api/ask` (main Q&A), `POST /api/ask/stream` (streaming), fund management & health endpoints. Swagger UI at `/swagger`.

**Architecture:** Domain-Driven Design with bounded contexts. The LLM autonomously routes between RAG and function calling (or combines them for hybrid answers).

## Documentation

## Build & Run Commands

## Architecture

- **Domain-Driven Design** with bounded contexts

### Data Flow

### Key Services

### Configuration

## Important Notes

## Testing Guidelines

Complete testing guidelines have been moved to the `dotnet-unit-testing-nunit` skill.

**Quick Reference:**

- Use NUnit + AutoFixture + AutoMoq for all .NET tests
- Always resolve SUT from AutoFixture (never `new`)
- Follow AAA pattern (Arrange, Act, Assert)
- Test naming: `MethodName_Scenario_ExpectedBehavior`
- Mock all external dependencies

For detailed patterns, examples, and advanced techniques, see `.claude/skills/dotnet-unit-testing-nunit/SKILL.md`
