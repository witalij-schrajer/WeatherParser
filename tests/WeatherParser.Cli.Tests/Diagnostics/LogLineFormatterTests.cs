using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeatherParser.Cli.Diagnostics;

namespace WeatherParser.Cli.Tests.Diagnostics;

[TestClass]
[TestCategory("T-P3-08")]
public class LogLineFormatterTests
{
    private readonly DateTimeOffset _timestamp = new(2026, 10, 29, 21, 5, 7, TimeSpan.FromHours(2));

    [TestMethod]
    public void LogLine_ContainsUtcSeverityIdCommandSource()
    {
        var formatter = new LogLineFormatter();
        var result = formatter.Format(_timestamp, LogLevel.Error, "STATION-UNKNOWN", "ParseDWD", "dwd:00044", "The station is unknown.", null);
        result.Should().Be("2026-10-29T19:05:07 UTC ERROR STATION-UNKNOWN command=ParseDWD source=dwd:00044 The station is unknown.");
    }

    [TestMethod]
    public void LogLine_WithoutSource_OmitsSourceField()
    {
        var formatter = new LogLineFormatter();
        var result = formatter.Format(_timestamp, LogLevel.Warning, "SELECTION-EMPTY", "SelectDWD", null, "No station matches.", null);
        result.Should().Be("2026-10-29T19:05:07 UTC WARNING SELECTION-EMPTY command=SelectDWD No station matches.");
    }

    [TestMethod]
    public void LogLine_WithoutIdAndCommand_UsesDashes()
    {
        var formatter = new LogLineFormatter();
        var result = formatter.Format(_timestamp, LogLevel.Information, null, "-", null, "x", null);
        result.Should().Be("2026-10-29T19:05:07 UTC INFORMATION - command=- x");
    }

    [TestMethod]
    public void LogLine_SingleLine_NewlinesReplaced()
    {
        var formatter = new LogLineFormatter();
        var result = formatter.Format(_timestamp, LogLevel.Information, null, "-", null, "a\r\nb\nc", null);
        result.Should().EndWith("a  b c");
        result.Should().NotContain("\r");
        result.Should().NotContain("\n");
    }

    [TestMethod]
    public void LogLine_Exception_TypeAndMessageOnly_NoStackTrace()
    {
        var formatter = new LogLineFormatter();
        var ex = new InvalidOperationException("boom\nsecond line");
        var result = formatter.Format(_timestamp, LogLevel.Information, null, "-", null, "Failed.", ex);
        result.Should().EndWith("Failed. InvalidOperationException: boom second line");
        result.Should().NotContain(" at ");
    }

    [TestMethod]
    [TestCategory("T-P3-08")]
    public void LogLine_AggregateException_NoInnerMessages()
    {
        var formatter = new LogLineFormatter();
        var exception = new AggregateException(new InvalidOperationException("inner-secret-7c1"), new ArgumentException("second"));
        var result = formatter.Format(_timestamp, LogLevel.Information, null, "-", null, "Failed.", exception);
        result.Should().EndWith("Failed. AggregateException: 2 inner exception(s)");
        result.Should().NotContain("inner-secret-7c1");
    }

    [TestMethod]
    [TestCategory("T-P3-08")]
    public void LogLine_ThrownException_NoStackTrace()
    {
        var formatter = new LogLineFormatter();

        static Exception Throw() 
        { 
            try 
            { 
                throw new InvalidOperationException("boom"); 
            } 
            catch (Exception e) 
            { 
                return e; 
            } 
        }

        var exception = Throw();
        exception.StackTrace.Should().NotBeNull();

        var result = formatter.Format(_timestamp, LogLevel.Information, null, "-", null, "Failed.", exception);
        result.Should().EndWith("Failed. InvalidOperationException: boom");
        result.Should().NotContain(" at ");
    }
}
