namespace WeatherParser.Cli.Output;

/// <summary>
/// Supplies the fixed English text of an <see cref="ExportError"/> per error ID, without exception messages, paths, URLs or values (CL-10, C-86).
/// </summary>
/// <returns>One line of text.</returns>
public interface IExportErrorMessages
{
    /// <summary>
    /// Gets the fixed English text for the specified error ID.
    /// </summary>
    /// <param name="errorId">The error ID.</param>
    /// <returns>One line of text.</returns>
    string For(string errorId);
}
