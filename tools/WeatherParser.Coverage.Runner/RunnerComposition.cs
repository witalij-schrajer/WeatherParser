using System.IO;
using WeatherParser.Coverage;

namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Composition root for the runner application, shared by Program.cs and end-to-end tests.
/// </summary>
public static class RunnerComposition
{
    /// <summary>
    /// Creates the runner application with all dependencies wired.
    /// </summary>
    /// <param name="output">The output writer for JSON results.</param>
    /// <param name="error">The error writer for diagnostics.</param>
    /// <returns>The configured runner application instance.</returns>
    public static IRunnerApplication Create(TextWriter output, TextWriter error)
    {
        var fileReader = new FileReader();
        var reportReader = new CoverageReportReader(fileReader);
        var baselineReader = new BaselineMetadataReader(fileReader);
        var changesReader = new ChangedFileListReader(fileReader);
        var aggregator = new ProvenanceAggregator();
        var evaluator = new RuleEvaluator();
        var comparabilityChecker = new BaselineComparabilityChecker();
        var classifier = new PullRequestClassifier();
        var gate = new CoverageGate(reportReader, baselineReader, changesReader, aggregator, evaluator, comparabilityChecker, classifier, new ReconciliationReader(fileReader));
        var fileWriter = new FileWriter();
        var baselineWriter = new BaselineWriter(reportReader, aggregator, fileWriter);
        var resultWriter = new JsonResultWriter(output);

        return new RunnerApplication(
            new CommandLineParser(),
            gate,
            baselineWriter,
            resultWriter,
            fileReader,
            error);
    }
}
