namespace KanelBrief.Core.Models;

/// <summary>
/// Envelope for <c>/api/sync/*</c> date-range responses. Serialized with
/// <c>KanelJsonOptions.CamelCase</c> so the wire shape is <c>{ from, to, count, runs }</c>.
/// </summary>
public sealed class SyncRunsResponse<T>
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public int Count { get; set; }
    public List<T> Runs { get; set; } = new();
}
