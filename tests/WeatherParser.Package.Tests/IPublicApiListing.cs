using System;
using System.Collections.Generic;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Provides functionality for generating and comparing public API listings.
/// </summary>
internal interface IPublicApiListing
{
    /// <summary>
    /// Gets the path to the checked-in public API listing file.
    /// </summary>
    string ListingPath { get; }

    /// <summary>
    /// Generates a public API listing for the specified assembly.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly to analyze.</param>
    /// <param name="namespacePrefix">If not null, only types whose namespace equals this value or starts with <paramref name="namespacePrefix"/> + "." are included.</param>
    /// <returns>A sorted list of public API entries.</returns>
    IReadOnlyList<string> Generate(string assemblyPath, string? namespacePrefix = null);

    /// <summary>
    /// Compares two public API listings.
    /// </summary>
    /// <param name="expected">The expected listing.</param>
    /// <param name="actual">The actual listing.</param>
    /// <returns>A tuple of added and removed entries.</returns>
    (IReadOnlyList<string> Added, IReadOnlyList<string> Removed) Compare(IReadOnlyList<string> expected, IReadOnlyList<string> actual);

    /// <summary>
    /// Reads the public API listing from a file.
    /// </summary>
    /// <param name="path">The path to the listing file.</param>
    /// <returns>The lines of the file, trimmed and without empty lines.</returns>
    IReadOnlyList<string> ReadListing(string path);
}
