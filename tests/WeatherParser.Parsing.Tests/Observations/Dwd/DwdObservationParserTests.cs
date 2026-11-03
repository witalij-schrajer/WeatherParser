using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdObservationParserTests
{
    private MockRepository _mocks = null!;
    private Mock<IDwdStationResolver> _resolver = null!;
    private Mock<IDwdObservationSource> _source = null!;
    private Mock<IDwdObservationMapper> _mapper = null!;
    private Mock<IObservationDatasetBuilder> _builder = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<DwdObservationParser> _logger = null!;
    private CancellationTokenSource _caller = null!;
    private DwdObservationParser _subject = null!;

    private static readonly DateTime Retrieved = new(2026, 10, 24, 12, 20, 30, DateTimeKind.Utc);
    private static readonly IReadOnlyList<CdcProductList> Lists = new[] { new CdcProductList("tu", "AirTemperatureStationList", new CdcStationCatalog(Array.Empty<CdcStationRecord>(), Array.Empty<CatalogDiagnostic>())) };

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _resolver = _mocks.Create<IDwdStationResolver>();
        _source = _mocks.Create<IDwdObservationSource>();
        _mapper = _mocks.Create<IDwdObservationMapper>();
        _builder = _mocks.Create<IObservationDatasetBuilder>();
        _clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 24, 12, 20, 30, 750, TimeSpan.Zero));
        _logger = new CapturingLogger<DwdObservationParser>();
        _caller = new CancellationTokenSource();
        _subject = new DwdObservationParser(new DwdObservationOptions(), _resolver.Object, _source.Object, _mapper.Object, _builder.Object, _clock, _logger);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _caller.Dispose();
        _mocks.VerifyAll();
    }

    private static ObservationLocation Location() => new() { StationId = "00044", Name = "Station", Latitude = 50.1m, Longitude = 8.6m, AltitudeM = 100m };

    private static DwdResolvedStation Station(params string[] products) => new("00044", Location(), products);

    private static CdcNowEvidence Evidence(string code) => new("00044", code, Array.Empty<CdcNowEvidenceRow>());

    private void ArrangeUntilArchives(DwdResolvedStation station, IReadOnlyDictionary<string, CdcNowEvidence?> archives)
    {
        _resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(Lists);
        _resolver.Setup(r => r.Resolve("00044", Lists)).Returns(station);
        _source.Setup(s => s.ReadArchivesAsync("00044", station.ProductCodes, It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(archives);
    }

    private static DwdObservationParser RealBuilderParser(Mock<IDwdStationResolver> resolver, Mock<IDwdObservationSource> source, Mock<IDwdObservationMapper> mapper, ManualTimeProvider clock, CapturingLogger<DwdObservationParser> logger) =>
        new(new DwdObservationOptions(), resolver.Object, source.Object, mapper.Object, new ObservationDatasetBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter()), clock, logger);

    private static ObservationReading Temperature(double value, DateTime time) => new() { Kind = QuantityKind.Temperature, Value = value, Time = new NormalizedSourceTime(time, TimestampQuality.Source, null) };

    [TestMethod]
    public async Task ParseAsync_ValidStation_ReturnsDatasetFromBuilder()
    {
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        var readings = new[] { Temperature(12.3, Retrieved) };
        _mapper.Setup(m => m.Map(It.Is<IReadOnlyList<CdcNowEvidence>>(e => e.Count == 1 && e[0].ProductCode == "tu"))).Returns(new DwdMappingResult(readings, Array.Empty<DwdMappingOmission>()));
        var dataset = new ObservationDataset();
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Returns(new ObservationBuildResult(dataset, Array.Empty<OmittedReading>()));

        (await _subject.ParseAsync("00044", _caller.Token)).Should().BeSameAs(dataset);
    }

    [TestMethod]
    public async Task ParseAsync_ReadsReferenceInstantOnceAndTruncatesToSeconds()
    {
        _resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        _source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).Returns(() =>
        {
            _clock.Advance(TimeSpan.FromSeconds(60));
            return Task.FromResult(Lists);
        });
        var station = Station("tu");
        _resolver.Setup(r => r.Resolve("00044", Lists)).Returns(station);
        _source.Setup(s => s.ReadArchivesAsync("00044", station.ProductCodes, It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });

        var readings = new[] { Temperature(12.3, Retrieved) };
        _mapper.Setup(m => m.Map(It.Is<IReadOnlyList<CdcNowEvidence>>(e => e.Count == 1 && e[0].ProductCode == "tu"))).Returns(new DwdMappingResult(readings, Array.Empty<DwdMappingOmission>()));
        ObservationInput? input = null;
        var dataset = new ObservationDataset();
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Callback<ObservationInput>(i => input = i).Returns(new ObservationBuildResult(dataset, Array.Empty<OmittedReading>()));

        await _subject.ParseAsync("00044", _caller.Token);

        _clock.GetUtcNowCalls.Should().Be(1);
        input!.RetrievedAtUtc.Should().Be(Retrieved);
    }

    [TestMethod]
    public async Task ParseAsync_PassesProviderDwdLocationAndReadings()
    {
        ArrangeUntilArchives(Station("tu"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu") });
        var readings = new[] { Temperature(12.3, Retrieved) };
        _mapper.Setup(m => m.Map(It.Is<IReadOnlyList<CdcNowEvidence>>(e => e.Count == 1 && e[0].ProductCode == "tu"))).Returns(new DwdMappingResult(readings, Array.Empty<DwdMappingOmission>()));
        ObservationInput? input = null;
        var dataset = new ObservationDataset();
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Callback<ObservationInput>(i => input = i).Returns(new ObservationBuildResult(dataset, Array.Empty<OmittedReading>()));

        await _subject.ParseAsync("00044", _caller.Token);

        input!.Provider.Should().Be("dwd");
        input.Location!.StationId.Should().Be("00044");
        input.Readings.Should().BeSameAs(readings);
    }

    [TestMethod]
    public async Task ParseAsync_RequestsOnlyResolvedProducts()
    {
        ArrangeUntilArchives(Station("tu", "sd"), new Dictionary<string, CdcNowEvidence?> { ["tu"] = Evidence("tu"), ["sd"] = Evidence("sd") });
        var readings = new[] { Temperature(12.3, Retrieved) };
        _mapper.Setup(m => m.Map(It.Is<IReadOnlyList<CdcNowEvidence>>(e => e.Count == 2 && e[0].ProductCode == "tu" && e[1].ProductCode == "sd"))).Returns(new DwdMappingResult(readings, Array.Empty<DwdMappingOmission>()));
        var dataset = new ObservationDataset();
        _builder.Setup(b => b.Build(It.IsAny<ObservationInput>())).Returns(new ObservationBuildResult(dataset, Array.Empty<OmittedReading>()));

        await _subject.ParseAsync("00044", _caller.Token);
    }

    [TestMethod]
    public async Task ParseAsync_SourceIsDwdColonStationId()
    {
        var resolver = _mocks.Create<IDwdStationResolver>();
        var source = _mocks.Create<IDwdObservationSource>();
        var mapper = _mocks.Create<IDwdObservationMapper>();
        var logger = new CapturingLogger<DwdObservationParser>();
        var parser = RealBuilderParser(resolver, source, mapper, _clock, logger);

        resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(Lists);
        resolver.Setup(r => r.Resolve("00044", Lists)).Returns(Station("tu"));
        var evidence = Evidence("tu");
        source.Setup(s => s.ReadArchivesAsync("00044", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(new Dictionary<string, CdcNowEvidence?> { ["tu"] = evidence });
        var readings = new[] { Temperature(12.3, Retrieved.AddMinutes(-20)), new ObservationReading { Kind = QuantityKind.RelativeHumidity, Value = 80.0, Time = new NormalizedSourceTime(Retrieved.AddMinutes(-10), TimestampQuality.Source, null) } };
        mapper.Setup(m => m.Map(It.Is<IReadOnlyList<CdcNowEvidence>>(e => e.Count == 1 && e[0].ProductCode == "tu"))).Returns(new DwdMappingResult(readings, Array.Empty<DwdMappingOmission>()));

        var result = await parser.ParseAsync("00044", _caller.Token);

        result.Source.Should().Be("dwd:00044");
    }

    [TestMethod]
    public async Task ParseAsync_DatasetMeasuredAtIsLatestReading()
    {
        var resolver = _mocks.Create<IDwdStationResolver>();
        var source = _mocks.Create<IDwdObservationSource>();
        var mapper = _mocks.Create<IDwdObservationMapper>();
        var logger = new CapturingLogger<DwdObservationParser>();
        var parser = RealBuilderParser(resolver, source, mapper, _clock, logger);

        resolver.Setup(r => r.ValidateId("00044")).Returns("00044");
        source.Setup(s => s.ReadStationListsAsync(It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(Lists);
        resolver.Setup(r => r.Resolve("00044", Lists)).Returns(Station("tu"));
        var evidence = Evidence("tu");
        source.Setup(s => s.ReadArchivesAsync("00044", It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>(), _caller.Token)).ReturnsAsync(new Dictionary<string, CdcNowEvidence?> { ["tu"] = evidence });
        var readings = new[] { Temperature(12.3, Retrieved.AddMinutes(-20)), new ObservationReading { Kind = QuantityKind.RelativeHumidity, Value = 80.0, Time = new NormalizedSourceTime(Retrieved.AddMinutes(-10), TimestampQuality.Source, null) } };
        mapper.Setup(m => m.Map(It.Is<IReadOnlyList<CdcNowEvidence>>(e => e.Count == 1 && e[0].ProductCode == "tu"))).Returns(new DwdMappingResult(readings, Array.Empty<DwdMappingOmission>()));

        var result = await parser.ParseAsync("00044", _caller.Token);

        result.MeasuredAtUtc.Should().Be(Retrieved.AddMinutes(-10));
    }

    [TestMethod]
    public async Task ParseAsync_NullStationId_ThrowsStationIdInvalidBeforeAnySourceCall()
    {
        _resolver.Setup(r => r.ValidateId(null!)).Throws(new DwdStationException(DwdStationException.StationIdInvalidErrorId, "Invalid."));

        Func<Task> act = () => _subject.ParseAsync(null!, _caller.Token);

        await act.Should().ThrowAsync<DwdStationException>().Where(ex => ex.ErrorId == DwdStationException.StationIdInvalidErrorId);
        _logger.Entries.Should().BeEmpty();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesDwdObservationParser()
    {
        var services = new ServiceCollection();
        services.AddWeatherParsing().AddTestConsumerServices();

        var provider = services.BuildServiceProvider();

        var parser = provider.GetRequiredService<IDwdObservationParser>();

        parser.Should().BeOfType<DwdObservationParser>();
    }

    [TestMethod]
    public void Constructor_NullArguments_Throw()
    {
        Action act = () => new DwdObservationParser(null!, _resolver.Object, _source.Object, _mapper.Object, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");

        act = () => new DwdObservationParser(new DwdObservationOptions(), null!, _source.Object, _mapper.Object, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("resolver");

        act = () => new DwdObservationParser(new DwdObservationOptions(), _resolver.Object, null!, _mapper.Object, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("source");

        act = () => new DwdObservationParser(new DwdObservationOptions(), _resolver.Object, _source.Object, null!, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("mapper");

        act = () => new DwdObservationParser(new DwdObservationOptions(), _resolver.Object, _source.Object, _mapper.Object, null!, _clock, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("builder");

        act = () => new DwdObservationParser(new DwdObservationOptions(), _resolver.Object, _source.Object, _mapper.Object, _builder.Object, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");

        act = () => new DwdObservationParser(new DwdObservationOptions(), _resolver.Object, _source.Object, _mapper.Object, _builder.Object, _clock, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
