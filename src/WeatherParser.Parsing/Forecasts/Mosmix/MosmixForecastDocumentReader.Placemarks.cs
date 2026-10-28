using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

public sealed partial class MosmixForecastDocumentReader
{
    private const int MaxTokenLength = 17;
    private static readonly Regex IdPattern = new(@"\A[A-Z0-9]{4,5}\z", RegexOptions.CultureInvariant);
    private static readonly Regex NamePattern = new(@"\A[A-Za-z0-9_]{1,16}\z", RegexOptions.CultureInvariant);
    private static readonly Regex TokenPattern = new(@"\A-?[0-9]{1,9}(\.[0-9]{1,6})?\z", RegexOptions.CultureInvariant);
    private static readonly Regex CoordinatePattern = new(@"\A-?[0-9]{1,5}(\.[0-9]{1,9})?\z", RegexOptions.CultureInvariant);

    private sealed class PlacemarkResult
    {
        public required string Id { get; init; }
        public string? Description { get; init; }
        public string? Coordinates { get; init; }
        public IReadOnlyList<MosmixForecastSeries>? Series { get; init; }
    }

    private PlacemarkResult ReadPlacemark(XmlReader reader, int timeSteps, bool single, string mosmixId)
    {
        string? id = null;
        List<MosmixForecastSeries>? retained = null;
        var nameCount = 0;
        var descriptionCount = 0;
        var extendedDataCount = 0;
        var pointCount = 0;
        var description = default(string?);
        var coordinates = default(string?);
        if (reader.IsEmptyElement)
        {
            reader.Read();
        }
        else
        {
            reader.ReadStartElement();
            while (MoveToChildElement(reader))
            {
                if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "name")
                {
                    nameCount++;
                    id = ReadBoundedText(reader, MaxShortTextLength, "kml:name", trim: true).Trim();
                }
                else if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "description")
                {
                    descriptionCount++;
                    if (descriptionCount > 1)
                    {
                        throw Invalid("kml:description must occur at most once");
                    }

                    description = ReadBoundedText(reader, MaxShortTextLength, "kml:description");
                }
                else if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "ExtendedData")
                {
                    extendedDataCount++;
                    if (extendedDataCount > 1)
                    {
                        throw Invalid("kml:Placemark/kml:ExtendedData must occur at most once");
                    }

                    retained = single || id is null || id == mosmixId ? new List<MosmixForecastSeries>() : null;
                    ReadPlacemarkForecasts(reader, timeSteps, retained);
                }
                else if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "Point")
                {
                    pointCount++;
                    if (pointCount > 1)
                    {
                        throw Invalid("kml:Placemark/kml:Point must occur at most once");
                    }

                    coordinates = ReadPoint(reader);
                }
                else
                {
                    reader.Skip();
                }
            }

            reader.ReadEndElement();
        }

        if (nameCount != 1)
        {
            throw Invalid("kml:name must occur exactly once");
        }

        if (!single && !IdPattern.IsMatch(id!))
        {
            throw Invalid("a placemark ID does not match ^[A-Z0-9]{4,5}$");
        }

        var keep = single || id == mosmixId;
        return new PlacemarkResult
        {
            Id = id!,
            Description = keep ? description : null,
            Coordinates = keep ? coordinates : null,
            Series = keep ? retained : null
        };
    }

    private string? ReadPoint(XmlReader reader)
    {
        var coordinatesCount = 0;
        string? coordinates = null;
        if (reader.IsEmptyElement)
        {
            reader.Read();
        }
        else
        {
            reader.ReadStartElement();
            while (MoveToChildElement(reader))
            {
                if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "coordinates")
                {
                    coordinatesCount++;
                    if (coordinatesCount > 1)
                    {
                        throw Invalid("kml:coordinates must occur exactly once");
                    }

                    coordinates = ReadBoundedText(reader, MaxShortTextLength, "kml:coordinates");
                }
                else
                {
                    reader.Skip();
                }
            }

            reader.ReadEndElement();
        }

        if (coordinatesCount != 1)
        {
            throw Invalid("kml:coordinates must occur exactly once");
        }

        return coordinates;
    }

    private void ReadPlacemarkForecasts(XmlReader reader, int timeSteps, List<MosmixForecastSeries>? retained)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.ReadStartElement();
        var forecasts = 0;
        var names = new HashSet<string>(StringComparer.Ordinal);
        while (MoveToChildElement(reader))
        {
            if (reader.NamespaceURI != DwdNamespace || reader.LocalName != "Forecast")
            {
                reader.Skip();
                continue;
            }

            forecasts++;
            if (forecasts > _limits.MaxForecastsPerPlacemark)
            {
                throw Invalid("a placemark has more than the bounded number of forecasts");
            }

            var name = reader.GetAttribute("elementName", DwdNamespace);
            if (name is null || !NamePattern.IsMatch(name) || !names.Add(name))
            {
                throw Invalid("a forecast element name is missing, invalid or duplicated");
            }

            ReadForecastValue(reader, timeSteps, name, retained);
        }

        reader.ReadEndElement();
    }

    private static void ReadForecastValue(XmlReader reader, int timeSteps, string name, List<MosmixForecastSeries>? retained)
    {
        var values = 0;
        if (reader.IsEmptyElement)
        {
            reader.Read();
        }
        else
        {
            reader.ReadStartElement();
            while (MoveToChildElement(reader))
            {
                if (reader.NamespaceURI != DwdNamespace || reader.LocalName != "value")
                {
                    reader.Skip();
                    continue;
                }

                values++;
                if (values > 1)
                {
                    throw Invalid("dwd:value must occur exactly once");
                }

                var tokens = retained is null ? null : new List<string?>(timeSteps);
                ScanTokens(reader, timeSteps, tokens, name);
                if (retained is not null)
                {
                    retained.Add(new MosmixForecastSeries { ElementName = name, Values = tokens! });
                }
            }

            reader.ReadEndElement();
        }

        if (values != 1)
        {
            throw Invalid("dwd:value must occur exactly once");
        }
    }

    private static void ScanTokens(XmlReader reader, int timeSteps, List<string?>? tokens, string name)
    {
        var token = new StringBuilder(MaxTokenLength);
        var buffer = new char[4096];
        var count = 0;

        void Complete()
        {
            if (token.Length == 0)
            {
                return;
            }

            count++;
            if (count > timeSteps)
            {
                throw Invalid("a series does not have one token per time step");
            }

            var text = token.ToString();
            token.Clear();
            if (text == "-")
            {
                tokens?.Add(null);
            }
            else
            {
                if (!TokenPattern.IsMatch(text))
                {
                    throw Invalid("a series contains an invalid token");
                }

                tokens?.Add(text);
            }
        }

        if (reader.IsEmptyElement)
        {
            reader.Read();
        }
        else
        {
            reader.Read();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType is not (XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace))
                {
                    throw Invalid("dwd:value must contain only text");
                }

                int read;
                while ((read = reader.ReadValueChunk(buffer, 0, buffer.Length)) > 0)
                {
                    for (var i = 0; i < read; i++)
                    {
                        if (char.IsWhiteSpace(buffer[i]))
                        {
                            Complete();
                        }
                        else
                        {
                            token.Append(buffer[i]);
                            if (token.Length > MaxTokenLength)
                            {
                                throw Invalid("a series contains an invalid token");
                            }
                        }
                    }
                }

                reader.Read();
            }

            reader.Read();
        }

        Complete();
        if (count != timeSteps)
        {
            throw Invalid("a series does not have one token per time step");
        }
    }

    private static string ReadBoundedText(XmlReader reader, int maxLength, string path, bool trim = false)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return string.Empty;
        }

        var text = new StringBuilder();
        var buffer = new char[maxLength + 1];
        reader.Read();
        var pendingWhitespace = false;
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.NodeType is not (XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace))
            {
                throw Invalid(path + " must contain only text");
            }

            int read;
            while ((read = reader.ReadValueChunk(buffer, 0, buffer.Length)) > 0)
            {
                if (trim)
                {
                    for (var i = 0; i < read; i++)
                    {
                        var c = buffer[i];
                        if (char.IsWhiteSpace(c))
                        {
                            pendingWhitespace = text.Length > 0;
                        }
                        else
                        {
                            if (pendingWhitespace)
                            {
                                text.Append(' ');
                                pendingWhitespace = false;
                            }

                            text.Append(c);
                        }

                        if (text.Length > maxLength)
                        {
                            throw Invalid(path + " is too long");
                        }
                    }
                }
                else
                {
                    text.Append(buffer, 0, read);
                    if (text.Length > maxLength)
                    {
                        throw Invalid(path + " is too long");
                    }
                }
            }

            reader.Read();
        }

        reader.Read();
        var result = text.ToString();
        return trim ? result.Trim() : result;
    }

    private static MosmixForecastDocument ToDocument(PlacemarkResult placemark, DateTime issuedAt, IReadOnlyList<DateTime> timeline)
    {
        if (string.IsNullOrWhiteSpace(placemark.Description))
        {
            throw Invalid("kml:description of the location must be present and non-empty");
        }

        if (placemark.Coordinates is null)
        {
            throw Invalid("kml:coordinates of the location is missing");
        }

        var (latitude, longitude, altitude) = ParseCoordinates(placemark.Coordinates);
        return new MosmixForecastDocument
        {
            MosmixId = placemark.Id,
            IssuedAtUtc = issuedAt,
            Timeline = timeline,
            LocationName = placemark.Description,
            Latitude = latitude,
            Longitude = longitude,
            AltitudeM = altitude,
            Series = placemark.Series ?? Array.Empty<MosmixForecastSeries>()
        };
    }

    private static (decimal Latitude, decimal Longitude, decimal? Altitude) ParseCoordinates(string text)
    {
        var parts = text.Trim().Split(',');
        if (parts.Length != 3)
        {
            throw Invalid("kml:coordinates of the location is invalid");
        }

        if (!CoordinatePattern.IsMatch(parts[0]) || !CoordinatePattern.IsMatch(parts[1]) || !CoordinatePattern.IsMatch(parts[2]))
        {
            throw Invalid("kml:coordinates of the location is invalid");
        }

        var longitude = decimal.Parse(parts[0], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        var latitude = decimal.Parse(parts[1], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        var altitude = decimal.Parse(parts[2], NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

        if (latitude < -90 || latitude > 90)
        {
            throw Invalid("kml:coordinates of the location is invalid");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw Invalid("kml:coordinates of the location is invalid");
        }

        return (latitude, longitude, altitude);
    }
}
