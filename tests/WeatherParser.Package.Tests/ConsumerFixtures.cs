using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace WeatherParser.Package.Tests;

internal static class ConsumerFixtures
{
    public static byte[] CdcList(params string[] lines)
    {
        var sb = new StringBuilder();
        sb.Append("Stations_id von_datum bis_datum Stationshoehe geoBreite geoLaenge Stationsname Bundesland Abgabe\r\n");
        sb.Append("----------- --------- --------- ------------- --------- --------- ----------------------------------------- ---------- --------\r\n");
        foreach (var line in lines)
        {
            sb.Append(line);
            sb.Append("\n");
        }

        return Encoding.Latin1.GetBytes(sb.ToString());
    }

    public static string CdcLine(string stationId, string altitude, string latitude, string longitude, string name, string toDate = "20261021")
    {
        var chars = new string(' ', 1000).ToCharArray();

        // Station ID (positions 0-4)
        stationId.CopyTo(0, chars, 0, stationId.Length);

        // From date (positions 6-13)
        "20000101".CopyTo(0, chars, 6, 8);

        // To date (positions 15-22)
        toDate.CopyTo(0, chars, 15, toDate.Length);

        // Altitude (positions 23-38, right-aligned)
        var altitudeEnd = 38;
        var altitudeStart = altitudeEnd - altitude.Length + 1;
        altitude.CopyTo(0, chars, altitudeStart, altitude.Length);

        // Latitude (positions 39-50, right-aligned)
        var latitudeEnd = 50;
        var latitudeStart = latitudeEnd - latitude.Length + 1;
        latitude.CopyTo(0, chars, latitudeStart, latitude.Length);

        // Longitude (positions 51-59, right-aligned)
        var longitudeEnd = 59;
        var longitudeStart = longitudeEnd - longitude.Length + 1;
        longitude.CopyTo(0, chars, longitudeStart, longitude.Length);

        // Name (positions 61-100, left-aligned)
        name.CopyTo(0, chars, 61, name.Length);

        // Federal state (positions 102-141, left-aligned)
        "Hessen".CopyTo(0, chars, 102, 6);

        // Release status (position 143)
        "Frei".CopyTo(0, chars, 143, 4);

        return new string(chars);
    }

    public static byte[] CdcNowZip(string productCode, string stationId, params string[] rows)
    {
        if (productCode != "tu")
        {
            throw new ArgumentException("Only 'tu' product code is supported.", nameof(productCode));
        }

        var header = "STATIONS_ID;MESS_DATUM;  QN;PP_10;TT_10;TM5_10;RF_10;TD_10;eor";
        var sb = new StringBuilder();
        sb.Append(header);
        sb.Append("\r\n");
        foreach (var row in rows)
        {
            sb.Append(row);
            sb.Append("\r\n");
        }

        var content = Encoding.Latin1.GetBytes(sb.ToString());
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry($"produkt_zehn_now_{productCode}_20250420_20261021_{stationId}.txt", CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(content, 0, content.Length);
        }

        return ms.ToArray();
    }

    public static string CdcRow(string stationId, string messDatum, params string[] values)
    {
        return $"{stationId,10};{messDatum};    3;" + string.Join(";", values.Select(v => v.PadLeft(8))) + ";eor";
    }

    public static byte[] MosmixCatalog(params string[] lines)
    {
        var sb = new StringBuilder();
        sb.Append("ID    ICAO NAME                 LAT    LON     ELEV\n");
        sb.Append("----- ---- -------------------- ------ ------- -----\n");
        foreach (var line in lines)
        {
            sb.Append(line);
            sb.Append("\n");
        }

        return Encoding.Latin1.GetBytes(sb.ToString());
    }

    public static string MosmixLine(string id, string name, string latitude, string longitude, string? elevation)
    {
        return id.PadLeft(5) + " " + "----".PadRight(4) + " " + name.PadRight(20) + " " + latitude.PadLeft(6) + " " + longitude.PadLeft(7) + (elevation is null ? "" : " " + elevation.PadLeft(5));
    }

    public static byte[] MosmixLZip(string kml)
    {
        return Kmz(kml);
    }

    public static byte[] MosmixLKmz(string id)
    {
        var kml = MosmixLKml(id);
        return MosmixLZip(kml);
    }

    private const string KmlNs = "http://www.opengis.net/kml/2.2";
    private const string DwdNs = "https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd";
    private const string DefaultIssueTime = "2026-10-21T03:00:00.000Z";

    private static string Forecast(string elementName, string values) => $"<dwd:Forecast dwd:elementName=\"{elementName}\"><dwd:value>{values}</dwd:value></dwd:Forecast>";

