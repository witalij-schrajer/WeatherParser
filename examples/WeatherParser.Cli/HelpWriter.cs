namespace WeatherParser.Cli;

/// <summary>
/// Writes the usage line and the five commands of the example CLI (REQ-010; the options, units and examples are completed in P3.04).
/// </summary>
public sealed class HelpWriter : IHelpWriter
{
    /// <summary>
    /// Writes the complete help text to the writer.
    /// </summary>
    /// <param name="writer">The destination.</param>
    public void WriteHelp(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        
        writer.WriteLine("Usage: WeatherParser --<command> [options]");
        writer.WriteLine();
        writer.WriteLine("Commands (exactly one per invocation):");
        writer.WriteLine("  --SelectDWD        Find suitable DWD stations.");
        writer.WriteLine("  --ParseDWD         Retrieve observations of one DWD station.");
        writer.WriteLine("  --ParseOstHessen   Retrieve observations of matching OsthessenWetter stations.");
        writer.WriteLine("  --SelectMOSMIX     Find suitable MOSMIX forecast locations.");
        writer.WriteLine("  --ParseMOSMIX      Retrieve the forecast of one MOSMIX location.");
        writer.WriteLine();
        writer.WriteLine("Use --Help to show this text.");
    }
}
