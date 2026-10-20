using FluentAssertions;
using Moq;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Quantities;

[TestClass]
public sealed class PressureConverterTests
{
    private MockRepository _repository = null!;
    private IPressureConverter _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new MockRepository(MockBehavior.Strict);
        _subject = new PressureConverter(new OutputRounding());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _repository.VerifyAll();
    }

    private const decimal A = 0.7500617m;
    private const decimal B = 1.3332239m;
    private const decimal D = 0.00000018491463m;

    [TestMethod]
    public void Convert_FromHpa_DerivesBothOutputsFromUnroundedOriginal()
    {
        var result = _subject.FromHpa(1000.5m, PressureReference.SeaLevel);

        result.Hpa.Should().Be(1001m);
        result.MmHg.Should().Be(750m);
        result.OriginalUnit.Should().Be(PressureUnit.Hectopascal);
    }

    [TestMethod]
    public void Convert_FromHpa_RoundsTheUnroundedMmHgValue()
    {
        var rounding = _repository.Create<IOutputRounding>();
        rounding.Setup(r => r.Round(QuantityKind.Pressure, 1000.5m)).Returns(1001m);
        rounding.Setup(r => r.Round(QuantityKind.Pressure, 750.43673085m)).Returns(750m);

        var subject = new PressureConverter(rounding.Object);
        var result = subject.FromHpa(1000.5m, PressureReference.Sensor);

        result.Hpa.Should().Be(1001m);
        result.MmHg.Should().Be(750m);
    }

    [TestMethod]
    public void Convert_FromMmHg_UsesReverseFactor()
    {
        var result = _subject.FromMmHg(760m, PressureReference.Unknown);

        result.Hpa.Should().Be(1013m);
        result.MmHg.Should().Be(760m);
        result.OriginalUnit.Should().Be(PressureUnit.MillimetreOfMercury);
    }

    [TestMethod]
    [DataRow(PressureReference.Sensor)]
    [DataRow(PressureReference.SeaLevel)]
    [DataRow(PressureReference.Unknown)]
    public void Convert_PreservesPressureReference(PressureReference reference)
    {
        var result1 = _subject.FromHpa(1013.2m, reference);
        var result2 = _subject.FromMmHg(760m, reference);

        result1.Reference.Should().Be(reference);
        result2.Reference.Should().Be(reference);
    }

    [TestMethod]
    public void Convert_UndefinedReference_ThrowsArgumentOutOfRangeException()
    {
        Action action1 = () => _subject.FromHpa(1000m, (PressureReference)99);
        Action action2 = () => _subject.FromMmHg(1000m, (PressureReference)99);

        action1.Should().Throw<ArgumentOutOfRangeException>();
        action2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Constructor_NullRounding_ThrowsArgumentNullException()
    {
        Action action = () => new PressureConverter(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Convert_FromHpa_RoundedOutputsStayWithinOriginBounds()
    {
        for (var h = 800.0m; h <= 1100.0m; h += 0.1m)
        {
            var r = _subject.FromHpa(h, PressureReference.Sensor);
            r.Hpa.Should().BeApproximately(h, 0.5m, because: $"h={h}");
            r.MmHg.Should().BeApproximately(h * A, 0.5m, because: $"h={h}");
            Math.Abs(r.Hpa * A - r.MmHg).Should().BeLessThanOrEqualTo(0.87503085m, because: $"h={h}");
            Math.Abs(r.MmHg * B - r.Hpa).Should().BeLessThanOrEqualTo(0.5m * B + 0.5m + Math.Abs(h) * D, because: $"h={h}");
        }
    }

    [TestMethod]
    public void Convert_FromMmHg_RoundedOutputsStayWithinOriginBounds()
    {
        for (var m = 600.0m; m <= 825.0m; m += 0.1m)
        {
            var r = _subject.FromMmHg(m, PressureReference.Sensor);
            r.MmHg.Should().BeApproximately(m, 0.5m, because: $"m={m}");
            r.Hpa.Should().BeApproximately(m * B, 0.5m, because: $"m={m}");
            Math.Abs(r.MmHg * B - r.Hpa).Should().BeLessThanOrEqualTo(1.16661195m, because: $"m={m}");
            Math.Abs(r.Hpa * A - r.MmHg).Should().BeLessThanOrEqualTo(0.5m * A + 0.5m + Math.Abs(m) * D, because: $"m={m}");
        }
    }

    [TestMethod]
    public void Convert_FromMmHg_LargeValue_ShowsFactorDiscrepancyWithinBound()
    {
        var r = _subject.FromMmHg(100000000m, PressureReference.Unknown);

        r.Hpa.Should().Be(133322390m);
        r.MmHg.Should().Be(100000000m);
        (r.Hpa * A - r.MmHg).Should().Be(18.491463m);
        Math.Abs(r.Hpa * A - r.MmHg).Should().BeLessThanOrEqualTo(0.5m * A + 0.5m + 100000000m * D, because: "large value test");
    }
}
