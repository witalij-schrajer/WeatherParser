using System.Text.Json;
using FluentAssertions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class EnumVocabularyGuardTests
{
    [TestMethod]
    public void Product_UndefinedValue_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => new MosmixProductAvailability().Product = (MosmixProduct)99)
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Availability_UndefinedValue_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => new MosmixProductAvailability().Availability = (AvailabilityState)99)
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Product_DefinedValues_AreAccepted()
    {
        foreach (var value in Enum.GetValues<MosmixProduct>())
        {
            var instance = new MosmixProductAvailability();
            instance.Product = value;
            instance.Product.Should().Be(value);
        }
    }

    [TestMethod]
    public void Availability_DefinedValues_AreAccepted()
    {
        foreach (var value in Enum.GetValues<AvailabilityState>())
        {
            var instance = new MosmixProductAvailability();
            instance.Availability = value;
            instance.Availability.Should().Be(value);
        }
    }

    [TestMethod]
    public void JsonRead_IntegerProduct_IsRejected()
    {
        FluentActions.Invoking(() => JsonSerializer.Deserialize<MosmixProductAvailability>("{\"product\":1,\"availability\":\"available\",\"available_forecast_elements\":[]}"))
            .Should().Throw<JsonException>();
    }

    [TestMethod]
    public void JsonRead_IntegerAvailability_IsRejected()
    {
        FluentActions.Invoking(() => JsonSerializer.Deserialize<MosmixProductAvailability>("{\"product\":\"MOSMIX_L\",\"availability\":0,\"available_forecast_elements\":[]}")
            )
            .Should().Throw<JsonException>();
    }

    [TestMethod]
    public void JsonRead_UnknownProductString_IsRejected()
    {
        FluentActions.Invoking(() => JsonSerializer.Deserialize<MosmixProductAvailability>("{\"product\":\"MOSMIX_X\",\"availability\":\"available\",\"available_forecast_elements\":[]}")
            )
            .Should().Throw<JsonException>();
    }
}
