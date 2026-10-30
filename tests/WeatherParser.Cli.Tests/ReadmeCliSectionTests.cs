using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Tests.Configuration;

namespace WeatherParser.Cli.Tests;

[TestClass]
[TestCategory("T-P3-04")]
[TestCategory("T-P3-09")]
public class ReadmeCliSectionTests
{
    /// <summary>
    /// Tests that the exit code table in the README matches the ExitCodes constants.
    /// </summary>
    [TestMethod]
    public void Readme_ExitCodeTable_MatchesCliExitCodes()
    {
        var section = CliSection("### Exit codes");
        var rows = TableRows(section, "| Exit code | Meaning |");
        var exitCodes = typeof(ExitCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (int)f.GetRawConstantValue()!)
            .ToHashSet();

        var tableExitCodes = rows.Select(row => int.Parse(row[0].Trim('`'))).ToHashSet();
        tableExitCodes.Should().Equal(exitCodes);
    }

    /// <summary>
    /// Tests that the error IDs in the README match the CliErrorIds.
    /// </summary>
    [TestMethod]
    public void Readme_ErrorIds_MatchCliErrorIds()
    {
        var section = CliSection("### Error IDs");
        var rows = TableRows(section, "| Error ID | Level | Meaning |");
        var errorIds = rows.Select(row => row[0].Trim('`')).ToHashSet();
        errorIds.Should().Equal(CliErrorIds.All);
    }

    /// <summary>
    /// Tests that the options in the README match the CLI grammar.
    /// </summary>
    [TestMethod]
    public void Readme_Options_MatchGrammarTable()
    {
        var section = CliSection("### Commands and options");
        var rows = TableRows(section, "| Command | Required options | Optional options |");
        rows.Count.Should().Be(5);

        var globalOptions = GlobalOptionNames.ToHashSet();
        foreach (var row in rows)
        {
            var commandName = OptionNames(row[0]).Single();
            var command = ParseCommand(commandName);
            var helpText = HelpText(command);
            var allOptions = OptionNames(helpText).Except(globalOptions).Except(new[] { commandName }).ToHashSet();

            var requiredOptions = string.IsNullOrEmpty(row[1]) || row[1] == "–" ? new HashSet<string>() : OptionNames(row[1]).ToHashSet();
            var optionalOptions = string.IsNullOrEmpty(row[2]) || row[2] == "–" ? new HashSet<string>() : OptionNames(row[2]).ToHashSet();

            var documentedOptions = requiredOptions.Union(optionalOptions);
            documentedOptions.Should().Equal(allOptions);
        }

        // Check global options
        section = CliSection("### Global options");
        var globalRows = TableRows(section, "| Option | Meaning |");
        var globalOptionNames = globalRows.Select(row => OptionNames(row[0]).First()).ToHashSet();
        globalOptionNames.Should().Equal(globalOptions);

        // Verify all global options appear in help
        var fullHelp = HelpText(null);
        foreach (var option in globalOptions)
        {
            OptionNames(fullHelp).Should().Contain(option);
        }
    }

    /// <summary>
    /// Tests that the example configuration file is referenced and valid.
    /// </summary>
    [TestMethod]
    public void Readme_ExampleConfiguration_Referenced_AndValid()
    {
        var section = CliSection("### Configuration");
        section.Should().Contain("`examples/weatherparser.example.json`");

        var configPath = Path.Combine(RepositoryRoot(), "examples", "weatherparser.example.json");
        var configuration = new ConfigurationLoader(new EnvironmentReader()).Load(configPath);
        foreach (var command in Enum.GetValues<CliCommand>())
        {
            using var provider = TestConfigurations.Build(configuration, TestConfigurations.Request(command, OutputFormat.Json));
        }
    }

