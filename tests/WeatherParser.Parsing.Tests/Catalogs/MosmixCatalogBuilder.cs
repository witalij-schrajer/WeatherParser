using System;
using System.Linq;
using System.Text;

namespace WeatherParser.Parsing.Tests.Catalogs;

internal static class MosmixCatalogBuilder
{
    public const string Header = "ID    ICAO NAME                 LAT    LON     ELEV";
    public const string DashLine = "----- ---- -------------------- ------ ------- -----";

    public static string Line(
        string id = "X123A",
        string icao = "----",
        string name = "MUSTERHAUSEN",
        string latitude = "50.00",
        string longitude = "9.00",
        string? elevation = "250")
    {
        return id.PadLeft(5) + " " + icao.PadRight(4) + " " + name.PadRight(20) + " " + latitude.PadLeft(6) + " " + longitude.PadLeft(7) + (elevation is null ? "" : " " + elevation.PadLeft(5));
    }

    public static byte[] Catalog(params string[] lines)
    {
        return Encoding.Latin1.GetBytes(Header + "\n" + DashLine + "\n" + string.Concat(lines.Select(l => l + "\n")));
    }

    public static byte[] Text(string text)
    {
        return Encoding.Latin1.GetBytes(text);
    }
}
