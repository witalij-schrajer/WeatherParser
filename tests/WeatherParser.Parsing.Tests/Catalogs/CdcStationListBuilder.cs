using System.Text;

namespace WeatherParser.Parsing.Tests.Catalogs;

internal static class CdcStationListBuilder
{
    public const string Header = "Stations_id von_datum bis_datum Stationshoehe geoBreite geoLaenge Stationsname Bundesland Abgabe";
    public const string DashLine = "----------- --------- --------- ------------- --------- --------- ----------------------------------------- ---------- --------";

    public static string Line(
        string stationId = "01234",
        string fromDate = "20000101",
        string toDate = "20261020",
        string altitude = "123",
        string latitude = "50.1234",
        string longitude = "9.5678",
        string name = "Musterhausen",
        string federalState = "Hessen")
    {
        var chars = new string(' ', 1000).ToCharArray();

        // Station ID (positions 0-4)
        stationId.CopyTo(0, chars, 0, stationId.Length);

        // From date (positions 6-13)
        fromDate.CopyTo(0, chars, 6, fromDate.Length);

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
        federalState.CopyTo(0, chars, 102, federalState.Length);

        // Release status (position 143)
        "Frei".CopyTo(0, chars, 143, 4);

        return new string(chars);
    }

    public static MemoryStream Stream(
        IEnumerable<string> dataLines,
        string header = Header,
        string dashLine = DashLine,
        string lineEnding = "\r\n",
        Encoding? encoding = null)
    {
        var sb = new StringBuilder();
        sb.Append(header).Append(lineEnding);
        sb.Append(dashLine).Append(lineEnding);
        foreach (var line in dataLines)
        {
            sb.Append(line).Append(lineEnding);
        }

        var bytes = (encoding ?? Encoding.Latin1).GetBytes(sb.ToString());
        return new MemoryStream(bytes);
    }

    public static MemoryStream Raw(string text, Encoding? encoding = null)
    {
        return new MemoryStream((encoding ?? Encoding.Latin1).GetBytes(text));
    }
}
