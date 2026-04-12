namespace KanelBrief.Functions.Tests.Agents;

/// <summary>
/// Minimal deterministic <see cref="TimeProvider"/> for Functions.Tests.
/// Mirrors the Core.Tests version so we don't need a shared test utility library.
/// </summary>
internal sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public TestTimeProvider(DateTimeOffset start) => _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);

    public void Set(DateTimeOffset value) => _now = value;
}
