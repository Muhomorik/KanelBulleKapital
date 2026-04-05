using System.Diagnostics;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// A single capital rotation chain entry from the Substitution Chain Agent.
/// Describes where capital is fleeing, where it flows, and the causal mechanism.
/// </summary>
[DebuggerDisplay("{CapitalFleeing} → {FlowsToward}")]
public class RotationChain
{
    public Guid ChainId { get; private set; }
    public string CapitalFleeing { get; private set; }
    public string FlowsToward { get; private set; }
    public string Mechanism { get; private set; }

    /// <summary>FK to the parent <see cref="SubstitutionChainRun"/>.</summary>
    public Guid RunId { get; private set; }

    private RotationChain() // EF Core
    {
        CapitalFleeing = null!;
        FlowsToward = null!;
        Mechanism = null!;
    }

    /// <summary>
    /// Creates a new rotation chain entry parsed from agent output.
    /// </summary>
    public RotationChain(string capitalFleeing, string flowsToward, string mechanism)
    {
        ChainId = Guid.NewGuid();
        CapitalFleeing = capitalFleeing;
        FlowsToward = flowsToward;
        Mechanism = mechanism;
    }
}
