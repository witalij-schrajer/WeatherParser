using System.Text;

namespace WeatherParser.Cli.Tests;

internal static class ProcessFixtures
{
    /// <summary>
    /// Gets the marker text that must not appear in any CLI output.
    /// </summary>
    public const string Marker = "marker-7c1-source-text";

    /// <summary>
    /// Builds a synthetic OsthessenWetter HTML page with the specified structure.
    /// </summary>
    public static string OsthessenPage()
    {
        string OverviewHeader() => "<th class='tg-iohx'>&nbsp;</th> <th class='tg-iohx'>Wetterstation<br></th><th>Temp.2m<br><font size='-2'></b>&Auml;nderung<b></th</font><th class='tg-iohx'>Temp.-<br>Trend</th><th class='tg-iohx'>Tmax/<br>Tmin</th><th class='tg-iohx'> Spitzen-<br>b&ouml;e</th><th class='tg-iohx'>Nieder-<br>schlag</th> <th class='tg-iohx'>Luft-<br>feuchte</th> </tr>";

        string WindHeader() => "<th class='tg-iohx'>&nbsp;</th> <th class='tg-iohx'>Wetterstation<br><font size='1em'>letzte Daten</font></th><th>Spitzen-<br>b&ouml;e<br><font size='1em'>heute</font></th><th class='tg-iohx'>Spitzen-<br>b&ouml;e<br><font size='1em'>verg.10Min.</font></th><th class='tg-iohx'>Wind<br>&#216;<br><font size='1em'>10 Min.</font></th><th class='tg-iohx'>Wind<br>&#216;<br><font size='1em'>60 Min.</font></th></tr>";

        string Value(string number, string unit) => string.IsNullOrEmpty(number) ? "" : number + "<font size='-2'>" + unit + "</font>";

        string OverviewRow(string nameHtml, string status = "Station online", string time = "09:31 Uhr", string temperature = "10.3", string change = "+1", string humidity = "93")
        {
            return $"<tr> <td class='tg-iohx'><img src='/uploads/s.jpg' title='{status}' width='18' height='18' border='0'></td> " +
                   $"<td align=center><b><font size='2em'>{nameHtml}</font><br><font size='1'>{time}</font></td> " +
                   $"<td class='tg-iohx'><font size='3'><b>{Value(temperature, "&deg;C")}</b></font><br><font size='1'>{Value(change, "&deg;C/h")}</b></font></td> " +
                   $"<td class='tg-iohx'><img src='/uploads/t.gif' title='{change}&deg;C/h' width='30' height='27' border='0'></td> " +
                   $"<td class='tg-iohx'><font color=#FF0000>11.1</font><font size='-2' color=#FF0000>&deg;C</font><br><font color=#0000FF>6.8</font><font size='-2' color=#0000FF>&deg;C</font></td></td> " +
                   $"<td class='tg-iohx'>57.9<font size='-2'>km/h</font></td> " +
                   $"<td class='tg-iohx'>8.4<font size='-2'>mm</font></td> " +
                   $"<td class='tg-iohx'>" + Value(humidity, "%") + "</td> </tr>";
        }

        string WindRow(string nameHtml, string status = "Station online", string time = "09:31 Uhr", string mean10 = "23.1")
        {
            return $"<tr> " +
                   $"<td class='tg-iohx'><img src='/uploads/s.jpg' title='{status}' width='18' height='18' border='0'></td> " +
                   $"<td align=center><b><font size='2em'>{nameHtml}</font><br><font size='1'>{time}</font></td> " +
                   $"<td class='tg-iohx'><font size='3'><b>57.9<font size='-2'>km/h</font></b></font></td> " +
                   $"<td class='tg-iohx'>30.6<font size='-2'>km/h</font></td> " +
                   $"<td class='tg-iohx'>{Value(mean10, "km/h")}</td></td> " +
                   $"<td class='tg-iohx'>24.0<font size='-2'>km/h</font></td> </tr>";
        }

        string Table(string header, params string[] rows)
        {
            return "<table width='100%' class='tg'> " + header + string.Concat(rows) + "</table>";
        }

        return "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><title>Messwerte</title></head><body>"
            + Table(OverviewHeader(), OverviewRow("M&#252;s-Hohh&#252;tte", time: "09:30 Uhr", temperature: "-4.2", change: "-0.5", humidity: "0"), OverviewRow("Langenbieber", status: "Station offline", time: "", temperature: "", change: "", humidity: ""))
            + "<table width='100%' class='tg'> <th class='tg-031e' colspan='6'><b>&#216;-Schneefallgrenze in Osthessen: </b></td><td class='tg-iohx'>1977 m</td></th></tr></table>"
            + Table(WindHeader(), WindRow("M&#252;s-Hohh&#252;tte", time: "09:30 Uhr", mean10: "0.0"), WindRow("Langenbieber", status: "Station offline", time: "", mean10: ""))
            + "<p>" + Marker + "</p></body></html>";
    }

