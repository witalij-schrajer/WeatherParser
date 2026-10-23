using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixLocationCatalogReaderLineTests
{
    private MockRepository _mocks = null!;
    private Mock<ILogger<MosmixLocationCatalogReader>> _logger = null!;
    private MosmixLocationCatalogReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _logger = _mocks.Create<ILogger<MosmixLocationCatalogReader>>();
        _subject = new(_logger.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    [TestMethod]
    public async Task ReadAsync_DashLineOf2049Characters_IsFormatFailure()
    {
        // Arrange
        var data = MosmixCatalogBuilder.Text(MosmixCatalogBuilder.Header + "\n" + MosmixCatalogBuilder.DashLine + new string(' ', 2049 - MosmixCatalogBuilder.DashLine.Length) + "\n" + MosmixCatalogBuilder.Line() + "\n");

        // Act
        var act = FluentActions.Awaiting(() => _subject.ReadAsync(new MemoryStream(data), "mosmix-catalog", CancellationToken.None));

        // Assert
        var ex = await act.Should().ThrowAsync<CatalogFormatException>();
        ex.Which.ErrorId.Should().Be("CATALOG-FORMAT-INVALID");
    }

    [TestMethod]
    public async Task ReadAsync_BlankLines_AreIgnoredAndStillCounted()
    {
        // Arrange
        var data = MosmixCatalogBuilder.Catalog(
            MosmixCatalogBuilder.Line("A001"),
            "",
            "   ",
            MosmixCatalogBuilder.Line("A002"));

        // Act
        var result = await _subject.ReadAsync(new MemoryStream(data), "mosmix-catalog", CancellationToken.None);

        // Assert
        result.Records.Should().HaveCount(2);
        result.Records[0].MosmixId.Should().Be("A001");
        result.Records[0].LineNumber.Should().Be(3);
        result.Records[1].MosmixId.Should().Be("A002");
        result.Records[1].LineNumber.Should().Be(6);
        result.Diagnostics.Should().BeEmpty();
    }
}
