using FluentAssertions;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Geography;

[TestClass]
public sealed class SearchAreaFilterTests
{
    private MockRepository _repository = null!;
    private Mock<IDistanceCalculator> _calculator = null!;
    private ISearchAreaFilter _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new MockRepository(MockBehavior.Strict);
        _calculator = _repository.Create<IDistanceCalculator>();
        _subject = new SearchAreaFilter(_calculator.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _repository.VerifyAll();
    }

    private static SearchParameters Parameters(decimal altitudeM = 300m, decimal maxDistanceKm = 100m, decimal maxAltitudeDifferenceM = 150m) => new() { Latitude = 50m, Longitude = 9m, AltitudeM = altitudeM, MaxDistanceKm = maxDistanceKm, MaxAltitudeDifferenceM = maxAltitudeDifferenceM };

    private void SetupSpherical(decimal lat, decimal lon, decimal km) => _calculator.Setup(c => c.SphericalKm(50m, 9m, lat, lon)).Returns(km);
    private void SetupCalculate(decimal lat, decimal lon, decimal km) => _calculator.Setup(c => c.CalculateKm(50m, 9m, lat, lon)).Returns(km);

    [TestMethod]
    public void Evaluate_FarCandidate_SkipsCalculateKm()
    {
        var parameters = Parameters();
        SetupSpherical(50.1m, 9.1m, 500m);
        var result = _subject.Evaluate(parameters, 50.1m, 9.1m, 350m);

        result.Match.Should().Be(SearchAreaMatch.Outside);
        result.DistanceKm.Should().BeNull();
        result.AltitudeDifferenceM.Should().Be(50m);
        _calculator.Verify(c => c.CalculateKm(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Never());
        _calculator.Verify(c => c.SphericalKm(50m, 9m, 50.1m, 9.1m), Times.Once());
    }

    [TestMethod]
    public void Evaluate_NearCandidate_CallsEachMethodOnce()
    {
        var parameters = Parameters();
        SetupSpherical(50.2m, 9.2m, 50m);
        SetupCalculate(50.2m, 9.2m, 50.2m);
        var result = _subject.Evaluate(parameters, 50.2m, 9.2m, 350m);

        result.Match.Should().Be(SearchAreaMatch.Inside);
        result.DistanceKm.Should().Be(50.2m);
        _calculator.Verify(c => c.SphericalKm(50m, 9m, 50.2m, 9.2m), Times.Once());
        _calculator.Verify(c => c.CalculateKm(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Once());
    }

    [TestMethod]
    public void Evaluate_PrefilterThresholdBoundary()
    {
        // Candidate A: spherical exactly 102m, calculate 101m → Outside
        var parameters = Parameters();
        SetupSpherical(50.1m, 9.1m, 102m);
        SetupCalculate(50.1m, 9.1m, 101m);
        var resultA = _subject.Evaluate(parameters, 50.1m, 9.1m, 300m);

        resultA.Match.Should().Be(SearchAreaMatch.Outside);
        resultA.DistanceKm.Should().Be(101m);
        _calculator.Verify(c => c.SphericalKm(50m, 9m, 50.1m, 9.1m), Times.Once());
        _calculator.Verify(c => c.CalculateKm(50m, 9m, 50.1m, 9.1m), Times.Once());

        // Candidate B: spherical 102.0001m → Outside, no CalculateKm
        SetupSpherical(50.2m, 9.2m, 102.0001m);
        var resultB = _subject.Evaluate(parameters, 50.2m, 9.2m, 300m);

        resultB.Match.Should().Be(SearchAreaMatch.Outside);
        resultB.DistanceKm.Should().BeNull();
        _calculator.Verify(c => c.SphericalKm(50m, 9m, 50.2m, 9.2m), Times.Once());
        _calculator.Verify(c => c.CalculateKm(50m, 9m, 50.2m, 9.2m), Times.Never());
    }

    [TestMethod]
    public void Evaluate_HugeMaxDistance_NoOverflow()
    {
        var parameters = Parameters(maxDistanceKm: decimal.MaxValue);
        SetupSpherical(50.1m, 9.1m, 20000m);
        SetupCalculate(50.1m, 9.1m, 20000m);
        var result = _subject.Evaluate(parameters, 50.1m, 9.1m, 350m);

        result.Match.Should().Be(SearchAreaMatch.Inside);
        result.DistanceKm.Should().Be(20000m);
    }

    [TestMethod]
    public void Evaluate_DistanceBelowEqualAboveMax()
    {
        var parameters = Parameters();
        SetupSpherical(50.1m, 9.1m, 100m);
        SetupCalculate(50.1m, 9.1m, 99.999999m);
        var result1 = _subject.Evaluate(parameters, 50.1m, 9.1m, 300m);

        result1.Match.Should().Be(SearchAreaMatch.Inside);
        result1.DistanceKm.Should().Be(99.999999m);

        SetupSpherical(50.2m, 9.2m, 100m);
        SetupCalculate(50.2m, 9.2m, 100m);
        var result2 = _subject.Evaluate(parameters, 50.2m, 9.2m, 300m);

        result2.Match.Should().Be(SearchAreaMatch.Inside);
        result2.DistanceKm.Should().Be(100m);

        SetupSpherical(50.3m, 9.3m, 100m);
        SetupCalculate(50.3m, 9.3m, 100.000001m);
        var result3 = _subject.Evaluate(parameters, 50.3m, 9.3m, 300m);

        result3.Match.Should().Be(SearchAreaMatch.Outside);
        result3.DistanceKm.Should().Be(100.000001m);
    }

    [TestMethod]
    public void Evaluate_DistanceDecimalsThatCollapseToSameDouble_AreDistinguished()
    {
        var parameters = Parameters();
        SetupSpherical(50.1m, 9.1m, 100m);
        SetupCalculate(50.1m, 9.1m, 100m);
        var result1 = _subject.Evaluate(parameters, 50.1m, 9.1m, 300m);

        result1.Match.Should().Be(SearchAreaMatch.Inside);
        result1.DistanceKm.Should().Be(100m);

        // Assert that these two values are equal as double
        ((double)100m).Should().Be((double)100.000000000000000000000001m);

        SetupSpherical(50.2m, 9.2m, 100m);
        SetupCalculate(50.2m, 9.2m, 100.000000000000000000000001m);
        var result2 = _subject.Evaluate(parameters, 50.2m, 9.2m, 300m);

        result2.Match.Should().Be(SearchAreaMatch.Outside);
        result2.DistanceKm.Should().Be(100.000000000000000000000001m);
    }

    [TestMethod]
    public void Evaluate_AltitudeDifferenceBelowEqualAboveMax_BothSigns()
    {
        var parameters = Parameters(altitudeM: 300m, maxAltitudeDifferenceM: 150m);
        SetupSpherical(50.1m, 9.1m, 10m);
        SetupCalculate(50.1m, 9.1m, 10m);
        var result1 = _subject.Evaluate(parameters, 50.1m, 9.1m, 449.999m);

        result1.Match.Should().Be(SearchAreaMatch.Inside);
        result1.AltitudeDifferenceM.Should().Be(149.999m);

        SetupSpherical(50.2m, 9.2m, 10m);
        SetupCalculate(50.2m, 9.2m, 10m);
        var result2 = _subject.Evaluate(parameters, 50.2m, 9.2m, 450m);

        result2.Match.Should().Be(SearchAreaMatch.Inside);
        result2.AltitudeDifferenceM.Should().Be(150m);

        SetupSpherical(50.3m, 9.3m, 10m);
        SetupCalculate(50.3m, 9.3m, 10m);
        var result3 = _subject.Evaluate(parameters, 50.3m, 9.3m, 450.001m);

        result3.Match.Should().Be(SearchAreaMatch.Outside);
        result3.AltitudeDifferenceM.Should().Be(150.001m);

        SetupSpherical(50.4m, 9.4m, 10m);
        SetupCalculate(50.4m, 9.4m, 10m);
        var result4 = _subject.Evaluate(parameters, 50.4m, 9.4m, 150.001m);

        result4.Match.Should().Be(SearchAreaMatch.Inside);
        result4.AltitudeDifferenceM.Should().Be(-149.999m);

        SetupSpherical(50.5m, 9.5m, 10m);
        SetupCalculate(50.5m, 9.5m, 10m);
        var result5 = _subject.Evaluate(parameters, 50.5m, 9.5m, 150m);

        result5.Match.Should().Be(SearchAreaMatch.Inside);
        result5.AltitudeDifferenceM.Should().Be(-150m);

        SetupSpherical(50.6m, 9.6m, 10m);
        SetupCalculate(50.6m, 9.6m, 10m);
        var result6 = _subject.Evaluate(parameters, 50.6m, 9.6m, 149.999m);

        result6.Match.Should().Be(SearchAreaMatch.Outside);
        result6.AltitudeDifferenceM.Should().Be(-150.001m);
    }

    [TestMethod]
    public void Evaluate_AltitudeDecimalsThatCollapseToSameDouble_AreDistinguished()
    {
        var parameters = Parameters(altitudeM: 0m, maxAltitudeDifferenceM: 1m);
        SetupSpherical(50.1m, 9.1m, 10m);
        SetupCalculate(50.1m, 9.1m, 10m);
        var result1 = _subject.Evaluate(parameters, 50.1m, 9.1m, 1.000000000000000000000000001m);

        result1.Match.Should().Be(SearchAreaMatch.Outside);
        result1.AltitudeDifferenceM.Should().Be(1.000000000000000000000000001m);

        SetupSpherical(50.2m, 9.2m, 10m);
        SetupCalculate(50.2m, 9.2m, 10m);
        var result2 = _subject.Evaluate(parameters, 50.2m, 9.2m, -1.000000000000000000000000001m);

        result2.Match.Should().Be(SearchAreaMatch.Outside);
        result2.AltitudeDifferenceM.Should().Be(-1.000000000000000000000000001m);

        SetupSpherical(50.3m, 9.3m, 10m);
        SetupCalculate(50.3m, 9.3m, 10m);
        var result3 = _subject.Evaluate(parameters, 50.3m, 9.3m, 1m);

        result3.Match.Should().Be(SearchAreaMatch.Inside);
        result3.AltitudeDifferenceM.Should().Be(1m);

        SetupSpherical(50.4m, 9.4m, 10m);
        SetupCalculate(50.4m, 9.4m, 10m);
        var result4 = _subject.Evaluate(parameters, 50.4m, 9.4m, -1m);

        result4.Match.Should().Be(SearchAreaMatch.Inside);
        result4.AltitudeDifferenceM.Should().Be(-1m);
    }

    [TestMethod]
    public void Evaluate_AltitudeFilteredSeparately()
    {
        var parameters = Parameters();
        SetupSpherical(50.1m, 9.1m, 10m);
        SetupCalculate(50.1m, 9.1m, 10m);
        var resultA = _subject.Evaluate(parameters, 50.1m, 9.1m, 451m);

        resultA.Match.Should().Be(SearchAreaMatch.Outside);
        resultA.DistanceKm.Should().Be(10m);

        SetupSpherical(50.2m, 9.2m, 100m);
        SetupCalculate(50.2m, 9.2m, 101m);
        var resultB = _subject.Evaluate(parameters, 50.2m, 9.2m, 300m);

        resultB.Match.Should().Be(SearchAreaMatch.Outside);
        resultB.DistanceKm.Should().Be(101m);
    }

    [TestMethod]
    public void Evaluate_ReturnsSignedAltitudeDifference()
    {
        var parameters = Parameters(maxAltitudeDifferenceM: 500m);
        SetupSpherical(50.1m, 9.1m, 10m);
        SetupCalculate(50.1m, 9.1m, 10m);
        var result = _subject.Evaluate(parameters, 50.1m, 9.1m, 120m);

        result.Match.Should().Be(SearchAreaMatch.Inside);
        result.AltitudeDifferenceM.Should().Be(-180m);
    }

    [TestMethod]
    public void Evaluate_NegativeReferenceAltitude()
    {
        var parameters = Parameters(altitudeM: -5m, maxAltitudeDifferenceM: 15m);
        SetupSpherical(50.1m, 9.1m, 10m);
        SetupCalculate(50.1m, 9.1m, 10m);
        var result = _subject.Evaluate(parameters, 50.1m, 9.1m, 10m);

        result.Match.Should().Be(SearchAreaMatch.Inside);
        result.AltitudeDifferenceM.Should().Be(15m);
    }

    [TestMethod]
    public void Evaluate_ZeroLimits()
    {
        var parameters = Parameters(maxDistanceKm: 0m, maxAltitudeDifferenceM: 0m);
        SetupSpherical(50.1m, 9.1m, 0m);
        SetupCalculate(50.1m, 9.1m, 0m);
        var resultA = _subject.Evaluate(parameters, 50.1m, 9.1m, 300m);

        resultA.Match.Should().Be(SearchAreaMatch.Inside);
        resultA.DistanceKm.Should().Be(0m);

        SetupSpherical(50.2m, 9.2m, 0m);
        SetupCalculate(50.2m, 9.2m, 0.000001m);
        var resultB = _subject.Evaluate(parameters, 50.2m, 9.2m, 300m);

        resultB.Match.Should().Be(SearchAreaMatch.Outside);
        resultB.DistanceKm.Should().Be(0.000001m);

        SetupSpherical(50.3m, 9.3m, 0m);
        SetupCalculate(50.3m, 9.3m, 0m);
        var resultC = _subject.Evaluate(parameters, 50.3m, 9.3m, 300.001m);

        resultC.Match.Should().Be(SearchAreaMatch.Outside);
        resultC.DistanceKm.Should().Be(0m);
    }

    [TestMethod]
    public void Evaluate_MissingCandidateAltitude_IsNotFilterable()
    {
        var parameters = Parameters(maxAltitudeDifferenceM: decimal.MaxValue);
        var result = _subject.Evaluate(parameters, 50.1m, 9.1m, null);

        result.Match.Should().Be(SearchAreaMatch.NotFilterable);
        result.DistanceKm.Should().BeNull();
        result.AltitudeDifferenceM.Should().BeNull();
    }

    [TestMethod]
    public void Evaluate_MissingAltitude_DoesNotCallCalculator()
    {
        var parameters = Parameters();
        var result = _subject.Evaluate(parameters, 50.1m, 9.1m, null);

        result.Match.Should().Be(SearchAreaMatch.NotFilterable);
        _calculator.Verify(c => c.SphericalKm(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Never());
        _calculator.Verify(c => c.CalculateKm(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Never());
    }

    [TestMethod]
    public void Evaluate_NullParameters_Throws()
    {
        Action action = () => _subject.Evaluate(null!, 50m, 9m, 300m);

        action.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesSearchAreaFilter()
    {
        using var provider = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var result = provider.GetRequiredService<ISearchAreaFilter>();

        result.Should().BeOfType<SearchAreaFilter>();
    }
}
