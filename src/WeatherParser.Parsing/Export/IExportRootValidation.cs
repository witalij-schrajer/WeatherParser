using System;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Implemented by export roots whose graph invariants must hold before any serializer writes them (contract C-64).
/// </summary>
internal interface IExportRootValidation
{
    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> with an actionable English message when an invariant is violated.
    /// </summary>
    void ValidateForExport();
}
