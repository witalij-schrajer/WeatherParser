using System;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Output;
using WeatherParser.Cli.Tests.Configuration;
using WeatherParser.Cli.Mqtt;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Mqtt;
using MQTTnet;

namespace WeatherParser.Cli.Tests.Commands;

[TestClass]
[TestCategory("T-P3-10")]
public class CommandRegistrarTests
{
    [TestMethod]
    [DataRow(CliCommand.SelectDwd, OutputFormat.Json)]
    [DataRow(CliCommand.SelectMosmix, OutputFormat.Json)]
    [DataRow(CliCommand.ParseDwd, OutputFormat.Json)]
    [DataRow(CliCommand.ParseOstHessen, OutputFormat.Json)]
    [DataRow(CliCommand.ParseMosmix, OutputFormat.Json)]
    [DataRow(CliCommand.ParseDwd, OutputFormat.Xml)]
    public void Register_DocumentMode_ResolvesHandlerAndDocumentWriter(CliCommand command, OutputFormat output)
    {
        var request = TestConfigurations.Request(command, output);
        using var provider = Build(TestConfigurations.Valid(), request);

        provider.GetRequiredService(HandlerType(request)).Should().NotBeNull();
        provider.GetRequiredService<IResultWriter>().Should().BeOfType<DocumentResultWriter>();
        provider.GetService<IMessagePublisher>().Should().BeNull();
        provider.GetService<IMqttPublicationSettingsFactory>().Should().BeNull();
        provider.GetService<IMqttClient>().Should().BeNull();
    }

    [TestMethod]
    [DataRow(CliCommand.SelectDwd)]
    [DataRow(CliCommand.SelectMosmix)]
    [DataRow(CliCommand.ParseDwd)]
    [DataRow(CliCommand.ParseOstHessen)]
    [DataRow(CliCommand.ParseMosmix)]
    public void Register_MqttMode_ResolvesHandlerAndMqttWriter(CliCommand command)
    {
        var request = TestConfigurations.Request(command, OutputFormat.Mqtt);
        using var provider = Build(TestConfigurations.Valid(), request);

        provider.GetRequiredService(HandlerType(request)).Should().NotBeNull();
        provider.GetRequiredService<IResultWriter>().Should().BeOfType<MqttResultWriter>();
    }

    [TestMethod]
    public void Register_MqttMode_RegistersTheMqttNetTransport()
    {
        var request = TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Mqtt);
        using var provider = Build(TestConfigurations.Valid(), request);

        provider.GetRequiredService<IMessagePublisher>().Should().BeOfType<MqttPublisher>();
        provider.GetRequiredService<IMqttClientAdapter>().Should().BeOfType<MqttNetClientAdapter>();
        provider.GetRequiredService<IMqttNetOptionsBuilder>().Should().BeOfType<MqttNetOptionsBuilder>();
        provider.GetRequiredService<IMqttClient>().Should().NotBeNull();
    }

    [TestMethod]
    public async Task Register_MqttMode_WriterQueuesIntoTheRegisteredQueue()
    {
        var request = TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Mqtt);
        using var provider = Build(TestConfigurations.Valid(), request);

        var writer = provider.GetRequiredService<IResultWriter>();
        var queue = provider.GetRequiredService<IPublicationQueue>();
        
        await writer.WriteAsync(TestExportData.CdcStations("00044"), new CommandContext(request.Common, null, TextWriter.Null), CancellationToken.None);
        
        queue.Messages.Should().ContainSingle().Which.MessageType.Should().Be(MqttMessageType.CdcStationList);
    }

    [TestMethod]
    public void Register_ParseMosmixS_WithoutMosmixLSection_ResolvesHandler()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mosmix!.L = null;
        var request = new ParseMosmixRequest(new CommonOptions(OutputFormat.Json, null, null, false), MosmixProduct.MosmixS, "10637");

        using var provider = Build(configuration, request);

        provider.GetRequiredService(HandlerType(request)).Should().NotBeNull();
    }

    [TestMethod]
    public void Register_ParseMosmixL_WithoutMosmixSSection_ResolvesHandler()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mosmix!.S = null;
        var request = new ParseMosmixRequest(new CommonOptions(OutputFormat.Json, null, null, false), MosmixProduct.MosmixL, "10637");

        using var provider = Build(configuration, request);

        provider.GetRequiredService(HandlerType(request)).Should().NotBeNull();
    }

    [TestMethod]
    public void Register_SelectMosmixWithRefresh_ResolvesHandler()
    {
        var configuration = TestConfigurations.Valid();
        var request = new SelectMosmixRequest(new CommonOptions(OutputFormat.Json, null, null, false), TestExportData.Search(), true);

        using var provider = Build(configuration, request);

        provider.GetRequiredService(HandlerType(request)).Should().NotBeNull();
    }

    private static ServiceProvider Build(CliConfiguration configuration, CommandRequest request) => 
        CompositionRoot.Build(services => 
        { 
            new ConfigurationRegistrar().Register(services, configuration, request); 
            new CommandRegistrar().Register(services, request); 
            services.AddLogging(); 
        });

    private static Type HandlerType(CommandRequest request) => typeof(ICommandHandler<>).MakeGenericType(request.GetType());
}
