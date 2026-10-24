using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Parsing.Catalogs;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixLocationCatalogReaderNegativeBoundsTests
{
    private MockRepository _mocks = null!;
    private Mock<ILogger<MosmixLocationCatalogReader>> _logger = null!;
    private IMosmixLocationCatalogReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _logger = _mocks.Create<ILogger<MosmixLocationCatalogReader>>();
        _subject = new MosmixLocationCatalogReader(_logger.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    [TestMethod]
    public async Task ReadAsync_CoordinatesJustBelowNegativeBounds_Skipped()
    {
        var catalog = MosmixCatalogBuilder.Catalog(
            MosmixCatalogBuilder.Line("A001"),
            MosmixCatalogBuilder.Line("A002", latitude: "-90.01", longitude: "9.00"),
            MosmixCatalogBuilder.Line("A003", latitude: "50.00", longitude: "-180.01"));

        var result = await _subject.ReadAsync(new MemoryStream(catalog), "mosmix-catalog", CancellationToken.None);

        result.Records.Should().ContainSingle().Which.MosmixId.Should().Be("A001");
        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics[0].LineNumber.Should().Be(4);
        result.Diagnostics[0].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[1].LineNumber.Should().Be(5);
        result.Diagnostics[1].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }
}
