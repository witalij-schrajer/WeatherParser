using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

internal static class MosmixForecastKmzBuilder
{
    public const string KmlNs = "http://www.opengis.net/kml/2.2";
    public const string DwdNs = "https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd";
    public const string DefaultIssueTime = "2026-10-26T15:00:00.000Z";

    public static string TimeSteps(int count)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            var time = new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc).AddHours(i);
            sb.Append($"          <dwd:TimeStep>{time.ToString("yyyy-MM-dd'T'HH:mm:ss'.000Z'", CultureInfo.InvariantCulture)}</dwd:TimeStep>\n");
        }
        return sb.ToString();
    }

    public static string ProductDefinition(string? issueTime = DefaultIssueTime, int timeSteps = 3, string? timeStepsXml = null, string productId = "MOSMIX", string extra = "")
    {
        var sb = new StringBuilder();
        sb.Append("      <dwd:ProductDefinition>\n");
        sb.Append("        <dwd:Issuer>Example issuer</dwd:Issuer>\n");
        sb.Append($"        <dwd:ProductID>{productId}</dwd:ProductID>\n");
        sb.Append("        <dwd:GeneratingProcess>Example process</dwd:GeneratingProcess>\n");
        if (issueTime != null)
        {
            sb.Append($"        <dwd:IssueTime>{issueTime}</dwd:IssueTime>\n");
        }
        sb.Append("        <dwd:ReferencedModel><dwd:Model dwd:name=\"ICON\" dwd:referenceTime=\"2026-10-26T12:00:00Z\"/></dwd:ReferencedModel>\n");
        sb.Append("        <dwd:ForecastTimeSteps>\n");
        sb.Append(timeStepsXml ?? TimeSteps(timeSteps));
        sb.Append("        </dwd:ForecastTimeSteps>\n");
        sb.Append("        <dwd:FormatCfg><dwd:DefaultUndefSign>-</dwd:DefaultUndefSign></dwd:FormatCfg>\n");
        sb.Append(extra);
        sb.Append("      </dwd:ProductDefinition>\n");
        return sb.ToString();
    }

    public static string Forecast(string elementName, string values) => $"<dwd:Forecast dwd:elementName=\"{elementName}\"><dwd:value>{values}</dwd:value></dwd:Forecast>";

    public static string Placemark(string id, string description = "BEISPIELSTADT", string? coordinates = "9.6,50.5,250.0", params string[] forecasts)
    {
        if (forecasts.Length == 0)
        {
            forecasts = new[] { Forecast("TTT", "281.15 281.05 -"), Forecast("FF", "3.10 - 2.60") };
        }

        var sb = new StringBuilder();
        sb.Append($"    <kml:Placemark>\n");
        sb.Append($"      <kml:name>{id}</kml:name>\n");
        sb.Append($"      <kml:description>{description}</kml:description>\n");
        sb.Append("      <kml:ExtendedData>\n");
        foreach (var forecast in forecasts)
        {
            sb.Append("        ").Append(forecast).Append('\n');
        }
        sb.Append("      </kml:ExtendedData>\n");
        if (coordinates != null)
        {
            sb.Append("      <kml:Point>\n");
            sb.Append($"        <kml:coordinates>{coordinates}</kml:coordinates>\n");
            sb.Append("      </kml:Point>\n");
        }
        sb.Append("    </kml:Placemark>\n");
        return sb.ToString();
    }

    public static string Document(string productDefinition, params string[] placemarks)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"ISO-8859-1\" standalone=\"yes\"?>\n");
        sb.Append($"<kml:kml xmlns:dwd=\"{DwdNs}\" xmlns:kml=\"{KmlNs}\">\n");
        sb.Append("  <kml:Document>\n");
        sb.Append("    <kml:ExtendedData>\n");
        sb.Append(productDefinition);
        sb.Append("    </kml:ExtendedData>\n");
        foreach (var placemark in placemarks)
        {
            sb.Append(placemark);
        }
        sb.Append("  </kml:Document>\n");
        sb.Append("</kml:kml>\n");
        return sb.ToString();
    }

    public static string Single(string id = "10637", string description = "BEISPIELSTADT", string coordinates = "9.6,50.5,250.0", params string[] forecasts)
    {
        return Document(ProductDefinition(), Placemark(id, description, coordinates, forecasts));
    }

    public const string SingleMemberName = "MOSMIX_L_2026102615_10637.kml";
    public const string AllStationsMemberName = "MOSMIX_S_2026102619_240.kml";

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

    public static byte[] Kmz(string kml, CompressionLevel level = CompressionLevel.Optimal, string memberName = SingleMemberName) => Zip(level, (memberName, Encoding.Latin1.GetBytes(kml)));

    public static byte[] LargeAllStationsKmz(int placemarks, int selectedIndex, string selectedId, int timeSteps, int elementsPerPlacemark, bool includeTemperature = false, string? issueTime = DefaultIssueTime)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(AllStationsMemberName, CompressionLevel.Fastest);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.Latin1);

            writer.WriteLine("<?xml version=\"1.0\" encoding=\"ISO-8859-1\" standalone=\"yes\"?>");
            writer.WriteLine($"<kml:kml xmlns:dwd=\"{DwdNs}\" xmlns:kml=\"{KmlNs}\">");
            writer.WriteLine("  <kml:Document>");
            writer.WriteLine("    <kml:ExtendedData>");
            writer.Write(ProductDefinition(issueTime: issueTime, timeSteps: timeSteps));
            writer.WriteLine("    </kml:ExtendedData>");

            var values = string.Join(" ", new string[timeSteps].Select(_ => "281.15"));
            for (int i = 0; i < placemarks; i++)
            {
                var id = i == selectedIndex ? selectedId : "P" + i.ToString("D4", CultureInfo.InvariantCulture);
                writer.WriteLine($"    <kml:Placemark>");
                writer.WriteLine($"      <kml:name>{id}</kml:name>");
                writer.WriteLine("      <kml:description>STATION</kml:description>");
                writer.WriteLine("      <kml:ExtendedData>");

                if (includeTemperature)
                {
                    var temperatureValues = string.Join(" ", new string[timeSteps].Select(_ => "283.15"));
                    writer.WriteLine($"        <dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>{temperatureValues}</dwd:value></dwd:Forecast>");
                }

                for (int k = 0; k < elementsPerPlacemark; k++)
                {
                    writer.WriteLine($"        <dwd:Forecast dwd:elementName=\"E{k}\"><dwd:value>{values}</dwd:value></dwd:Forecast>");
                }

                writer.WriteLine("      </kml:ExtendedData>");
                writer.WriteLine("      <kml:Point>");
                writer.WriteLine("        <kml:coordinates>9.6,50.5,250.0</kml:coordinates>");
                writer.WriteLine("      </kml:Point>");
                writer.WriteLine("    </kml:Placemark>");
            }

            writer.WriteLine("  </kml:Document>");
            writer.WriteLine("</kml:kml>");
        }

        return ms.ToArray();
    }
}
