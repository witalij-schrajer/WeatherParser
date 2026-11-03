using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace WeatherParser.Coverage;

/// <summary>
/// Reads and parses native Microsoft.CodeCoverage XML reports.
/// </summary>
public sealed class CoverageReportReader : ICoverageReportReader
{
    private readonly IFileReader _fileReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageReportReader"/> class.
    /// </summary>
    /// <param name="fileReader">The file reader to use for reading files.</param>
    public CoverageReportReader(IFileReader fileReader)
    {
        _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
    }

    /// <inheritdoc/>
    public async Task<CoverageReport> ReadAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new CoverageInputException($"Coverage report '{path}' was not found.");
        }

        if (!_fileReader.Exists(path))
        {
            throw new CoverageInputException($"Coverage report '{path}' was not found.");
        }

        var xml = await _fileReader.ReadAllTextAsync(path, cancellationToken);
        return ReadFromXml(xml, path);
    }

    private CoverageReport ReadFromXml(string xml, string path)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        try
        {
            var doc = XDocument.Load(XmlReader.Create(new System.IO.StringReader(xml), settings));
            var root = doc.Root;

            if (root == null)
            {
                throw new CoverageInputException($"Coverage report '{path}' is not well-formed XML.");
            }

            if (root.Name.LocalName == "coverage")
            {
                throw new CoverageInputException($"Coverage report '{path}' is a Cobertura report. Cobertura reports are not accepted; native Microsoft.CodeCoverage block counters (blocks_covered/blocks_not_covered) are required.");
            }

            if (root.Name.LocalName != "results")
            {
                throw new CoverageInputException($"Coverage report '{path}' root element is '{root.Name.LocalName}', expected 'results'.");
            }

            var modules = new List<ModuleCoverage>();
            var modulesElement = root.Element("modules");
            if (modulesElement == null)
            {
                throw new CoverageInputException($"Coverage report '{path}' contains no modules.");
            }

            foreach (var moduleElement in modulesElement.Elements("module"))
            {
                var name = moduleElement.Attribute("name")?.Value ?? string.Empty;
                var id = moduleElement.Attribute("id")?.Value ?? string.Empty;
                var pathAttr = moduleElement.Attribute("path")?.Value ?? string.Empty;

                var blocksCovered = moduleElement.Attribute("blocks_covered")?.Value;
                var blocksNotCovered = moduleElement.Attribute("blocks_not_covered")?.Value;

                if (string.IsNullOrEmpty(blocksCovered) || string.IsNullOrEmpty(blocksNotCovered))
                {
                    throw new CoverageInputException($"Module '{name}' is missing required block counters.");
                }

                if (!long.TryParse(blocksCovered, NumberStyles.None, CultureInfo.InvariantCulture, out var covered) || !long.TryParse(blocksNotCovered, NumberStyles.None, CultureInfo.InvariantCulture, out var notCovered))
                {
                    throw new CoverageInputException($"Module '{name}' has invalid block counter values.");
                }

                if (covered < 0 || notCovered < 0)
                {
                    throw new CoverageInputException($"Module '{name}' has negative block counters.");
                }

                if (covered > long.MaxValue - notCovered)
                {
                    throw new CoverageInputException($"Module '{name}' has invalid block counter values: their total exceeds the supported range.");
                }

                modules.Add(new ModuleCoverage(name, id, pathAttr, new BlockCounters(covered, notCovered)));
            }

            if (modules.Count == 0)
            {
                throw new CoverageInputException($"Coverage report '{path}' contains no modules.");
            }

            return new CoverageReport(modules, "native-xml");
        }
        catch (XmlException ex)
        {
            throw new CoverageInputException($"Coverage report '{path}' is not well-formed XML: {ex.Message}", ex);
        }
    }
}
