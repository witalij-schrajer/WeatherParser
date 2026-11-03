using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace WeatherParser.SchemaGeneration;

/// <summary>
/// Applies the deterministic refinements of contracts C-99 to C-102 to an exported schema.
/// </summary>
public interface IExportSchemaRefiner
{
    /// <summary>
    /// Refines the schema in place; throws InvalidOperationException when a mapped property has no exported element.
    /// </summary>
    /// <param name="schema">The schema to refine.</param>
    /// <param name="rootTypes">The root types used to generate the schema.</param>
    void Refine(XDocument schema, IReadOnlyList<Type> rootTypes);
}