    /// <summary>
    /// Creates a synthetic OsthessenWetter configuration with the specified parameters.
    /// </summary>
    public static string OsthessenConfiguration(Uri baseUri, bool mqtt, bool publicationPermitted)
    {
        var config = $$"""
            {
              "OsthessenWetter": {
                "PageUri": "{{baseUri}}osthessen/",
                "PublicationPermitted": {{publicationPermitted.ToString().ToLowerInvariant()}},
                "Stations": [
                  { "Name": "Müs-Hohhütte", "Latitude": 50.5, "Longitude": 9.9, "AltitudeM": 900 },
                  { "Name": "Langenbieber", "Latitude": 50.6, "Longitude": 9.8 }
                ]
              }
            }
            """;

        if (mqtt)
        {
            config = $$"""
                {
                  "OsthessenWetter": {
                    "PageUri": "{{baseUri}}osthessen/",
                    "PublicationPermitted": {{publicationPermitted.ToString().ToLowerInvariant()}},
                    "Stations": [
                      { "Name": "Müs-Hohhütte", "Latitude": 50.5, "Longitude": 9.9, "AltitudeM": 900 },
                      { "Name": "Langenbieber", "Latitude": 50.6, "Longitude": 9.8 }
                    ]
                  },
                  "Mqtt": {
                    "Broker": { "Host": "127.0.0.1", "Port": 1, "AllowUnencrypted": true },
                    "Topics": { "Observation": { "TopicTemplate": "test/weather/measurements/{source}/state" } }
                  }
                }
                """;
        }

        return config;
    }

    /// <summary>
    /// Creates a DWD configuration with the specified base URI.
    /// </summary>
    public static string DwdConfiguration(Uri baseUri) => $$"""
        {
          "Dwd": {
            "Catalog": {
              "AirTemperatureStationList": "{{baseUri}}cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt",
              "AirTemperatureNowDirectory": "{{baseUri}}cdc/air_temperature/now/",
              "WindStationList": "{{baseUri}}cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt",
              "WindNowDirectory": "{{baseUri}}cdc/wind/now/",
              "PrecipitationStationList": "{{baseUri}}cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt",
              "PrecipitationNowDirectory": "{{baseUri}}cdc/precipitation/now/",
              "SolarStationList": "{{baseUri}}cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt",
              "SolarNowDirectory": "{{baseUri}}cdc/solar/now/"
            }
          }
        }
        """;

    /// <summary>
    /// Creates a Mosmix configuration with the specified base URI.
    /// </summary>
    public static string MosmixConfiguration(Uri baseUri) => $$"""
        { "Mosmix": { "L": { "SingleStationsDirectory": "{{baseUri}}mosmix/single_stations/", "MaxRunAge": "08:00:00" } } }
        """;

    /// <summary>
    /// Encodes the specified text as UTF-8.
    /// </summary>
    public static byte[] Utf8(string text) => new UTF8Encoding(false).GetBytes(text);
}
