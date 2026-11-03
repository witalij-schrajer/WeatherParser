namespace WeatherParser.Cli.Tests.Arguments;

using FluentAssertions;
using System.Globalization;
using WeatherParser.Cli.Arguments;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;

[TestClass]
[TestCategory("T-P3-03")]
/// <summary>
/// Verifies the value validation of the example CLI through the library services (CL-03, T-P3-03).
/// </summary>
public class CommandRequestValidatorTests
{
    private CommandRequestValidator _validator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _validator = new CommandRequestValidator(new CoordinateParser(), new SearchParameterFactory());
    }

    private static ParsedCommandLine Parsed(CliCommand command, params (string Name, string Value)[] values)
    {
        return new ParsedCommandLine(command, values.ToDictionary(v => v.Name, v => v.Value), new HashSet<string>(StringComparer.Ordinal));
    }

    private static ParsedCommandLine Parsed(CliCommand command, IReadOnlyCollection<string> flags, params (string Name, string Value)[] values)
    {
        return new ParsedCommandLine(command, values.ToDictionary(v => v.Name, v => v.Value), new HashSet<string>(flags, StringComparer.Ordinal));
    }

    private static CommandRequest ValidOk(CommandLineResult result)
    {
        result.Should().BeOfType<ValidatedCommand>();
        return ((ValidatedCommand)result).Request;
    }

    private static UsageError ValidError(CommandLineResult result)
    {
        result.Should().BeOfType<UsageError>();
        return (UsageError)result;
    }

    /// <summary>
    /// Verifies that decimal coordinates are accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_DecimalCoordinates_Accepted()
    {
        var parsed = Parsed(CliCommand.SelectDwd,
            ("Latitude", "50.5549"),
            ("Longitude", "9.6738"),
            ("Altitude", "300"),
            ("MaxDistance", "50"),
            ("MaxAltDifference", "200"));

        var result = _validator.Validate(parsed);
        var request = ValidOk(result);

        request.Should().BeOfType<SelectDwdRequest>();
        var selectRequest = (SelectDwdRequest)request;
        selectRequest.Search.Latitude.Should().Be(50.5549m);
        selectRequest.Search.Longitude.Should().Be(9.6738m);
        selectRequest.Search.AltitudeM.Should().Be(300m);
        selectRequest.Search.MaxDistanceKm.Should().Be(50m);
        selectRequest.Search.MaxAltitudeDifferenceM.Should().Be(200m);
    }

    /// <summary>
    /// Verifies that quoted DMS coordinates are accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_QuotedDmsCoordinates_Accepted()
    {
        var parsed = Parsed(CliCommand.SelectDwd,
            ("Latitude", "50\u00B0 33\u2032 18\u2033 N"),
            ("Longitude", "9\u00B0 40\u2032 26\u2033 E"),
            ("Altitude", "300"),
            ("MaxDistance", "50"),
            ("MaxAltDifference", "200"));

        var result = _validator.Validate(parsed);
        var request = ValidOk(result);

        request.Should().BeOfType<SelectDwdRequest>();
        var selectRequest = (SelectDwdRequest)request;
        selectRequest.Search.Latitude.Should().Be(50.555m);
        selectRequest.Search.Longitude.Should().BeApproximately(9.673889m, 0.000001m);
    }

    /// <summary>
    /// Verifies that out-of-range coordinates result in a usage error with the library error ID.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_CoordinateOutOfRange_UsageErrorWithLibraryId()
    {
        var parsed = Parsed(CliCommand.SelectDwd,
            ("Latitude", "91"),
            ("Longitude", "9.6738"),
            ("Altitude", "300"),
            ("MaxDistance", "50"),
            ("MaxAltDifference", "200"));

        var result = _validator.Validate(parsed);
        var error = ValidError(result);

        error.ErrorId.Should().Be("COORDINATE-INVALID");
    }

    /// <summary>
    /// Verifies that wrong hemisphere coordinates result in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_WrongHemisphere_UsageError()
    {
        var parsed = Parsed(CliCommand.SelectDwd,
            ("Latitude", "50\u00B0 33\u2032 18\u2033 E"),
            ("Longitude", "9.6738"),
            ("Altitude", "300"),
            ("MaxDistance", "50"),
            ("MaxAltDifference", "200"));

        var result = _validator.Validate(parsed);
        var error = ValidError(result);

        error.ErrorId.Should().Be("COORDINATE-INVALID");
    }

    /// <summary>
    /// Verifies that under German culture the same result is obtained.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_UnderGermanCulture_SameResult()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

            var parsed = Parsed(CliCommand.SelectDwd,
                ("Latitude", "50.5549"),
                ("Longitude", "9.6738"),
                ("Altitude", "300.5"),
                ("MaxDistance", "50"),
                ("MaxAltDifference", "200"));

            var result = _validator.Validate(parsed);
            var request = ValidOk(result);

            request.Should().BeOfType<SelectDwdRequest>();
            var selectRequest = (SelectDwdRequest)request;
            selectRequest.Search.AltitudeM.Should().Be(300.5m);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    /// <summary>
    /// Verifies that comma decimal values result in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_CommaDecimal_UsageError()
    {
        var parsed = Parsed(CliCommand.SelectDwd,
            ("Latitude", "50.5549"),
            ("Longitude", "9.6738"),
            ("Altitude", "300,5"),
            ("MaxDistance", "50"),
            ("MaxAltDifference", "200"));

        var result = _validator.Validate(parsed);
        var error = ValidError(result);

        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--Altitude' requires a decimal number such as 300 or -12.5.");
    }

    /// <summary>
    /// Verifies that non-numeric distance values result in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_NonNumericDistance_UsageError()
    {
        var parsed = Parsed(CliCommand.SelectDwd,
            ("Latitude", "50.5549"),
            ("Longitude", "9.6738"),
            ("Altitude", "300"),
            ("MaxDistance", "fifty"),
            ("MaxAltDifference", "200"));

        var result = _validator.Validate(parsed);
        var error = ValidError(result);

        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--MaxDistance' requires a decimal number such as 300 or -12.5.");
    }

    /// <summary>
    /// Verifies that negative altitude values are accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_NegativeAltitude_Accepted()
    {
        var parsed = Parsed(CliCommand.SelectDwd,
            ("Latitude", "50.5549"),
            ("Longitude", "9.6738"),
            ("Altitude", "-12.5"),
            ("MaxDistance", "50"),
            ("MaxAltDifference", "200"));

        var result = _validator.Validate(parsed);
        var request = ValidOk(result);

        request.Should().BeOfType<SelectDwdRequest>();
        var selectRequest = (SelectDwdRequest)request;
        selectRequest.Search.AltitudeM.Should().Be(-12.5m);
    }

    /// <summary>
    /// Verifies that zero distance and altitude difference values are accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_ZeroDistanceAndAltitudeDifference_Accepted()
    {
        var parsed = Parsed(CliCommand.SelectDwd,
            ("Latitude", "50.5549"),
            ("Longitude", "9.6738"),
            ("Altitude", "300"),
            ("MaxDistance", "0"),
            ("MaxAltDifference", "0"));

        var result = _validator.Validate(parsed);
        var request = ValidOk(result);

        request.Should().BeOfType<SelectDwdRequest>();
        var selectRequest = (SelectDwdRequest)request;
        selectRequest.Search.MaxDistanceKm.Should().Be(0m);
        selectRequest.Search.MaxAltitudeDifferenceM.Should().Be(0m);
    }

    /// <summary>
    /// Verifies that negative distance values result in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_NegativeDistance_UsageError()
    {
        foreach (var name in new[] { "MaxDistance", "MaxAltDifference" })
        {
            var underflow = Parsed(CliCommand.SelectDwd,
                ("Latitude", "50.5549"),
                ("Longitude", "9.6738"),
                ("Altitude", "300"),
                ("MaxDistance", name == "MaxDistance" ? "-0.00000000000000000000000000001" : "50"),
                ("MaxAltDifference", name == "MaxAltDifference" ? "-0.00000000000000000000000000001" : "200"));
            var underflowError = ValidError(_validator.Validate(underflow));
            underflowError.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
            underflowError.Message.Should().Be($"Option '--{name}' has more digits than a decimal number holds exactly.");
        }

        var parsed = Parsed(CliCommand.SelectDwd,
            ("Latitude", "50.5549"),
            ("Longitude", "9.6738"),
            ("Altitude", "300"),
            ("MaxDistance", "-1"),
            ("MaxAltDifference", "200"));

        var result = _validator.Validate(parsed);
        var error = ValidError(result);

        error.ErrorId.Should().Be("SEARCH-PARAMETER-INVALID");
    }

    /// <summary>
    /// Verifies that the refresh flag for MOSMIX is handled correctly.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_SelectMosmix_RefreshFlag()
    {
        // With flag
        var parsedWithFlag = Parsed(CliCommand.SelectMosmix, new HashSet<string>(StringComparer.Ordinal) { "RefreshMosmixSEvidence" },
            ("Latitude", "50.5549"),
            ("Longitude", "9.6738"),
            ("Altitude", "300"),
            ("MaxDistance", "50"),
            ("MaxAltDifference", "200"));

        var resultWithFlag = _validator.Validate(parsedWithFlag);
        var requestWithFlag = ValidOk(resultWithFlag);

        requestWithFlag.Should().BeOfType<SelectMosmixRequest>();
        var selectMosmixRequestWithFlag = (SelectMosmixRequest)requestWithFlag;
        selectMosmixRequestWithFlag.RefreshMosmixSEvidence.Should().BeTrue();

        // Without flag
        var parsedWithoutFlag = Parsed(CliCommand.SelectMosmix,
            ("Latitude", "50.5549"),
            ("Longitude", "9.6738"),
            ("Altitude", "300"),
            ("MaxDistance", "50"),
            ("MaxAltDifference", "200"));

        var resultWithoutFlag = _validator.Validate(parsedWithoutFlag);
        var requestWithoutFlag = ValidOk(resultWithoutFlag);

        requestWithoutFlag.Should().BeOfType<SelectMosmixRequest>();
        var selectMosmixRequestWithoutFlag = (SelectMosmixRequest)requestWithoutFlag;
        selectMosmixRequestWithoutFlag.RefreshMosmixSEvidence.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that product values are mapped to library product.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_ProductValues_MappedToLibraryProduct()
    {
        // MOSMIX_S
        var parsedS = Parsed(CliCommand.ParseMosmix,
            ("Product", "mosmix_s"),
            ("MOSMIXID", "10637"));

        var resultS = _validator.Validate(parsedS);
        var requestS = ValidOk(resultS);

        requestS.Should().BeOfType<ParseMosmixRequest>();
        var parseMosmixRequestS = (ParseMosmixRequest)requestS;
        parseMosmixRequestS.Product.Should().Be(MosmixProduct.MosmixS);

        // MOSMIX_L
        var parsedL = Parsed(CliCommand.ParseMosmix,
            ("Product", "MOSMIX_L"),
            ("MOSMIXID", "10637"));

        var resultL = _validator.Validate(parsedL);
        var requestL = ValidOk(resultL);

        requestL.Should().BeOfType<ParseMosmixRequest>();
        var parseMosmixRequestL = (ParseMosmixRequest)requestL;
        parseMosmixRequestL.Product.Should().Be(MosmixProduct.MosmixL);
    }

    /// <summary>
    /// Verifies that IDs and patterns are passed unchanged.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_IdsAndPattern_PassedUnchanged()
    {
        // ParseDwd with StationId
        var parsedDwdWithId = Parsed(CliCommand.ParseDwd,
            ("StationID", "00044"));

        var resultDwdWithId = _validator.Validate(parsedDwdWithId);
        var requestDwdWithId = ValidOk(resultDwdWithId);

        requestDwdWithId.Should().BeOfType<ParseDwdRequest>();
        var parseDwdRequestWithId = (ParseDwdRequest)requestDwdWithId;
        parseDwdRequestWithId.StationId.Should().Be("00044");

        // ParseDwd without StationId
        var parsedDwdWithoutId = Parsed(CliCommand.ParseDwd);

        var resultDwdWithoutId = _validator.Validate(parsedDwdWithoutId);
        var requestDwdWithoutId = ValidOk(resultDwdWithoutId);

        requestDwdWithoutId.Should().BeOfType<ParseDwdRequest>();
        var parseDwdRequestWithoutId = (ParseDwdRequest)requestDwdWithoutId;
        parseDwdRequestWithoutId.StationId.Should().BeNull();

        // ParseOstHessen with pattern
        var parsedOstHessen = Parsed(CliCommand.ParseOstHessen,
            ("StationNamePattern", "Gro\u00DF*"));

        var resultOstHessen = _validator.Validate(parsedOstHessen);
        var requestOstHessen = ValidOk(resultOstHessen);

        requestOstHessen.Should().BeOfType<ParseOstHessenRequest>();
        var parseOstHessenRequest = (ParseOstHessenRequest)requestOstHessen;
        parseOstHessenRequest.StationNamePattern.Should().Be("Gro\u00DF*");

        // ParseMosmix with MosmixId
        var parsedMosmix = Parsed(CliCommand.ParseMosmix,
            ("Product", "mosmix_s"),
            ("MOSMIXID", "0A12"));

        var resultMosmix = _validator.Validate(parsedMosmix);
        var requestMosmix = ValidOk(resultMosmix);

        requestMosmix.Should().BeOfType<ParseMosmixRequest>();
        var parseMosmixRequest = (ParseMosmixRequest)requestMosmix;
        parseMosmixRequest.MosmixId.Should().Be("0A12");
    }

    /// <summary>
    /// Verifies that common options have correct defaults.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_CommonOptions_Defaults()
    {
        var parsed = Parsed(CliCommand.ParseDwd);

        var result = _validator.Validate(parsed);
        var request = ValidOk(result);

        request.Should().BeOfType<ParseDwdRequest>();
        var parseDwdRequest = (ParseDwdRequest)request;
        parseDwdRequest.Common.Should().Be(new CommonOptions(OutputFormat.Mqtt, null, null, false));
    }

    /// <summary>
    /// Verifies that common options are correctly given.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_CommonOptions_Given()
    {
        var parsed = Parsed(CliCommand.ParseDwd, new[] { "Verbose" },
            ("Output", "XML"),
            ("OutputFile", "out.xml"),
            ("Config", "c.json"));

        var result = _validator.Validate(parsed);
        var request = ValidOk(result);

        request.Should().BeOfType<ParseDwdRequest>();
        var parseDwdRequest = (ParseDwdRequest)request;
        parseDwdRequest.Common.Should().Be(new CommonOptions(OutputFormat.Xml, "out.xml", "c.json", true));
    }

    /// <summary>
    /// Verifies that long exact decimals are accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_LongExactDecimals_Accepted()
    {
        var request = (SelectDwdRequest)ValidOk(_validator.Validate(Parsed(CliCommand.SelectDwd,
            ("Latitude", "50.5549"), ("Longitude", "9.6738"), ("Altitude", "300.000000000000000000000"),
            ("MaxDistance", "0.000000000000000000001"), ("MaxAltDifference", "200"))));
        request.Search.AltitudeM.Should().Be(300m);
        request.Search.MaxDistanceKm.Should().Be(0.000000000000000000001m);
    }

    /// <summary>
    /// Verifies that negative zero is accepted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_NegativeZero_Accepted()
    {
        var request = (SelectDwdRequest)ValidOk(_validator.Validate(Parsed(CliCommand.SelectDwd,
            ("Latitude", "50.5549"), ("Longitude", "9.6738"), ("Altitude", "-0"),
            ("MaxDistance", "50"), ("MaxAltDifference", "200"))));
        request.Search.AltitudeM.Should().Be(0m);
    }

    /// <summary>
    /// Verifies that precision loss results in a usage error.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-03")]
    public void Validate_PrecisionLoss_UsageError()
    {
        var error = ValidError(_validator.Validate(Parsed(CliCommand.SelectDwd,
            ("Latitude", "50.5549"), ("Longitude", "9.6738"), ("Altitude", "300"),
            ("MaxDistance", "123456789012345.00000000000000000001"), ("MaxAltDifference", "200"))));
        error.ErrorId.Should().Be("CLI-ARGUMENT-INVALID");
        error.Message.Should().Be("Option '--MaxDistance' has more digits than a decimal number holds exactly.");
    }
}