    /// <summary>
    /// Tests that the README does not contain private IP addresses or paths.
    /// </summary>
    [TestMethod]
    public void Readme_NoPrivatePathsOrAddresses()
    {
        var section = Section("## Example CLI");
        var privateIpRegexes = new[]
        {
            @"\b(10|127)\.\d{1,3}\.\d{1,3}\.\d{1,3}\b",
            @"\b192\.168\.\d{1,3}\.\d{1,3}\b",
            @"\b172\.(1[6-9]|2\d|3[01])\.\d{1,3}\.\d{1,3}\b"
        };

        foreach (var regex in privateIpRegexes)
        {
            Regex.Matches(section, regex).Should().BeEmpty("no private IP addresses should be present");
        }

        var privatePathRegexes = new[]
        {
            @"[A-Za-z]:\\Users\\",
            @"/home/",
            @"/Users/"
        };

        foreach (var regex in privatePathRegexes)
        {
            Regex.Matches(section, regex).Should().BeEmpty("no private paths should be present");
        }

        var urls = Regex.Matches(section, @"https?://([A-Za-z0-9.-]+)")
            .Select(match => match.Groups[1].Value)
            .ToList();

        var allowedHosts = new HashSet<string> { "example.invalid", "opendata.dwd.de", "www.dwd.de", "osthessen-wetter.de" };
        foreach (var host in urls)
        {
            allowedHosts.Should().Contain(host, $"URL host '{host}' is not allowed");
        }
    }

    // The repository root is the directory that holds WeatherParser.slnx, searched upwards from the test output.
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "WeatherParser.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("The repository root was not found.");
    }

    // The text of a README section from its heading to the next heading of the same or a higher level.
    private static string Section(string heading)
    {
        var lines = File.ReadAllText(Path.Combine(RepositoryRoot(), "README.md")).Replace("\r\n", "\n").Split('\n');
        var level = heading.TakeWhile(c => c == '#').Count();
        var start = Array.FindIndex(lines, line => line.TrimEnd() == heading);
        start.Should().BeGreaterThanOrEqualTo(0, $"README.md must contain the heading '{heading}'");
        var end = start + 1;
        while (end < lines.Length && !(lines[end].StartsWith('#') && lines[end].TakeWhile(c => c == '#').Count() <= level))
        {
            end++;
        }

        return string.Join('\n', lines.Skip(start + 1).Take(end - start - 1));
    }

    private static string CliSection(string subheading)
    {
        var section = Section("## Example CLI");
        var lines = section.Split('\n');
        var start = Array.FindIndex(lines, line => line.TrimEnd() == subheading);
        start.Should().BeGreaterThanOrEqualTo(0, $"the section 'Example CLI' must contain the heading '{subheading}'");
        var end = start + 1;
        while (end < lines.Length && !lines[end].StartsWith("### ", StringComparison.Ordinal))
        {
            end++;
        }

        return string.Join('\n', lines.Skip(start + 1).Take(end - start -1));
    }

    // The data rows of the first table under the header: lists of trimmed cells.
    private static List<string[]> TableRows(string text, string header)
    {
        var lines = text.Split('\n').Select(line => line.Trim()).ToList();
        var start = lines.IndexOf(header);
        start.Should().BeGreaterThanOrEqualTo(0, $"a table with the header '{header}' is expected");
        return lines.Skip(start + 2).TakeWhile(line => line.StartsWith('|'))
            .Select(line => line.Trim('|').Split('|').Select(cell => cell.Trim()).ToArray())
            .ToList();
    }

    private static IEnumerable<string> OptionNames(string text) =>
        Regex.Matches(text, @"--[A-Za-z]+").Select(match => match.Value);

    private static string HelpText(CliCommand? command)
    {
        using var writer = new StringWriter();
        if (command is { } selected)
        {
            new HelpWriter().WriteCommandHelp(writer, selected);
        }
        else
        {
            new HelpWriter().WriteHelp(writer);
        }

        return writer.ToString();
    }

    private static readonly string[] GlobalOptionNames = { "--Output", "--OutputFile", "--Config", "--Verbose", "--Help" };

    private static CliCommand ParseCommand(string option)
    {
        return option switch
        {
            "--SelectDWD" => CliCommand.SelectDwd,
            "--ParseDWD" => CliCommand.ParseDwd,
            "--ParseOstHessen" => CliCommand.ParseOstHessen,
            "--SelectMOSMIX" => CliCommand.SelectMosmix,
            "--ParseMOSMIX" => CliCommand.ParseMosmix,
            _ => throw new ArgumentOutOfRangeException(nameof(option), option, null)
        };
    }
}
