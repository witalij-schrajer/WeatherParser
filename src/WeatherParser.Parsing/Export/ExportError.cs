using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents one typed error record of a partial result; it is never an observation or forecast.
/// </summary>
[Serializable]
public sealed class ExportError
{
    private static readonly Regex ErrorIdPattern = new(@"\A[A-Z][A-Z0-9]*(-[A-Z0-9]+)*\z", RegexOptions.CultureInvariant);
    private static readonly Regex ProviderPattern = new(@"\A[a-z][a-z0-9-]*\z", RegexOptions.CultureInvariant);

    private RequiredMember<string> _errorId;

    /// <summary>
    /// Gets or sets the error identifier.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("error_id")]
    [JsonPropertyName("error_id")]
    public string ErrorId
    {
        get => _errorId.Get("error_id");
        set
        {
            if (value is null)
            {
                throw new ArgumentException("error_id must not be null (contract C-86).", nameof(ErrorId));
            }
            if (!ErrorIdPattern.IsMatch(value))
            {
                throw new ArgumentException($"error_id '{value}' must match [A-Z][A-Z0-9]*(-[A-Z0-9]+)* (contract C-86).", nameof(ErrorId));
            }
            _errorId = RequiredMember<string>.Of(value);
        }
    }

    private RequiredMember<string> _message;

    /// <summary>
    /// Gets or sets the error message.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("message")]
    [JsonPropertyName("message")]
    public string Message
    {
        get => _message.Get("message");
        set
        {
            if (value is null)
            {
                throw new ArgumentException("message must not be null (contract C-86).", nameof(Message));
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("message must not be empty or whitespace-only (contract C-86).", nameof(Message));
            }
            if (value.Contains('\r') || value.Contains('\n'))
            {
                throw new ArgumentException("message must be a single line without CR or LF (contract C-86).", nameof(Message));
            }
            _message = RequiredMember<string>.Of(value);
        }
    }

    private RequiredMember<string> _source;

    /// <summary>
    /// Gets or sets the source of the error.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("source")]
    [JsonPropertyName("source")]
    public string Source
    {
        get => _source.Get("source");
        set
        {
            if (value is null)
            {
                throw new ArgumentException("source must not be null (contract C-86).", nameof(Source));
            }
            var separator = value.IndexOf(':');
            if (separator <= 0)
            {
                throw new ArgumentException($"source '{value}' must have the form <provider>:<id> (contract C-86).", nameof(Source));
            }
            var provider = value[..separator];
            var station = value[(separator + 1)..];
            if (!ProviderPattern.IsMatch(provider))
            {
                throw new ArgumentException($"source '{value}' must start with a provider matching [a-z][a-z0-9-]* (contract C-86).", nameof(Source));
            }
            if (string.IsNullOrWhiteSpace(station))
            {
                throw new ArgumentException($"source '{value}' must have a non-empty id after the first ':' (contract C-86).", nameof(Source));
            }
            _source = RequiredMember<string>.Of(value);
        }
    }
}
