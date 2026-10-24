using System.IO;
using System.IO.Compression;
using System.Text;

internal static class MosmixSKmzBuilder
{
    public const string DefaultIssueTime = "2026-10-23T03:00:00.000Z";

    public static string Forecast(string elementName, string values)
    {
        var sb = new StringBuilder();
        sb.Append("<dwd:Forecast dwd:elementName=\"");
        sb.Append(elementName);
        sb.Append("\"><dwd:value>");
        sb.Append(values);
        sb.Append("</dwd:value></dwd:Forecast>");
        return sb.ToString();
    }

    public static string Placemark(string id, params string[] forecasts)
    {
        var sb = new StringBuilder();
        sb.Append("<kml:Placemark><kml:name>");
        sb.Append(id);
        sb.Append("</kml:name><kml:description>ORT</kml:description><kml:ExtendedData>");
        sb.Append(string.Concat(forecasts));
        sb.Append("</kml:ExtendedData><kml:Point><kml:coordinates>9.6,50.5,250.0</kml:coordinates></kml:Point></kml:Placemark>");
        return sb.ToString();
    }

    public static string Kml(string issueTime = DefaultIssueTime, int timeSteps = 3, params string[] placemarks)
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
            sb.Append("<dwd:TimeStep>2026-10-23T");
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

    public static byte[] Kmz(string kml, string memberName = "MOSMIX_S_2026102303_240.kml")
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

    public static string[] Placemarks(int count, Func<int, string> forecasts)
    {
        var result = new string[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = Placemark(i.ToString("D5"), forecasts(i));
        }
        return result;
    }
}
