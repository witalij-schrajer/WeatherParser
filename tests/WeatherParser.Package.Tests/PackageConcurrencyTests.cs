using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Concurrent use of one consumer container (contract MQ-12): parallel parses and serializations equal a sequential run, and parallel requests never cross.
/// </summary>
[TestClass]
public sealed class PackageConcurrencyTests
{
    private const string BaseUri = "https://a.example.invalid/";
    private static readonly string[] Ids = { "10637", "10865", "P0489", "01001" };
    private ConsumerSelectionHost _host = null!;
    private Services _services = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _host = ArrangedHost();
        _services = Services.From(_host.Provider);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _host.Dispose();
    }

    [TestMethod]
    public async Task Library_ParallelParsesAndSerializations_EqualSequentialResults()
    {
        var sequential = new List<IReadOnlyList<byte[]>>();
        for (var i = 0; i < 16; i++)
        {
            sequential.Add(await RunWorkload(_services, i));
        }

        var participants = Math.Clamp(Environment.ProcessorCount, 2, 4);
        var gate = new OverlapGate(participants);
        _host.Handler.OnRequest = _ => gate.Enter();

        var parallel = await Task.WhenAll(Enumerable.Range(0, 16).Select(i => Task.Run(() => RunWorkload(_services, i))));

        gate.MaxInside.Should().BeGreaterThanOrEqualTo(participants);
        for (var i = 0; i < 16; i++)
        {
            parallel[i].Should().HaveCount(sequential[i].Count, $"index {i}");
            for (var k = 0; k < sequential[i].Count; k++)
            {
                parallel[i][k].Should().Equal(sequential[i][k], $"index {i}, part {k}");
            }
        }
    }

    [TestMethod]
    public async Task Library_ParallelUse_NoCrossTalkBetweenRequests()
    {
        var participants = Math.Clamp(Environment.ProcessorCount, 2, 4);
        var gate = new OverlapGate(participants);
        _host.Handler.OnRequest = _ => gate.Enter();

        var tasks = Enumerable.Range(0, 16).Select(i => Ids[i % Ids.Length]).Select(id => Task.Run(async () =>
        {
            var result = await _services.MosmixL.ParseAsync(id, CancellationToken.None);
            var message = await _services.Mqtt.SerializeAsync(result.Run, CancellationToken.None);
            return (RequestedId: id, result.Run, Message: message);
        }));

        var results = await Task.WhenAll(tasks);

        gate.MaxInside.Should().BeGreaterThanOrEqualTo(participants);
        foreach (var (requestedId, run, message) in results)
        {
            var index = Array.IndexOf(Ids, requestedId);
            run.MosmixId.Should().Be(requestedId);
            run.Source.Should().Be("dwd-mosmix-l:" + requestedId);
            run.Location.Name.Should().Be("STATION " + requestedId);
            run.Forecasts[0].Values.TemperatureC.Should().Be(decimal.Round(280 + index * 10 - 273.15m, 1, MidpointRounding.AwayFromZero));
            message.Topic.Should().Be("haus/weather/forecasts/dwd-mosmix-l:" + requestedId + "/state");
        }

        var expected = Ids.SelectMany(id => Enumerable.Repeat(ConsumerLibrary.MosmixLUri(BaseUri, id).AbsoluteUri, 4));
        _host.Handler.Requests.Should().BeEquivalentTo(expected);
    }

    private static async Task<IReadOnlyList<byte[]>> RunWorkload(Services services, int index)
    {
        switch (index % 6)
        {
            case 0:
                return await Parts(services, await services.Dwd.ParseAsync("00044", CancellationToken.None));
            case 1:
                return await Parts(services, (await services.Osthessen.ParseAsync("Bimbach", CancellationToken.None)).Datasets.Single());
            case 2:
                return await Parts(services, (await services.MosmixL.ParseAsync(Ids[index % Ids.Length], CancellationToken.None)).Run);
            case 3:
                return await Parts(services, (await services.MosmixS.ParseAsync("10637", CancellationToken.None)).Run);
            case 4:
                return await Parts(services, await services.Cdc.SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None));
            default:
                return await Parts(services, ConsumerTestData.ValidDataset());
        }
    }

    private static async Task<IReadOnlyList<byte[]>> Parts<TRoot>(Services services, TRoot root) where TRoot : class
    {
        using var json = new System.IO.MemoryStream();
        await services.Json.SerializeAsync(root, json, CancellationToken.None);
        using var xml = new System.IO.MemoryStream();
        await services.Xml.SerializeAsync(root, xml, CancellationToken.None);
        var message = await services.Mqtt.SerializeAsync(root, CancellationToken.None);
        var envelope = Encoding.UTF8.GetBytes($"{message.Topic}|{message.QualityOfService}|{message.Retain}|{message.MessageType}");
        return new[] { json.ToArray(), xml.ToArray(), envelope, message.Payload.ToArray() };
    }

    private static ConsumerSelectionHost ArrangedHost()
    {
        var host = ConsumerLibrary.Host(BaseUri);
        host.ArrangeCdcFullStation();
        ConsumerLibrary.ArrangeOsthessen(host, BaseUri);
        for (var index = 0; index < Ids.Length; index++)
        {
            host.Handler.Add(ConsumerLibrary.MosmixLUri(BaseUri, Ids[index]), ConsumerFixtures.MosmixLForecastKmz(Ids[index], "STATION " + Ids[index], ("TTT", Kelvin(index)), ("FF", "0.00 5.00 10.00")));
        }

        ConsumerLibrary.ArrangeMosmixS(host, BaseUri, "10637");
        return host;
    }

    private static string Kelvin(int index) => string.Join(" ", Enumerable.Range(0, 3).Select(step => (280 + index * 10 + step).ToString("0.00", CultureInfo.InvariantCulture)));

    /// <summary>
    /// Every service the workloads use, resolved once from the built container before any worker starts (the host builds its provider lazily without synchronization).
    /// </summary>
    private sealed record Services(
        IDwdObservationParser Dwd,
        IOsthessenWetterParser Osthessen,
        IMosmixLParser MosmixL,
        IMosmixSParser MosmixS,
        ICdcStationSelector Cdc,
        IJsonExportSerializer Json,
        IXmlExportSerializer Xml,
        IMqttMessageSerializer Mqtt)
    {
        public static Services From(IServiceProvider provider) => new(
            provider.GetRequiredService<IDwdObservationParser>(),
            provider.GetRequiredService<IOsthessenWetterParser>(),
            provider.GetRequiredService<IMosmixLParser>(),
            provider.GetRequiredService<IMosmixSParser>(),
            provider.GetRequiredService<ICdcStationSelector>(),
            provider.GetRequiredService<IJsonExportSerializer>(),
            provider.GetRequiredService<IXmlExportSerializer>(),
            provider.GetRequiredService<IMqttMessageSerializer>());
    }

    /// <summary>
    /// Holds the first requests in the consumer handler until the given number is inside at the same time, which proves overlapping calls without timing assertions.
    /// </summary>
    private sealed class OverlapGate
    {
        private readonly CountdownEvent _arrived;
        private int _inside;
        private int _maxInside;

        public OverlapGate(int participants) => _arrived = new CountdownEvent(participants);

        public int MaxInside => Volatile.Read(ref _maxInside);

        public void Enter()
        {
            var inside = Interlocked.Increment(ref _inside);
            int seen;
            do
            {
                seen = Volatile.Read(ref _maxInside);
                if (inside <= seen)
                {
                    break;
                }
            }
            while (Interlocked.CompareExchange(ref _maxInside, inside, seen) != seen);

            try
            {
                _arrived.Signal();
            }
            catch (InvalidOperationException)
            {
            }

            _arrived.Wait(TimeSpan.FromSeconds(30));
            Interlocked.Decrement(ref _inside);
        }
    }
}
