namespace KanelBrief.Core.Tests.Pipelines;

/// <summary>
/// Minimal deterministic <see cref="TimeProvider"/> for tests. Advances only when <see cref="Advance"/> is called.
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset start)
    {
        _now = start;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);

    public void Set(DateTimeOffset value) => _now = value;
}
