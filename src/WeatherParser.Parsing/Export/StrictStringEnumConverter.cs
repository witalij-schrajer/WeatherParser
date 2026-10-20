using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// String-only enum converter: integer values are rejected when reading and writing,
/// so JSON uses the fixed string vocabulary of the contract.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
internal sealed class StrictStringEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public StrictStringEnumConverter()
        : base(namingPolicy: null, allowIntegerValues: false)
    {
    }
}
