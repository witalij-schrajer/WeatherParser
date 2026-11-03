using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixLocationCatalogReaderTests
{
    private const string Endpoint = "mosmix-catalog";
    private IMosmixLocationCatalogReader _subject = null!;
    private MockRepository _mocks = null!;
    private Mock<ILogger<MosmixLocationCatalogReader>> _logger = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _logger = _mocks.Create<ILogger<MosmixLocationCatalogReader>>();
        _subject = new MosmixLocationCatalogReader(_logger.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private Task<MosmixLocationCatalog> Read(byte[] bytes) => _subject.ReadAsync(new MemoryStream(bytes), Endpoint, CancellationToken.None);

    private static async Task ShouldBeFormatFailure(Func<Task> act)
    {
        (await act.Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be(CatalogFormatException.CatalogFormatInvalidErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_ValidFixture_ReturnsAllLocations()
    {
        var bytes = MosmixCatalogBuilder.Catalog(
            MosmixCatalogBuilder.Line("X123A", name: "MUSTERHAUSEN", latitude: "50.00", longitude: "9.00", elevation: "250"),
            MosmixCatalogBuilder.Line("01234", icao: "EDXX", name: "BEISPIELDORF", latitude: "-12.00", longitude: "-45.00", elevation: "0"),
            MosmixCatalogBuilder.Line("B456", name: "FELDHAUSEN", latitude: "47.00", longitude: "11.00", elevation: "1200"));

        var result = await Read(bytes);

        result.Records.Should().Equal(
            new MosmixLocationRecord("X123A", "MUSTERHAUSEN", 50m, 9m, 250m, 3),
            new MosmixLocationRecord("01234", "BEISPIELDORF", -12m, -45m, 0m, 4),
            new MosmixLocationRecord("B456", "FELDHAUSEN", 47m, 11m, 1200m, 5));

        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_KeepsAlphanumericAndLeadingZeroIds()
    {
        var bytes = MosmixCatalogBuilder.Catalog(
            MosmixCatalogBuilder.Line("X123A"),
            MosmixCatalogBuilder.Line("01234"),
            MosmixCatalogBuilder.Line("00044"));

        var result = await Read(bytes);

        result.Records.Should().HaveCount(3);
        result.Records[0].MosmixId.Should().Be("X123A");
        result.Records[1].MosmixId.Should().Be("01234");
        result.Records[2].MosmixId.Should().Be("00044");
    }

    [TestMethod]
    public async Task ReadAsync_PreservesUmlautsInNames()
    {
        var bytes = MosmixCatalogBuilder.Catalog(
            MosmixCatalogBuilder.Line(name: "M\u00DCNCHEN-S\u00DCD"));

        var result = await Read(bytes);

        result.Records.Should().ContainSingle();
        result.Records[0].Name.Should().Be("M\u00DCNCHEN-S\u00DCD");
    }

    [TestMethod]
    public async Task ReadAsync_LeavesStreamOpenOnSuccess()
    {
        var stream = new MemoryStream(MosmixCatalogBuilder.Catalog(MosmixCatalogBuilder.Line()));
        await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task AddWeatherParsing_ResolvesMosmixLocationCatalogReader()
    {
        var services = new ServiceCollection();
        services.AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var reader = provider.GetRequiredService<IMosmixLocationCatalogReader>();
        reader.Should().BeOfType<MosmixLocationCatalogReader>();
    }

    [TestMethod]
    public async Task ReadAsync_HeaderTokensReordered_IsFormatFailure()
    {
        var bytes = MosmixCatalogBuilder.Text(
            "ID    ICAO NAME                 LON    LAT     ELEV\n" +
            "----- ---- -------------------- ------ ------- -----\n" +
            MosmixCatalogBuilder.Line());

        await ShouldBeFormatFailure(() => Read(bytes));
    }

    [TestMethod]
    public async Task ReadAsync_HeaderWithDifferentPadding_Accepted()
    {
        var bytes = MosmixCatalogBuilder.Text(
            "ID ICAO NAME LAT LON ELEV\n" +
            "----- ---- -------------------- ------ ------- -----\n" +
            MosmixCatalogBuilder.Line());

        var result = await Read(bytes);
        result.Records.Should().ContainSingle();
    }

    [TestMethod]
    public async Task ReadAsync_DashLineMismatch_IsFormatFailure()
    {
        var bytes = MosmixCatalogBuilder.Text(
            "ID    ICAO NAME                 LAT    LON     ELEV\n" +
            "===== ==== ====================\n" +
            MosmixCatalogBuilder.Line());

        await ShouldBeFormatFailure(() => Read(bytes));
    }

    [TestMethod]
    public async Task ReadAsync_FourCharacterIdWithLeadingSpace_Accepted()
    {
        var bytes = MosmixCatalogBuilder.Catalog(
            MosmixCatalogBuilder.Line("A051"));

        var result = await Read(bytes);
        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A051");
    }

    [TestMethod]
    public async Task ReadAsync_IdNotMatchingGrammar_Skipped()
    {
        var bytes = MosmixCatalogBuilder.Catalog(
            MosmixCatalogBuilder.Line(),
            MosmixCatalogBuilder.Line("a051x"),
            MosmixCatalogBuilder.Line("AB-12"),
            MosmixCatalogBuilder.Line("123"));

        var result = await Read(bytes);

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("X123A");

        result.Diagnostics.Should().HaveCount(3);
        result.Diagnostics[0].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[0].EndpointName.Should().Be(Endpoint);
        result.Diagnostics[0].LineNumber.Should().Be(4);

        result.Diagnostics[1].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[1].EndpointName.Should().Be(Endpoint);
        result.Diagnostics[1].LineNumber.Should().Be(5);

        result.Diagnostics[2].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[2].EndpointName.Should().Be(Endpoint);
        result.Diagnostics[2].LineNumber.Should().Be(6);
    }

    [TestMethod]
    public async Task ReadAsync_CharacterFiveNotASpace_Skipped()
    {
        var bytes = MosmixCatalogBuilder.Catalog(
            MosmixCatalogBuilder.Line(),
            "123456 ---- SECHSZIFFRIG           50.00    9.00   100");

        var result = await Read(bytes);

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("X123A");

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[0].EndpointName.Should().Be(Endpoint);
        result.Diagnostics[0].LineNumber.Should().Be(4);
    }

    [TestMethod]
    public async Task ReadAsync_EmptyName_Skipped()
    {
        var bytes = MosmixCatalogBuilder.Catalog(
            MosmixCatalogBuilder.Line(),
            MosmixCatalogBuilder.Line("Y987", name: ""));

        var result = await Read(bytes);

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("X123A");

        result.Diagnostics.Should().ContainSingle();
        result.Diagnostics[0].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[0].EndpointName.Should().Be(Endpoint);
        result.Diagnostics[0].LineNumber.Should().Be(4);
    }
}
