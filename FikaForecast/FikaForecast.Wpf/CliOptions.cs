using CommandLine;

namespace FikaForecast.Wpf;

/// <summary>
/// Command-line options parsed by <see cref="CommandLine.Parser"/>.
/// Registered as a singleton in the DI container so any component can check launch flags.
/// </summary>
public class CliOptions
{
    [Option("auto-schedule", Required = false, HelpText = "Start the batch scheduler automatically on launch.")]
    public bool AutoSchedule { get; set; }
}
