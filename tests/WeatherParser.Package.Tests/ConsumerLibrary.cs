using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Builds one fully registered consumer host per call (every endpoint and options type of the library, the REQ-061 example topics as consumer configuration) and serves synthetic fixtures for every source; public package API only.
/// </summary>
internal static class ConsumerLibrary
{
    public static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    public static MqttTopicOptions ExampleTopics(string prefix = "haus/weather")
    {
        return new MqttTopicOptions
        {
            Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
            {
                [MqttMessageType.Observation] = new()
                {
                    TopicTemplate = prefix + "/measurements/{source}/state"
                },
                [MqttMessageType.Forecast] = new()
                {
                    TopicTemplate = prefix + "/forecasts/{source}/state"
                },
                [MqttMessageType.CdcStationList] = new()
                {
                    TopicTemplate = prefix + "/stations/dwd/selection"
                },
                [MqttMessageType.MosmixLocationList] = new()
                {
                    TopicTemplate = prefix + "/stations/mosmix/selection"
                }
            }
        };
    }

    public static ConsumerSelectionHost Host(string baseUri = "https://a.example.invalid/", MqttTopicOptions? topics = null)
    {
        var host = new ConsumerSelectionHost(baseUri, Now);
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri(baseUri + "forecast/single_stations/") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri(baseUri + "forecast/all_stations/MOSMIX_S_LATEST_240.kmz") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(24) }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(24) }));
        host.Services.Replace(ServiceDescriptor.Singleton(new OsthessenWetterEndpoints { PageUri = PageUri(baseUri) }));
        host.Services.Replace(ServiceDescriptor.Singleton(new OsthessenWetterOptions { Stations = { new OsthessenWetterStationMetadata { Name = "Bimbach", Latitude = 50.53m, Longitude = 9.72m, AltitudeM = 312m } } }));
        host.Services.Replace(ServiceDescriptor.Singleton(topics ?? ExampleTopics()));
        return host;
    }

    public static Task<MqttMessage> Mqtt<TRoot>(ConsumerSelectionHost host, TRoot root) where TRoot : class =>
        host.Provider.GetRequiredService<IMqttMessageSerializer>().SerializeAsync(root, CancellationToken.None);

    public static Uri PageUri(string baseUri) => new(baseUri + "messwerte/");

    public static Uri MosmixLUri(string baseUri, string id) => new(baseUri + "forecast/single_stations/" + id + "/kml/MOSMIX_L_LATEST_" + id + ".kmz");

    public static Uri MosmixSUri(string baseUri) => new(baseUri + "forecast/all_stations/MOSMIX_S_LATEST_240.kmz");

    public static void ArrangeMosmixL(ConsumerSelectionHost host, string baseUri, string id)
    {
        host.Handler.Add(MosmixLUri(baseUri, id), ConsumerFixtures.MosmixLForecastKmz(id, "BEISPIELSTADT", ("TTT", "283.15 284.15 285.15"), ("FF", "0.00 5.00 10.00")));
    }

    public static void ArrangeMosmixS(ConsumerSelectionHost host, string baseUri, string id)
    {
        host.Handler.Add(MosmixSUri(baseUri), ConsumerFixtures.MosmixSForecastKmz(id, "BEISPIELSTADT", ("TTT", "283.15 284.15 285.15")));
    }

    public static void ArrangeOsthessen(ConsumerSelectionHost host, string baseUri)
    {
        host.Handler.Add(PageUri(baseUri), Page(Row("Bimbach")));
    }

    private static string Row(string name, string time = "13:31 Uhr", string temperature = "10.3", string humidity = "93")
    {
        return "<tr> <td class='tg-iohx'><img src='/uploads/s.jpg' title='Station online' width='18' height='18' border='0'></td> " +
               $"<td align=center><b><font size='2em'>{name}</font><br><font size='1'>{time}</font></td> " +
               $"<td class='tg-iohx'><font size='3'><b>{temperature}<font size='-2'>&deg;C</font></b></font><br><font size='1'>+1<font size='-2'>&deg;C/h</font></b></font></td> " +
               $"<td class='tg-iohx'><img src='/uploads/t.gif' title='+1&deg;C/h' width='30' height='27' border='0'></td> " +
               $"<td class='tg-iohx'><font color=#FF0000>11.1</font><font size='-2' color=#FF0000>&deg;C</font><br><font color=#0000FF>6.8</font><font size='-2' color=#0000FF>&deg;C</font></td></td> " +
               $"<td class='tg-iohx'>57.9<font size='-2'>km/h</font></td> " +
               $"<td class='tg-iohx'>8.4<font size='-2'>mm</font></td> " +
               $"<td class='tg-iohx'>{humidity}<font size='-2'>%</font></td> </tr>";
    }

    private static byte[] Page(params string[] rows)
    {
        var header = "<th class='tg-iohx'>&nbsp;</th> <th class='tg-iohx'>Wetterstation<br></th><th>Temp.2m<br><font size='-2'></b>&Auml;nderung<b></th</font><th class='tg-iohx'>Temp.-<br>Trend</th><th class='tg-iohx'>Tmax/<br>Tmin</th><th class='tg-iohx'> Spitzen-<br>b&ouml;e</th><th class='tg-iohx'>Nieder-<br>schlag</th> <th class='tg-iohx'>Luft-<br>feuchte</th> </tr>";
        return Encoding.UTF8.GetBytes("<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><title>Messwerte</title></head><body><table width='100%' class='tg'> " + header + string.Concat(rows) + "</table></body></html>");
    }
}
