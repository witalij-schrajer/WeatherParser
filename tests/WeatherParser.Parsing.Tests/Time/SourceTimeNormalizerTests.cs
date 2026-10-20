using WeatherParser.Parsing.Time;
using FluentAssertions;
using Moq;

namespace WeatherParser.Parsing.Tests.Time;

[TestClass]
public sealed class SourceTimeNormalizerTests
{
    private Mock<ITimeZoneResolver> _resolverMock = null!;
    private ISourceTimeNormalizer _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _resolverMock = new Mock<ITimeZoneResolver>(MockBehavior.Strict);
        _subject = new SourceTimeNormalizer(_resolverMock.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _resolverMock.VerifyAll();
    }

    [TestMethod]
    public void NormalizeComplete_WithOffset_ReturnsUtcAndSourceQuality()
    {
        // Arrange
        var input = new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(2));

        // Act
        var result = _subject.NormalizeComplete(input);

        // Assert
        result.UtcInstant.Should().Be(new DateTime(2026, 10, 25, 0, 30, 0, DateTimeKind.Utc));
        result.UtcInstant.Kind.Should().Be(DateTimeKind.Utc);
        result.Quality.Should().Be(TimestampQuality.Source);
        result.SourceTimeZone.Should().BeNull();
    }

    private static DateTime Utc(int y, int mo, int d, int h, int mi) => new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Utc);

    private void SetupBerlin() => _resolverMock.Setup(r => r.Resolve("Europe/Berlin")).Returns(TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));

    [TestMethod]
    public void NormalizeCompleteLocal_BerlinLocal_ConvertsWinterAndSummer()
    {
        // Arrange
        SetupBerlin();

        // Act
        var result1 = _subject.NormalizeCompleteLocal(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Unspecified));
        var result2 = _subject.NormalizeCompleteLocal(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Unspecified));

        // Assert
        result1.UtcInstant.Should().Be(new DateTime(2026, 1, 15, 11, 0, 0, DateTimeKind.Utc));
        result1.UtcInstant.Kind.Should().Be(DateTimeKind.Utc);
        result1.Quality.Should().Be(TimestampQuality.Source);
        result1.SourceTimeZone.Should().Be("Europe/Berlin");

        result2.UtcInstant.Should().Be(new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc));
        result2.UtcInstant.Kind.Should().Be(DateTimeKind.Utc);
        result2.Quality.Should().Be(TimestampQuality.Source);
        result2.SourceTimeZone.Should().Be("Europe/Berlin");
    }

    [TestMethod]
    public void NormalizeCompleteLocal_NonexistentLocalTime_IsRejected()
    {
        // Arrange
        SetupBerlin();

        // Act & Assert
        FluentActions.Invoking(() => _subject.NormalizeCompleteLocal(new DateTime(2026, 3, 29, 2, 30, 0, DateTimeKind.Unspecified)))
            .Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void NormalizeCompleteLocal_AmbiguousLocalTimeWithoutOffset_IsRejected()
    {
        // Arrange
        SetupBerlin();

        // Act & Assert
        FluentActions.Invoking(() => _subject.NormalizeCompleteLocal(new DateTime(2026, 10, 25, 2, 30, 0, DateTimeKind.Unspecified)))
            .Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void InferDate_AfterLocalMidnight_UsesLocalRetrievalDate()
    {
        // Arrange
        SetupBerlin();

        // Act
        var result = _subject.InferDate(new TimeOnly(0, 2), Utc(2026, 7, 15, 22, 5));

        // Assert
        result.UtcInstant.Should().Be(Utc(2026, 7, 15, 22, 2));
        result.UtcInstant.Kind.Should().Be(DateTimeKind.Utc);
    }

    [TestMethod]
    public void InferDate_DisplayAfterRetrievalTime_UsesPreviousDay()
    {
        // Arrange
        SetupBerlin();

        // Act
        var result = _subject.InferDate(new TimeOnly(23, 55), Utc(2026, 7, 15, 22, 5));

        // Assert
        result.UtcInstant.Should().Be(Utc(2026, 7, 15, 21, 55));
        result.UtcInstant.Kind.Should().Be(DateTimeKind.Utc);
    }

    [TestMethod]
    public void InferDate_SetsEstimatedDateAndBerlinMarkers()
    {
        // Arrange
        SetupBerlin();

        // Act
        var result = _subject.InferDate(new TimeOnly(11, 50), Utc(2026, 7, 15, 10, 0));

        // Assert
        result.UtcInstant.Should().Be(Utc(2026, 7, 15, 9, 50));
        result.UtcInstant.Kind.Should().Be(DateTimeKind.Utc);
        result.Quality.Should().Be(TimestampQuality.EstimatedDate);
        result.SourceTimeZone.Should().Be("Europe/Berlin");
    }

    [TestMethod]
    public void InferDate_ToleranceBoundary_FiveMinutes()
    {
        // Arrange
        SetupBerlin();

        // Act & Assert
        var result1 = _subject.InferDate(new TimeOnly(12, 5), Utc(2026, 7, 15, 10, 0));
        result1.UtcInstant.Should().Be(Utc(2026, 7, 15, 10, 5));

        var result2 = _subject.InferDate(new TimeOnly(12, 6), Utc(2026, 7, 15, 10, 0));
        result2.UtcInstant.Should().Be(Utc(2026, 7, 14, 10, 6));
    }

    [TestMethod]
    public void InferDate_DisplayAmbiguousOnRetrievalDate_IsRejected()
    {
        // Arrange
        SetupBerlin();

        // Act & Assert
        FluentActions.Invoking(() => _subject.InferDate(new TimeOnly(2, 45), Utc(2026, 10, 25, 1, 15)))
            .Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void InferDate_RolledOverDateAmbiguous_IsRejected()
    {
        // Arrange
        SetupBerlin();

        // Act & Assert
        FluentActions.Invoking(() => _subject.InferDate(new TimeOnly(2, 45), Utc(2026, 10, 25, 23, 30)))
            .Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void InferDate_DisplayNonexistentOnRetrievalDate_IsRejected()
    {
        // Arrange
        SetupBerlin();

        // Act & Assert
        FluentActions.Invoking(() => _subject.InferDate(new TimeOnly(2, 30), Utc(2026, 3, 29, 10, 0)))
            .Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void Normalize_NullInputs_AreRejectedAndNeverUseRetrievalTime()
    {
        // Act & Assert
        FluentActions.Invoking(() => _subject.NormalizeComplete(null))
            .Should().Throw<SourceTimeNormalizationException>();

        FluentActions.Invoking(() => _subject.NormalizeCompleteLocal(null))
            .Should().Throw<SourceTimeNormalizationException>();

        FluentActions.Invoking(() => _subject.InferDate(null, Utc(2026, 7, 15, 10, 0)))
            .Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void Normalize_NonUtcRetrievalInstant_IsRejected()
    {
        // Act & Assert
        FluentActions.Invoking(() => _subject.InferDate(new TimeOnly(12, 0), new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Local)))
            .Should().Throw<SourceTimeNormalizationException>();

        FluentActions.Invoking(() => _subject.InferDate(new TimeOnly(12, 0), new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Unspecified)))
            .Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void Normalize_SubSecondInput_IsRejected()
    {
        // Act & Assert
        FluentActions.Invoking(() => _subject.NormalizeComplete(new DateTimeOffset(2026, 7, 15, 12, 0, 0, 500, TimeSpan.Zero)))
            .Should().Throw<SourceTimeNormalizationException>();

        FluentActions.Invoking(() => _subject.NormalizeCompleteLocal(new DateTime(2026, 7, 15, 12, 0, 0, 500, DateTimeKind.Unspecified)))
            .Should().Throw<SourceTimeNormalizationException>();

        FluentActions.Invoking(() => _subject.InferDate(new TimeOnly(12, 0, 0, 500), Utc(2026, 7, 15, 10, 0)))
            .Should().Throw<SourceTimeNormalizationException>();

        FluentActions.Invoking(() => _subject.InferDate(new TimeOnly(12, 0), Utc(2026, 7, 15, 10, 0).AddMilliseconds(1)))
            .Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void NormalizeCompleteLocal_NonUnspecifiedKind_IsRejected()
    {
        // Act & Assert
        FluentActions.Invoking(() => _subject.NormalizeCompleteLocal(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)))
            .Should().Throw<SourceTimeNormalizationException>();

        FluentActions.Invoking(() => _subject.NormalizeCompleteLocal(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Local)))
            .Should().Throw<SourceTimeNormalizationException>();
    }

    [TestMethod]
    public void InferDate_DisplayedMidnight_IsValid()
    {
        // Arrange
        SetupBerlin();

        // Act
        var result = _subject.InferDate(new TimeOnly(0, 0), Utc(2026, 7, 15, 22, 5));

        // Assert
        result.UtcInstant.Should().Be(Utc(2026, 7, 15, 22, 0));
        result.Quality.Should().Be(TimestampQuality.EstimatedDate);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Normalize_ResolverCannotProvideZone_RaisesDefinedFailure(bool invalidZoneData)
    {
        Exception failure = invalidZoneData
            ? new InvalidTimeZoneException("corrupt")
            : new TimeZoneNotFoundException("not found");
        _resolverMock.Setup(r => r.Resolve("Europe/Berlin")).Throws(failure);

        FluentActions.Invoking(() => _subject.NormalizeCompleteLocal(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Unspecified)))
            .Should().Throw<SourceTimeNormalizationException>()
            .WithMessage("*Europe/Berlin*")
            .Where(e => e.InnerException == failure);
    }

    [TestMethod]
    public void Constructor_NullResolver_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new SourceTimeNormalizer(null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("timeZoneResolver");
    }

    [TestMethod]
    public void NormalizeComplete_SecondPassOfOverlapWithOffset_ReturnsUtc()
    {
        var result = _subject.NormalizeComplete(new DateTimeOffset(2026, 10, 25, 2, 30, 0, TimeSpan.FromHours(1)));

        result.UtcInstant.Should().Be(new DateTime(2026, 10, 25, 1, 30, 0, DateTimeKind.Utc));
        result.UtcInstant.Kind.Should().Be(DateTimeKind.Utc);
        result.Quality.Should().Be(TimestampQuality.Source);
        result.SourceTimeZone.Should().BeNull();
    }
}
