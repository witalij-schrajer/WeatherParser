using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageOsthessenWetterTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Uri PageUri = new("https://a.example.invalid/messwerte/");

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

    private static ConsumerSelectionHost Host(params string[] metadataNames)
    {
        var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.Services.Replace(ServiceDescriptor.Singleton(new OsthessenWetterEndpoints { PageUri = PageUri }));
        host.Services.Replace(ServiceDescriptor.Singleton(new OsthessenWetterOptions { Stations = metadataNames.Select(n => new OsthessenWetterStationMetadata { Name = n, Latitude = 50.5m, Longitude = 9.7m }).ToList() }));
        return host;
    }

    [TestMethod]
    public void Package_ResolvesOsthessenWetterParser()
    {
        using var host = Host("Bimbach");
        var parser = host.Provider.GetRequiredService<IOsthessenWetterParser>();
        parser.Should().BeOfType<OsthessenWetterParser>();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Package_ParseAsync_OneAndSeveralStations()
    {
        using var host = Host("Bimbach", "Schlitz");
        host.Handler.Add(PageUri, Page(Row("Bimbach"), Row("Schlitz")));

        var parser = host.Provider.GetRequiredService<IOsthessenWetterParser>();
        var result1 = await parser.ParseAsync("Bimbach", CancellationToken.None);
        result1.Datasets.Select(d => d.Source).Should().Equal(new[] { "osthessenwetter:bimbach" });
        var result2 = await parser.ParseAsync("*", CancellationToken.None);
        result2.Datasets.Select(d => d.Source).Should().Equal(new[] { "osthessenwetter:bimbach", "osthessenwetter:schlitz" });
        result2.StationFailures.Should().BeEmpty();
        host.Handler.Requests.Should().Equal(new[] { PageUri.AbsoluteUri, PageUri.AbsoluteUri });
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Package_Datasets_SerializeAndValidate()
    {
        using var host = Host("Bimbach");
        host.Handler.Add(PageUri, Page(Row("Bimbach")));

        var parser = host.Provider.GetRequiredService<IOsthessenWetterParser>();
        var result = await parser.ParseAsync("Bimbach", CancellationToken.None);
        var dataset = result.Datasets[0];

        var xml = await host.XmlAsync(dataset);
        ConsumerSelectionHost.ValidateAgainstPackagedSchema(xml).Should().BeEmpty();

        var json = await host.JsonAsync(dataset);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("source").GetString().Should().Be("osthessenwetter:bimbach");
        doc.RootElement.GetProperty("timestamp_quality").GetString().Should().Be("estimated_date");
        doc.RootElement.GetProperty("retrieved_at").GetString().Should().Be("2026-10-21T12:00:00");
        doc.RootElement.GetProperty("measurements").GetProperty("temperature_c").GetProperty("measured_at").GetString().Should().Be("2026-10-21T11:31:00");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Package_NoMatch_ThrowsPatternNoMatch()
    {
        using var host = Host("Bimbach");
        host.Handler.Add(PageUri, Page(Row("Bimbach")));

        var parser = host.Provider.GetRequiredService<IOsthessenWetterParser>();
        Func<Task> act = () => parser.ParseAsync("Fulda", CancellationToken.None);
        await act.Should().ThrowAsync<OsthessenWetterException>()
            .Where(e => e.ErrorId == OsthessenWetterException.PatternNoMatchErrorId);

        host.Handler.Requests.Should().Equal(new[] { PageUri.AbsoluteUri });
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Package_InvalidPattern_ThrowsWithoutRequest()
    {
        using var host = Host("Bimbach");

        var parser = host.Provider.GetRequiredService<IOsthessenWetterParser>();
        Func<Task> act = () => parser.ParseAsync("", CancellationToken.None);
        await act.Should().ThrowAsync<OsthessenWetterException>()
            .Where(e => e.ErrorId == OsthessenWetterException.PatternInvalidErrorId);

        host.Handler.Requests.Should().BeEmpty();
    }
}
