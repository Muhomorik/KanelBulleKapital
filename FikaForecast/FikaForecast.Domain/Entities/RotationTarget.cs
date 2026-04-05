using System.Diagnostics;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Domain.Entities;

/// <summary>
/// A single rotation target entry from the Opportunity Scan Agent.
/// Describes a category capturing capital inflows, its signal strength, and risk caveat.
/// </summary>
[DebuggerDisplay("{Category} — {SignalStrength}")]
public class RotationTarget
{
    public Guid TargetId { get; private set; }
    public string Category { get; private set; }
    public SignalStrength SignalStrength { get; private set; }
    public string Rationale { get; private set; }
    public string RiskCaveat { get; private set; }

    /// <summary>FK to the parent <see cref="OpportunityScanRun"/>.</summary>
    public Guid RunId { get; private set; }

    private RotationTarget() // EF Core
    {
        Category = null!;
        Rationale = null!;
        RiskCaveat = null!;
    }

    /// <summary>
    /// Creates a new rotation target entry parsed from agent output.
    /// </summary>
    public RotationTarget(string category, SignalStrength signalStrength, string rationale, string riskCaveat)
    {
        TargetId = Guid.NewGuid();
        Category = category;
        SignalStrength = signalStrength;
        Rationale = rationale;
        RiskCaveat = riskCaveat;
    }
}