    private static string MosmixLKml(string id = "X123A", string issueTime = DefaultIssueTime, int timeSteps = 3)
    {
        var steps = "";
        for (int i = 0; i < timeSteps; i++)
        {
            steps += $"          <dwd:TimeStep>2026-10-21T{(4 + i):00}:00:00.000Z</dwd:TimeStep>\n";
        }

        var forecasts = new[] { Forecast("TTT", "281.15 281.05 -"), Forecast("FF", "3.10 - 2.60") };

        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"ISO-8859-1\" standalone=\"no\"?>\n");
        sb.Append($"<kml:kml xmlns:dwd=\"{DwdNs}\" xmlns:kml=\"{KmlNs}\">\n");
        sb.Append("  <kml:Document>\n");
        sb.Append("    <kml:ExtendedData>\n");
        sb.Append("      <dwd:ProductDefinition>\n");
        sb.Append("        <dwd:Issuer>Example issuer</dwd:Issuer>\n");
        sb.Append("        <dwd:ProductID>MOSMIX</dwd:ProductID>\n");
        sb.Append($"        <dwd:IssueTime>{issueTime}</dwd:IssueTime>\n");
        sb.Append("        <dwd:ForecastTimeSteps>\n");
        sb.Append(steps);
        sb.Append("        </dwd:ForecastTimeSteps>\n");
        sb.Append("      </dwd:ProductDefinition>\n");
        sb.Append("    </kml:ExtendedData>\n");
        sb.Append("    <kml:Placemark>\n");
        sb.Append($"      <kml:name>{id}</kml:name>\n");
        sb.Append("      <kml:description>BEISPIELSTADT</kml:description>\n");
        sb.Append("      <kml:ExtendedData>\n");
        foreach (var forecast in forecasts)
        {
            sb.Append("        ").Append(forecast).Append('\n');
        }
        sb.Append("      </kml:ExtendedData>\n");
        sb.Append("      <kml:Point>\n");
        sb.Append("        <kml:coordinates>9.6,50.5,250.0</kml:coordinates>\n");
        sb.Append("      </kml:Point>\n");
        sb.Append("    </kml:Placemark>\n");
        sb.Append("  </kml:Document>\n");
        sb.Append("</kml:kml>\n");
        return sb.ToString();
    }

    private const string MosmixLMemberName = "MOSMIX_L_2026102103_X123A.kml";

    private static byte[] Kmz(string kml)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(MosmixLMemberName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            var bytes = Encoding.Latin1.GetBytes(kml);
            entryStream.Write(bytes, 0, bytes.Length);
        }

        return ms.ToArray();
    }

    public static byte[] MosmixSKmz(params (string Id, string[] Elements)[] placemarks)
    {
        var kml = MosmixSKml(placemarks: placemarks.Select(p => MosmixSPlacemark(p.Id, p.Elements.Select(e => Forecast(e, "281.15 - 280.95")))).ToArray());
        return MosmixSZip(kml);
    }

    private static string MosmixSPlacemark(string id, IEnumerable<string> forecasts)
    {
        var sb = new StringBuilder();
        sb.Append("<kml:Placemark><kml:name>");
        sb.Append(id);
        sb.Append("</kml:name><kml:description>ORT</kml:description><kml:ExtendedData>");
        sb.Append(string.Concat(forecasts));
        sb.Append("</kml:ExtendedData><kml:Point><kml:coordinates>9.6,50.5,250.0</kml:coordinates></kml:Point></kml:Placemark>");
        return sb.ToString();
    }

    private static string MosmixSKml(string issueTime = DefaultIssueTime, int timeSteps = 3, params string[] placemarks)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"ISO-8859-1\" standalone=\"no\"?>\n");
        sb.Append("<kml:kml xmlns:dwd=\"https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd\" xmlns:kml=\"http://www.opengis.net/kml/2.2\">\n");
        sb.Append("<kml:Document>\n");
        sb.Append("<kml:ExtendedData>\n");
        sb.Append("<dwd:ProductDefinition>\n");
        sb.Append("<dwd:Issuer>Example issuer</dwd:Issuer>\n");
        sb.Append("<dwd:ProductID>MOSMIX</dwd:ProductID>\n");
        sb.Append("<dwd:IssueTime>");
        sb.Append(issueTime);
        sb.Append("</dwd:IssueTime>\n");
        sb.Append("<dwd:ForecastTimeSteps>\n");
        for (int i = 0; i < timeSteps; i++)
        {
            sb.Append("<dwd:TimeStep>2026-10-21T");
            sb.Append((4 + i).ToString("00"));
            sb.Append(":00:00.000Z</dwd:TimeStep>\n");
        }
        sb.Append("</dwd:ForecastTimeSteps>\n");
        sb.Append("</dwd:ProductDefinition>\n");
        sb.Append("</kml:ExtendedData>\n");
        foreach (string placemark in placemarks)
        {
            sb.Append(placemark);
            sb.Append('\n');
        }
        sb.Append("</kml:Document>\n");
        sb.Append("</kml:kml>");
        return sb.ToString();
    }

    private static byte[] MosmixSZip(string kml, string memberName = "MOSMIX_S_2026102303_240.kml")
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(memberName, CompressionLevel.Optimal);
            using (var entryStream = entry.Open())
            {
                var bytes = Encoding.Latin1.GetBytes(kml);
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }
        return stream.ToArray();
    }
}
