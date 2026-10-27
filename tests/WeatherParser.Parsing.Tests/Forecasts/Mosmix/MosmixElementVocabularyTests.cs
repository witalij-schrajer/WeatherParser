using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public class MosmixElementVocabularyTests
{
    private static readonly string[] DefinedNames = new[]
    {
        "TTT", "Td", "TX", "TN", "DD", "FF", "FX1", "FX3", "FXh", "RR1c", "RR1", "RR3c", "RR3", "RRS1c", "RRS3c", "ww", "W1W2", "N", "Neff", "N05", "Nl", "Nm", "Nh", "PPPP", "QNH", "T5cm", "RadS3", "Rad1h", "RadL3", "VV", "SunD1", "FXh25", "FXh40", "FXh55", "wwM", "wwM6", "wwMh", "Rh00", "R602", "Rh02", "Rd02", "Rh10", "R650", "Rh50", "Rd50", "TG", "TM", "DRR1", "wwZ", "wwD", "wwC", "wwT", "wwL", "wwS", "wwF", "wwP", "VV10", "E_TTT", "E_FF", "E_DD", "E_Td", "RR6", "RR6c", "R600", "R101", "R102", "R103", "R105", "R107", "R110", "R120", "SunD", "RSunD", "PSd00", "PSd30", "PSd60", "RRad1", "PEvap", "R130", "R150", "RR1o1", "RR1w1", "RR1u1", "wwD6", "wwC6", "wwT6", "wwP6", "wwL6", "wwF6", "wwS6", "wwZ6", "wwMd", "FX625", "FX640", "FX655", "wwDh", "wwCh", "wwTh", "wwPh", "wwLh", "wwFh", "wwSh", "wwZh", "R610", "RRh", "RRhc", "ww3", "RRL1c", "Rd00", "Rd10", "RRd", "RRdc", "Nlm", "wwPd", "H_BsC", "wwTd", "E_PPP", "SunD3", "WPc11", "WPc31", "WPc61", "WPch1", "WPcd1", "Sa3", "Sa6", "Sah", "Sad", "Sa605", "Sa610", "Sa620", "Sah05", "Sah10", "Sah30", "Sad10", "Sad30", "Sad50", "SnCv"
    };

    private static readonly string[] MosmixLNames = new[]
    {
        "DD", "DRR1", "E_DD", "E_FF", "E_PPP", "E_TTT", "E_Td", "FF", "FX1", "FX3", "FX625", "FX640", "FX655", "FXh", "FXh25", "FXh40", "FXh55", "N", "N05", "Neff", "Nh", "Nl", "Nlm", "Nm", "PEvap", "PPPP", "PSd00", "PSd30", "PSd60", "R101", "R102", "R103", "R105", "R107", "R110", "R120", "R130", "R150", "R600", "R602", "R610", "R650", "RR1c", "RR1o1", "RR1u1", "RR1w1", "RR3c", "RR6c", "RRL1c", "RRS1c", "RRS3c", "RRad1", "RRdc", "RRhc", "RSunD", "Rad1h", "Rd00", "Rd02", "Rd10", "Rd50", "Rh00", "Rh02", "Rh10", "Rh50", "SunD", "SunD1", "SunD3", "T5cm", "TG", "TM", "TN", "TTT", "TX", "Td", "VV", "VV10", "W1W2", "WPc11", "WPc31", "WPc61", "WPcd1", "WPch1", "ww", "ww3", "wwC", "wwC6", "wwCh", "wwD", "wwD6", "wwDh", "wwF", "wwF6", "wwFh", "wwL", "wwL6", "wwLh", "wwM", "wwM6", "wwMd", "wwMh", "wwP", "wwP6", "wwPd", "wwPh", "wwS", "wwS6", "wwSh", "wwT", "wwT6", "wwTd", "wwTh", "wwZ", "wwZ6", "wwZh"
    };

    private static readonly string[] MosmixSNames = new[]
    {
        "DD", "FF", "FX1", "FX3", "FXh", "FXh25", "FXh40", "FXh55", "N", "N05", "Neff", "Nh", "Nl", "Nm", "PPPP", "R602", "R650", "RR1c", "RR3c", "RRS1c", "RRS3c", "Rad1h", "Rd02", "Rd50", "Rh00", "Rh02", "Rh10", "Rh50", "SunD1", "T5cm", "TN", "TTT", "TX", "Td", "VV", "W1W2", "ww", "wwM", "wwM6", "wwMh"
    };

    [TestMethod]
    public void Vocabulary_ListsHaveRecordedCounts()
    {
        // Arrange
        var defined = DefinedNames.Distinct().Count();
        var mosmixL = MosmixLNames.Distinct().Count();
        var mosmixS = MosmixSNames.Distinct().Count();

        // Act & Assert
        defined.Should().Be(137);
        mosmixL.Should().Be(114);
        mosmixS.Should().Be(40);
    }

    [TestMethod]
    public void Vocabulary_P4NamesAndMappedNames_AllDefined()
    {
        // Arrange
        var mapped = MosmixElementTable.Rows.Select(r => r.ElementName).ToArray();

        // Act & Assert
        MosmixLNames.Should().BeSubsetOf(DefinedNames);
        MosmixSNames.Should().BeSubsetOf(DefinedNames);
        mapped.Should().BeSubsetOf(DefinedNames);
    }

    [TestMethod]
    public void Vocabulary_MappedNamesInProductVocabulary()
    {
        // Arrange
        var mapped = MosmixElementTable.Rows.Select(r => r.ElementName).ToArray();

        // Act & Assert
        mapped.Should().BeSubsetOf(MosmixLNames);
        mapped.Should().BeSubsetOf(MosmixSNames);
    }

    [TestMethod]
    public void ElementTable_MatchesContractTable()
    {
        // Arrange
        var expected = new[]
        {
            new MosmixElementRow("TTT", QuantityKind.Temperature, "K", null, null, null, null),
            new MosmixElementRow("PPPP", QuantityKind.Pressure, "Pa", null, PressureUnit.Hectopascal, PressureReference.Unknown, null),
            new MosmixElementRow("DD", QuantityKind.WindDirection, "0..360 degrees", null, null, null, null),
            new MosmixElementRow("FF", QuantityKind.MeanWindSpeed, "m/s", null, null, null, WindSpeedUnit.MetresPerSecond),
            new MosmixElementRow("FX1", QuantityKind.WindGust, "m/s", 1, null, null, WindSpeedUnit.MetresPerSecond),
            new MosmixElementRow("Rad1h", QuantityKind.RadiationEnergy, "kJ/m2", 1, null, null, null),
            new MosmixElementRow("RR1c", QuantityKind.PrecipitationAccumulation, "kg/m2", 1, null, null, null)
        };

        // Act & Assert
        MosmixElementTable.Rows.Should().Equal(expected);
    }
}
