using System.Globalization;
using FluentAssertions;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli.Tests.Configuration;

/// <summary>
/// Verifies the ranges of the CLI's own settings: limits, broker and HTTP (CL-13, CL-14, CL-18, CL-21, T-P3-05).
/// </summary>
[TestClass]
[TestCategory("T-P3-05")]
public sealed class ConfigurationValidatorSettingsTests
{
    private ConfigurationValidator _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ConfigurationValidator(new ReferenceStationResolver(), new CredentialProvider(new EnvironmentReader(), new FileModeReader()));
    }

    private Action Act(CliConfiguration configuration, OutputFormat output)
    {
        var request = TestConfigurations.Request(CliCommand.ParseDwd, output);
        return () =>
        {
            using var provider = TestConfigurations.Build(configuration, request);
            _subject.Validate(provider, configuration, request);
        };
    }

    [TestMethod]
    [DataRow("ConnectTimeout", "00:00:01", true)]
    [DataRow("ConnectTimeout", "00:00:00", false)]
    [DataRow("ConnectTimeout", "00:02:00", true)]
    [DataRow("ConnectTimeout", "00:02:01", false)]
    [DataRow("PublishTimeout", "00:00:01", true)]
    [DataRow("PublishTimeout", "00:00:00", false)]
    [DataRow("PublishTimeout", "00:02:00", true)]
    [DataRow("PublishTimeout", "00:02:01", false)]
    [DataRow("RetryDelay", "00:00:00", true)]
    [DataRow("RetryDelay", "00:00:30", true)]
    [DataRow("RetryDelay", "00:00:31", false)]
    [DataRow("RetryDelay", "-00:00:01", false)]
    [DataRow("CommandDeadline", "00:01:00", true)]
    [DataRow("CommandDeadline", "00:00:59", false)]
    [DataRow("CommandDeadline", "01:00:00", true)]
    [DataRow("CommandDeadline", "01:00:01", false)]
    [DataRow("ShutdownGracePeriod", "00:00:01", true)]
    [DataRow("ShutdownGracePeriod", "00:00:00", false)]
    [DataRow("ShutdownGracePeriod", "00:00:30", true)]
    [DataRow("ShutdownGracePeriod", "00:00:31", false)]
    public void Limits_TimeSpanRanges(string setting, string value, bool valid)
    {
        var configuration = TestConfigurations.Valid();
        var limits = new LimitsSection();

        switch (setting)
        {
            case "ConnectTimeout":
                limits.ConnectTimeout = TimeSpan.Parse(value, CultureInfo.InvariantCulture);
                break;
            case "PublishTimeout":
                limits.PublishTimeout = TimeSpan.Parse(value, CultureInfo.InvariantCulture);
                break;
            case "RetryDelay":
                limits.RetryDelay = TimeSpan.Parse(value, CultureInfo.InvariantCulture);
                break;
            case "CommandDeadline":
                limits.CommandDeadline = TimeSpan.Parse(value, CultureInfo.InvariantCulture);
                break;
            case "ShutdownGracePeriod":
                limits.ShutdownGracePeriod = TimeSpan.Parse(value, CultureInfo.InvariantCulture);
                break;
        }

        configuration.Limits = limits;

        if (valid)
        {
            Act(configuration, OutputFormat.Json).Should().NotThrow();
        }
        else
        {
            Act(configuration, OutputFormat.Json).Should().Throw<CliException>()
                .WithMessage($"The setting 'Limits.{setting}' is *")
                .And.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        }
    }

    /// <summary>
    /// Validates the maximum publish attempts limit.
    /// </summary>
    [TestMethod]
    [DataRow(1, true)]
    [DataRow(0, false)]
    [DataRow(5, true)]
    [DataRow(6, false)]
    public void Limits_MaxPublishAttempts(int value, bool valid)
    {
        var configuration = TestConfigurations.Valid();
        configuration.Limits = new LimitsSection { MaxPublishAttempts = value };

        if (valid)
        {
            Act(configuration, OutputFormat.Json).Should().NotThrow();
        }
        else
        {
            Act(configuration, OutputFormat.Json).Should().Throw<CliException>()
                .WithMessage("The setting 'Limits.MaxPublishAttempts' is *")
                .And.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        }
    }

    /// <summary>
    /// Ensures that absent limits are accepted.
    /// </summary>
    [TestMethod]
    public void Limits_Absent_Accepted()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Limits = null;

        Act(configuration, OutputFormat.Json).Should().NotThrow();
    }

    /// <summary>
    /// Checks that MQTT broker host is required when using MQTT output.
    /// </summary>
    [TestMethod]
    public void Broker_HostMissing_MqttMode_Rejected()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mqtt = new MqttSection { Broker = new BrokerSection() };

        Act(configuration, OutputFormat.Mqtt).Should().Throw<CliException>()
            .WithMessage("The MQTT output needs the setting 'Mqtt.Broker.Host'.")
            .And.ErrorId.Should().Be("CLI-CONFIG-INVALID");

        configuration.Mqtt = null;
        Act(configuration, OutputFormat.Mqtt).Should().Throw<CliException>()
            .WithMessage("The MQTT output needs the setting 'Mqtt.Broker.Host'.")
            .And.ErrorId.Should().Be("CLI-CONFIG-INVALID");
    }

    /// <summary>
    /// Ensures that broker settings are not validated in JSON mode.
    /// </summary>
    [TestMethod]
    public void Broker_NotCheckedInJsonMode()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mqtt!.Broker = new BrokerSection { Port = 0, ProtocolVersion = "4.0" };

        Act(configuration, OutputFormat.Json).Should().NotThrow();
    }

    /// <summary>
    /// Validates the port range for MQTT broker.
    /// </summary>
    [TestMethod]
    [DataRow(1, true)]
    [DataRow(0, false)]
    [DataRow(65535, true)]
    [DataRow(65536, false)]
    public void Broker_PortRange(int port, bool valid)
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mqtt!.Broker = new BrokerSection { Host = "broker.example.invalid", Port = port };

        if (valid)
        {
            Act(configuration, OutputFormat.Mqtt).Should().NotThrow();
        }
        else
        {
            Act(configuration, OutputFormat.Mqtt).Should().Throw<CliException>()
                .WithMessage("The setting 'Mqtt.Broker.Port' is *")
                .And.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        }
    }

    /// <summary>
    /// Validates the keep-alive range for MQTT broker.
    /// </summary>
    [TestMethod]
    [DataRow("00:00:05", true)]
    [DataRow("00:00:04", false)]
    [DataRow("00:05:00", true)]
    [DataRow("00:05:01", false)]
    public void Broker_KeepAliveRange(string value, bool valid)
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mqtt!.Broker = new BrokerSection
        {
            Host = "broker.example.invalid",
            KeepAlive = TimeSpan.Parse(value, CultureInfo.InvariantCulture)
        };

        if (valid)
        {
            Act(configuration, OutputFormat.Mqtt).Should().NotThrow();
        }
        else
        {
            Act(configuration, OutputFormat.Mqtt).Should().Throw<CliException>()
                .WithMessage("The setting 'Mqtt.Broker.KeepAlive' is *")
                .And.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        }
    }

    /// <summary>
    /// Validates the MQTT protocol version.
    /// </summary>
    [TestMethod]
    [DataRow("5.0", true)]
    [DataRow("3.1.1", true)]
    [DataRow("3.1", false)]
    [DataRow("4.0", false)]
    public void Broker_ProtocolVersion(string value, bool valid)
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mqtt!.Broker = new BrokerSection
        {
            Host = "broker.example.invalid",
            ProtocolVersion = value
        };

        if (valid)
        {
            Act(configuration, OutputFormat.Mqtt).Should().NotThrow();
        }
        else
        {
            Act(configuration, OutputFormat.Mqtt).Should().Throw<CliException>()
                .WithMessage($"The setting 'Mqtt.Broker.ProtocolVersion' is '{value}'; allowed are 5.0 and 3.1.1.")
                .And.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        }
    }

    /// <summary>
    /// Validates the MQTT client ID format.
    /// </summary>
    [TestMethod]
    [DataRow("a", true)]
    [DataRow("weatherparser0123456789", true)]
    [DataRow("weatherparser01234567890", false)]
    [DataRow("weather-parser", false)]
    [DataRow("", false)]
    public void Broker_ClientId(string value, bool valid)
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mqtt!.Broker = new BrokerSection
        {
            Host = "broker.example.invalid",
            ClientId = value
        };

        if (valid)
        {
            Act(configuration, OutputFormat.Mqtt).Should().NotThrow();
        }
        else
        {
            Act(configuration, OutputFormat.Mqtt).Should().Throw<CliException>()
                .WithMessage("The setting 'Mqtt.Broker.ClientId' must have 1 to 23 letters and digits.")
                .And.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        }
    }

    /// <summary>
    /// Validates the HTTP user agent string.
    /// </summary>
    [TestMethod]
    [DataRow("WeatherParser/1.0 (+example)", true)]
    [DataRow("", false)]
    [DataRow("Wetter\u00E4", false)]
    public void Http_UserAgent(string value, bool valid)
    {
        var configuration = TestConfigurations.Valid();
        configuration.Http = new HttpSection { UserAgent = value };

        if (valid)
        {
            Act(configuration, OutputFormat.Json).Should().NotThrow();
        }
        else
        {
            Act(configuration, OutputFormat.Json).Should().Throw<CliException>()
                .WithMessage("The setting 'Http.UserAgent' must have 1 to 200 printable ASCII characters.")
                .And.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        }
    }

    /// <summary>
    /// Checks the length limit for HTTP user agent.
    /// </summary>
    [TestMethod]
    public void Http_UserAgentLength()
    {
        var configuration = TestConfigurations.Valid();
        var longUserAgent = new string('a', 200);
        configuration.Http = new HttpSection { UserAgent = longUserAgent };

        Act(configuration, OutputFormat.Json).Should().NotThrow();

        longUserAgent = new string('a', 201);
        configuration.Http.UserAgent = longUserAgent;

        Act(configuration, OutputFormat.Json).Should().Throw<CliException>()
            .WithMessage("The setting 'Http.UserAgent' must have 1 to 200 printable ASCII characters.")
            .And.ErrorId.Should().Be("CLI-CONFIG-INVALID");
    }
}
