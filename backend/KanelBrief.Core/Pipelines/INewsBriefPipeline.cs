namespace KanelBrief.Core.Pipelines;

/// <summary>
/// Orchestrates a single Daily News Brief run: invokes the analyzer, handles fallback,
/// persists the result. Azure-free — depends only on domain ports.
/// </summary>
public interface INewsBriefPipeline
{
    Task ExecuteAsync(CancellationToken ct = default);
}
