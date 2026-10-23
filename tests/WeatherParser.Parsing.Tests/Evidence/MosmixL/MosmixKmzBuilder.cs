using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixL;

internal static class MosmixKmzBuilder
{
    public const string KmlNs = "http://www.opengis.net/kml/2.2";
    public const string DwdNs = "https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd";
    public const string DefaultIssueTime = "2026-10-21T03:00:00.000Z";

    public static string Forecast(string elementName, string values) => $"<dwd:Forecast dwd:elementName=\"{elementName}\"><dwd:value>{values}</dwd:value></dwd:Forecast>";

    public static string Kml(string id = "X123A", string issueTime = DefaultIssueTime, int timeSteps = 3, params string[] forecasts)
    {
        var steps = "";
        for (int i = 0; i < timeSteps; i++)
        {
            steps += $"          <dwd:TimeStep>2026-10-21T{(4 + i):00}:00:00.000Z</dwd:TimeStep>\n";
        }

        if (forecasts.Length == 0)
        {
            forecasts = new[] { Forecast("TTT", "281.15 281.05 -"), Forecast("FF", "3.10 - 2.60") };
        }

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

    public const string MemberName = "MOSMIX_L_2026102103_X123A.kml";

    public static byte[] Zip(CompressionLevel level, params (string Name, byte[] Content)[] members)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in members)
            {
                var entry = archive.CreateEntry(name, level);
                using var entryStream = entry.Open();
                entryStream.Write(content, 0, content.Length);
            }
        }

        return ms.ToArray();
    }

    public static byte[] Kmz(string kml, CompressionLevel level = CompressionLevel.Optimal) => Zip(level, (MemberName, Encoding.Latin1.GetBytes(kml)));

    public static string PadTo(string kml, int length)
    {
        if (length < kml.Length + 7)
        {
            throw new ArgumentException("Target length is too small to fit the KML with padding comment.", nameof(length));
        }

        var paddingLength = length - kml.Length - 7;
        var padding = new string(' ', paddingLength);
        var index = kml.LastIndexOf("</kml:kml>");
        return kml.Insert(index, $"<!--{padding}-->");
    }
}
