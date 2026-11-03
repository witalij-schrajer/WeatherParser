# Die WeatherParser-Bibliothek verwenden

> Deutsche Übersetzung von [docs/LIBRARY_USAGE.md](../LIBRARY_USAGE.md). Maßgeblich ist die englische Fassung; die Übersetzung wird vor jeder neuen Version mit ihr abgeglichen und kann bis dahin zurückliegen. Übersetzungsstand: englische Fassung von `develop` `58eac05` (2026-11-03) mit den Änderungen zur Sichtbarkeit des Repositorys vom 2026-11-03. Die Codebeispiele sind unverändert übernommen; ihre Kommentare bleiben englisch.

Diese Anleitung führt Sie von der Paketdatei zu einer lauffähigen .NET-Anwendung, die die aktuellen DWD-10-Minuten-Beobachtungen einer Station liest und als JSON schreibt. Danach zeigt sie die anderen Quellen und den MQTT-Nachrichten-Serialisierer. Die ausführliche Referenz jeder Quelle (Optionen, Grenzen, exportierte Größen, Fehler-IDs, Quellenbedingungen) steht in der [README](README.md#bibliotheksreferenz).

Status: Version 0.1.0. Sie ist nicht auf nuget.org veröffentlicht. Sie benötigen die Paketdatei (siehe Abschnitt 1).

## 1. Was Sie benötigen

- Das .NET-10-SDK zum Bauen Ihrer Anwendung und die .NET-10-Laufzeit 10.0.14 oder neuer zum Ausführen ([CHANGELOG.md](../../CHANGELOG.md), „Compatibility“, Englisch).
- Die Paketdatei `WeatherParser.Parsing.0.1.0.nupkg`. Entweder:
  - Sie nehmen die `.nupkg` eines Release-Builds und prüfen sie gegen die Datei `SHA256SUMS` des Releases (`sha256sum -c --ignore-missing SHA256SUMS` unter Linux), oder
  - Sie bauen sie selbst aus einem Checkout wie in [CONTRIBUTING.md Abschnitt 4](../../CONTRIBUTING.md#4-pack-and-package-consumer) (Englisch) beschrieben.
- Netzwerkzugriff auf die Server der Quellen, wenn die Anwendung läuft. Die Bibliothek stellt einfache HTTPS-GET-Anfragen über den `HttpClient`, den Sie bereitstellen.

Ihre Anwendung benötigt weder den Quellcode-Checkout noch PowerShell, MSTest oder die Coverage-Werkzeuge. Die verifizierten Plattformen stehen in [COMPATIBILITY.md](COMPATIBILITY.md). Debian 13 ARM64 (NanoPi R76S) ist das verifizierte Zielsystem, und Windows x64 ist die getestete Entwicklungsplattform. Die ARM64-Bereitstellungsregeln aus [OPERATIONS.md](OPERATIONS.md) gelten für die Beispiel-CLI, nicht für Ihre Anwendung.

## 2. Die Anwendung anlegen und das Paket hinzufügen

Legen Sie die Paketdatei in einen Ordner, der als lokale Paketquelle dient, und fügen Sie sie dann einer neuen Konsolenanwendung hinzu:

```bash
dotnet new console --framework net10.0 --name MyWeatherApp
cd MyWeatherApp
mkdir -p ../local-nuget && cp <Pfad>/WeatherParser.Parsing.0.1.0.nupkg ../local-nuget/
dotnet new nugetconfig
dotnet nuget add source ../local-nuget --name weatherparser-local --configfile nuget.config
dotnet add package WeatherParser.Parsing --version 0.1.0
dotnet add package Microsoft.Extensions.DependencyInjection --version 10.0.14
dotnet add package Microsoft.Extensions.Logging.Console --version 10.0.14
```

`dotnet new nugetconfig` legt eine `nuget.config` mit nuget.org an; der nächste Befehl fügt den lokalen Ordner daneben hinzu. Beschränken Sie eine Wiederherstellung nicht auf den lokalen Ordner (`--source`): Die Bibliothek hängt von `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions` und `AngleSharp` ab, die NuGet von nuget.org wiederherstellt. Der Container und der Konsolen-Logger oben sind die Wahl Ihrer Anwendung. Jede Implementierung von `IServiceProvider`, die `ILogger<T>` auflöst, funktioniert.

Liegt ein früherer Build von `0.1.0` bereits in Ihrem NuGet-Cache, verwendet NuGet die zwischengespeicherte Kopie. Löschen Sie zuerst den Cache-Eintrag `weatherparser.parsing/0.1.0`, wie in CONTRIBUTING.md Abschnitt 4 beschrieben.

## 3. Die Bibliothek registrieren

`AddWeatherParsing()` registriert jeden Bibliotheksdienst für alle Quellen als Singleton. Es registriert keinen Endpunkt, keine Option, keinen `HttpClient`, keinen `TimeProvider` und keinen Logger: Diese stellt Ihre Anwendung bereit. Mit Container-Validierung (`ValidateOnBuild`) werden die Abhängigkeiten jedes Dienstes geprüft; deshalb müssen alle unten aufgeführten Typen registriert werden, auch wenn Sie nur eine Quelle nutzen. Ein fehlender Typ schlägt beim Bauen des Containers fehl, vor jeder Anfrage.

Fügen Sie diese Datei Ihrer Anwendung hinzu. Sie registriert die öffentlichen DWD-Endpunkte:

```csharp
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;

namespace MyWeatherApp;

/// <summary>
/// Registers WeatherParser and every endpoint and option type it needs. AddWeatherParsing registers the services of all sources, and container validation checks all of them, so every type below is required even when the application uses only one source.
/// </summary>
public static class WeatherParserRegistration
{
    /// <summary>
    /// Registers WeatherParser with the public DWD endpoints.
    /// </summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <param name="httpClient">The HTTP client owned by the application; the container does not dispose it.</param>
    /// <param name="timeProvider">TimeProvider.System in production.</param>
    /// <param name="evidenceCacheDirectory">An existing, writable absolute directory used only by the MOSMIX_S evidence cache.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddMyWeatherParsing(this IServiceCollection services, HttpClient httpClient, TimeProvider timeProvider, string evidenceCacheDirectory)
    {
        services.AddWeatherParsing();

        // Application-owned infrastructure; the library registers neither. Register ILogger<T> with AddLogging in the host.
        services.AddSingleton(httpClient);
        services.AddSingleton(timeProvider);

        // DWD 10-minute observations and CDC station selection.
        const string cdc = "https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/";
        services.AddSingleton(new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri(cdc + "air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt"),
            AirTemperatureNowDirectory = new Uri(cdc + "air_temperature/now/"),
            WindStationList = new Uri(cdc + "wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
            WindNowDirectory = new Uri(cdc + "wind/now/"),
            PrecipitationStationList = new Uri(cdc + "precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
            PrecipitationNowDirectory = new Uri(cdc + "precipitation/now/"),
            SolarStationList = new Uri(cdc + "solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
            SolarNowDirectory = new Uri(cdc + "solar/now/"),
        });
        services.AddSingleton(new DwdObservationOptions { ObservationDeadline = TimeSpan.FromMinutes(2) });

        // MOSMIX location selection.
        const string mosmix = "https://opendata.dwd.de/weather/local_forecasts/mos/";
        services.AddSingleton(new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_stationskatalog.cfg?view=nasPublication&nn=16102"),
            MosmixLSingleStationsDirectory = new Uri(mosmix + "MOSMIX_L/single_stations/"),
            MosmixSAllStationsUri = new Uri(mosmix + "MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
            EvidenceCacheDirectory = evidenceCacheDirectory,
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
        });

        // MOSMIX_L and MOSMIX_S forecasts; MaxRunAge has no default.
        services.AddSingleton(new MosmixLForecastEndpoints
        {
            SingleStationsDirectory = new Uri(mosmix + "MOSMIX_L/single_stations/"),
        });
        services.AddSingleton(new MosmixLForecastOptions
        {
            ParseDeadline = TimeSpan.FromSeconds(60),
            MaxRunAge = TimeSpan.FromHours(8),
        });
        services.AddSingleton(new MosmixSForecastEndpoints
        {
            AllStationsUri = new Uri(mosmix + "MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
        });
        services.AddSingleton(new MosmixSForecastOptions
        {
            ParseDeadline = TimeSpan.FromMinutes(5),
            MaxRunAge = TimeSpan.FromHours(2),
        });

        // OsthessenWetter observations: the values must not be redistributed until the source conditions are clarified. Add one Stations entry per station you parse.
        services.AddSingleton(new OsthessenWetterEndpoints
        {
            PageUri = new Uri("https://osthessen-wetter.de/messwerte/"),
        });
        services.AddSingleton(new OsthessenWetterOptions());

        // MQTT messages: add one mapping per message type you publish; without mappings every serialization fails with MQTT-MAPPING-MISSING.
        services.AddSingleton(new MqttTopicOptions());

        return services;
    }
}
```

| Registrierter Typ | Verwendet von | Hinweise |
| --- | --- | --- |
| `HttpClient` | jede Quelle | Gehört Ihrer Anwendung. Die Bibliothek begrenzt jede Anfrage selbst (30 s, für `MOSMIX_S` die Parse-Frist) und wiederholt nicht. Handler-Einstellungen wie Proxy, Weiterleitungen und Dekomprimierung liegen bei Ihnen. Setzen Sie einen identifizierenden User-Agent. |
| `TimeProvider` | jede Quelle | `TimeProvider.System` im Produktivbetrieb. Er setzt `retrieved_at` und die Fristen. |
| `ILogger<T>` | jede Quelle | Registrieren Sie ihn mit `AddLogging`. Hinweise und Auslassungen werden mit stabilen Ereignisnamen protokolliert. |
| `CdcCatalogEndpoints` | CDC-Stationsauswahl, DWD-Beobachtungen | Listen-URIs werden unverändert verwendet. Verzeichnis-URIs müssen mit `/` enden. |
| `DwdObservationOptions` | DWD-Beobachtungen | `ObservationDeadline`, Standard 2 Minuten, höchstens 1 Stunde. |
| `MosmixCatalogEndpoints` | MOSMIX-Standortauswahl | `EvidenceCacheDirectory` muss ein existierendes, beschreibbares absolutes Verzeichnis sein, das nur der MOSMIX_S-Nachweiscache verwendet. `MosmixSEvidenceMaxAge` hat keinen Standard. |
| `MosmixLForecastEndpoints`, `MosmixLForecastOptions` | `IMosmixLParser` | `MaxRunAge` hat keinen Standard. |
| `MosmixSForecastEndpoints`, `MosmixSForecastOptions` | `IMosmixSParser` | `MaxRunAge` hat keinen Standard. Jeder Parse-Vorgang lädt die ganze Datei aller Stationen herunter (etwa 36 MB). |
| `OsthessenWetterEndpoints`, `OsthessenWetterOptions` | `IOsthessenWetterParser` | `Stations` liefert die Koordinaten jeder Station, die Sie parsen. Die Werte dürfen nicht weiterverbreitet werden (Abschnitt 9). |
| `MqttTopicOptions` | `IMqttMessageSerializer` | Topic-Vorlagen je Nachrichtentyp. Eine leere Instanz ist gültig, lässt aber jede Serialisierung fehlschlagen. |

Die Container-Validierung prüft, dass ein Typ registriert ist, nicht dass seine Werte gültig sind. Ungültige Optionswerte zeigen sich, wenn ein Dienst zum ersten Mal aufgelöst wird: `MOSMIX-OPTIONS-INVALID`, `OSTHESSEN-OPTIONS-INVALID`, `MQTT-OPTIONS-INVALID`, `ENDPOINT-INVALID` und die Grenzfehler. Lösen Sie die verwendeten Parser einmal beim Start auf, wenn Sie diese Fehler vor dem ersten Parse-Vorgang sehen möchten. Das Bauen des Containers und das Auflösen von Diensten stellen keine Anfrage.

## 4. Den Parser in einer Anwendungsklasse verwenden

Anwendungsklassen erhalten die Schnittstellen der Bibliothek über ihre Konstruktoren. Nur der Kompositionsstamm (`Program.cs`) löst Dienste aus dem Container auf. Die Schnittstelle:

```csharp
namespace MyWeatherApp;

/// <summary>
/// Writes the current DWD observations of one station as one JSON document.
/// </summary>
public interface IDwdJsonExporter
{
    /// <summary>
    /// Parses the observations of the station and writes one complete JSON document to <paramref name="destination"/>.
    /// </summary>
    /// <param name="stationId">The five-digit CDC station ID, kept as a string with leading zeros, for example 00044.</param>
    /// <param name="destination">The stream that receives the JSON document; the caller owns and disposes it.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task ExportAsync(string stationId, Stream destination, CancellationToken cancellationToken);
}
```

Die Implementierung parst eine Station und serialisiert den Datensatz:

```csharp
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Serialization;

namespace MyWeatherApp;

/// <summary>
/// Parses the DWD observations of one station and writes them as JSON. The parser and the serializer come from the container through constructor injection.
/// </summary>
public sealed class DwdJsonExporter : IDwdJsonExporter
{
    private readonly IDwdObservationParser _parser;
    private readonly IJsonExportSerializer _serializer;

    public DwdJsonExporter(IDwdObservationParser parser, IJsonExportSerializer serializer)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc />
    public async Task ExportAsync(string stationId, Stream destination, CancellationToken cancellationToken)
    {
        var dataset = await _parser.ParseAsync(stationId, cancellationToken).ConfigureAwait(false);
        await _serializer.SerializeAsync(dataset, destination, cancellationToken).ConfigureAwait(false);
    }
}
```

`IDwdObservationParser.ParseAsync` prüft die Stations-ID (fünf ASCII-Ziffern) vor jeder Anfrage, liest die vier CDC-Stationslisten und die `now`-Archive der Station (höchstens acht Anfragen) und liefert ein `ObservationDataset`. `IJsonExportSerializer.SerializeAsync` schreibt ein vollständiges UTF-8-JSON-Dokument in den Stream des Aufrufers. `IXmlExportSerializer` schreibt die XML-Form derselben Entitäten, gültig gegen das im Paket enthaltene Schema `schemas/export/v1/weatherparser-export-v1.xsd`.

## 5. Der Kompositionsstamm

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyWeatherApp;

// Station IDs stay strings; leading zeros matter.
var stationId = args.Length > 0 ? args[0] : "00044";

// The MOSMIX_S evidence cache needs an existing writable directory.
var evidenceCacheDirectory = Path.Combine(AppContext.BaseDirectory, "mosmix-evidence");
Directory.CreateDirectory(evidenceCacheDirectory);

// The library bounds every request itself; the application owns and disposes the client.
using var httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
// Identify the application to the source servers.
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MyWeatherApp/1.0");

var services = new ServiceCollection();
// Logs go to stderr, so stdout carries only the JSON document.
services.AddLogging(logging => logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace));
services.AddMyWeatherParsing(httpClient, TimeProvider.System, evidenceCacheDirectory);
services.AddSingleton<IDwdJsonExporter, DwdJsonExporter>();

// A missing registration fails here, before any request.
await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

// Ctrl+C and the overall time limit both cancel the parse.
using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(3));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };

// Resolve only at the composition root; application classes get their dependencies through their constructors.
var exporter = provider.GetRequiredService<IDwdJsonExporter>();
await using var stdout = Console.OpenStandardOutput();
await exporter.ExportAsync(stationId, stdout, cancellation.Token);
```

Starten Sie die Anwendung mit einer Stations-ID, zum Beispiel `dotnet run -- 00044`. Das kontaktiert den Open-Data-Server des DWD: Beachten Sie die Bedingungen in Abschnitt 9.

## 6. Das Ergebnis

Für die Offline-Testdaten aus Abschnitt 10 (synthetische Werte für die Station `00044`, abgerufen am 2026-10-21 um 12:00 UTC) schreibt der Serialisierer das folgende Dokument. Es ist hier eingerückt dargestellt; der Serialisierer selbst schreibt es ohne Einrückung. Das sind keine echten Messwerte.

```json
{
  "schema_version": "1",
  "source": "dwd:00044",
  "location": {
    "station_id": "00044",
    "name": "Grossenkneten",
    "latitude": 52.9336,
    "longitude": 8.2370,
    "altitude_m": 44
  },
  "measured_at": "2026-10-21T11:50:00",
  "retrieved_at": "2026-10-21T12:00:00",
  "timestamp_quality": "source",
  "measurements": {
    "temperature_c": {
      "value": 12.3,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "quality": "3"
    },
    "pressure_hpa": {
      "value": 1001,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "quality": "3",
      "pressure_reference": "sensor"
    },
    "pressure_mmhg": {
      "value": 751,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "quality": "3",
      "pressure_reference": "sensor"
    },
    "humidity_percent": {
      "value": 85,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "quality": "3"
    },
    "wind_direction_deg": {
      "value": 270,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "period": {
        "start": "2026-10-21T11:40:00",
        "end": "2026-10-21T11:50:00"
      },
      "quality": "3"
    },
    "wind_speed_kmh": {
      "value": 12,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "period": {
        "start": "2026-10-21T11:40:00",
        "end": "2026-10-21T11:50:00"
      },
      "quality": "3"
    },
    "solar_irradiance_w_m2": {
      "value": 600,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "period": {
        "start": "2026-10-21T11:40:00",
        "end": "2026-10-21T11:50:00"
      },
      "quality": "3"
    },
    "rain_rate_mm_h": {
      "value": 0,
      "measured_at": "2026-10-21T11:50:00",
      "timestamp_quality": "source",
      "period": {
        "start": "2026-10-21T11:40:00",
        "end": "2026-10-21T11:50:00"
      },
      "quality": "3"
    }
  }
}
```

Was das Dokument aussagt:

- **Zeiten** sind UTC in der Form `YYYY-MM-DDTHH:mm:ss` ohne `Z` oder Offset. Jede Größe hat ihr eigenes `measured_at`. Das `measured_at` des Datensatzes ist das neueste davon, und `retrieved_at` ist der Zeitpunkt, zu dem die Bibliothek die Daten gelesen hat. Keines davon garantiert Aktualität: Eine Station, die nicht mehr meldet, liefert weiterhin ihre alten Werte; prüfen Sie `measured_at` also selbst.
- **Fehlend gegenüber null**: Eine Größe, die die Quelle nicht liefert oder als fehlend kennzeichnet, wird weggelassen. In diesem Beispiel haben die Testdaten keine Beleuchtungsstärke (keine DWD-10-Minuten-Quelle) und deshalb kein `illuminance_lux`. `0` ist ein echter Wert, wie `rain_rate_mm_h` zeigt.
- **Zeiträume**: Gemittelte und summierte Größen tragen ihren `period`. Hier ist die Rate die Millimeter der vorangegangenen 10 Minuten mal 6, und die Strahlung ist die 10-Minuten-Energiesumme geteilt durch 600 s.
- **Luftdruck**: `pressure_hpa` und `pressure_mmhg` stammen aus demselben Originalwert und nennen ihre `pressure_reference` (`sensor`: Stationshöhe, nicht auf Meereshöhe reduziert).
- **Qualität**: `quality` ist die DWD-Qualitätsstufe der Zeile, unverändert.
- **Rundung** erfolgt nach der Umrechnung: 0,1 °C, 1 hPa und 1 mmHg, 1 %, 1 km/h, exakte Mitten von null weg gerundet.
- **Schemastabilität**: Die Exportformatversion 1 (`schema_version` `"1"`) ändert sich nur durch optionale Ergänzungen. Validieren Sie XML mit dem Schema der Paketversion, die es erzeugt hat.

## 7. Fehler, Abbruch und Wiederholungen

| Situation | Was Sie erhalten |
| --- | --- |
| Ungültige Stations-ID | `DwdStationException`, `ErrorId` `STATION-ID-INVALID`, vor jeder Anfrage |
| Station in keiner Liste | `DwdStationException` `STATION-UNKNOWN` |
| Serverfehler, Timeout, zu große Datei | `CatalogRetrievalException` `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, `CATALOG-TOO-LARGE` |
| Unlesbare Liste oder unlesbares Archiv | `CatalogFormatException` `CATALOG-FORMAT-INVALID`, `CDC-EVIDENCE-INVALID` |
| Der Parse-Vorgang dauerte länger als `ObservationDeadline` | `ObservationLimitException` `OBSERVATION-DEADLINE-EXCEEDED` |
| Überhaupt kein nutzbarer Messwert | `ObservationDatasetRejectedException`; `OmittedReadings` listet die Gründe |
| Ein Produktarchiv fehlt (HTTP 404) | Keine Ausnahme. Die anderen Produkte werden verwendet, und eine Warnung `OBSERVATION-PRODUCT-MISSING` wird protokolliert. |
| Ihr Token wird abgebrochen | `OperationCanceledException`. Sie hat Vorrang vor der Frist, die wiederum Vorrang vor jedem anderen Fehler hat. |

Die Ausnahmen der Bibliothek haben keine gemeinsame Basisklasse. `DwdStationException`, `CatalogRetrievalException`, `CatalogFormatException`, `ObservationLimitException` und die anderen Quellausnahmen tragen eine stabile `ErrorId` (die vollständigen Listen stehen in der Bibliotheksreferenz der README); `ObservationDatasetRejectedException` hat keine `ErrorId` und listet die Gründe in `OmittedReadings`, und `OperationCanceledException` ist der Abbruch des Frameworks. Die Bibliothek wiederholt nie und hat keinen Scheduler. Ein Aufruf ist ein Abruf. Ob und wann wiederholt wird und wie oft geparst wird, entscheidet Ihre Anwendung: Für DWD-10-Minuten-Daten empfiehlt das Projekt höchstens einen Parse-Vorgang je Station alle 10 Minuten. Behandeln Sie `STATION-ID-INVALID` und die Optionsfehler als Konfigurationsfehler, nicht als Anlass für eine Wiederholung.

## 8. Lebensdauern und Threads

Alle Bibliotheksdienste sind Singletons ohne Zustand je Aufruf. Die Pakettests führen die Parser jeder Quelle, eine CDC-Stationsauswahl und die JSON-, XML- und MQTT-Serialisierer parallel aus und vergleichen die Ergebnisse mit sequenziellen Läufen. Ihr registrierter `HttpClient`, `TimeProvider` und Ihre Logger müssen deshalb für gleichzeitige Verwendung sicher sein, wie es die Framework-Implementierungen sind. Registrieren Sie Ihre eigenen Anwendungsklassen mit jeder passenden Lebensdauer; das Beispiel verwendet ein Singleton.

## 9. Die anderen Quellen und MQTT

Jede Quelle hat ihre eigene Schnittstelle, Signatur und ihr eigenes Fehlermodell. Sie werden mit demselben Aufruf `AddMyWeatherParsing` von oben registriert.

| Schnittstelle | Aufruf | Ergebnis | Teilergebnisse |
| --- | --- | --- | --- |
| `ICdcStationSelector`, `IMosmixLocationSelector` | `SelectAsync(SearchParameters, CancellationToken)` | `CdcStationList`, `MosmixLocationList` | CDC-Einträge listen ihre verfügbaren Messgrößen; jeder MOSMIX-Eintrag nennt die Verfügbarkeit je Produkt als `available`, `unavailable` oder `unknown` |
| `IDwdObservationParser` | `ParseAsync(string stationId, CancellationToken)` | `ObservationDataset` | Ein fehlendes Produkt wird mit einer Warnung übersprungen |
| `IOsthessenWetterParser` | `ParseAsync(string stationNamePattern, CancellationToken)` | `OsthessenWetterParseResult` mit `Datasets` und `StationFailures` | Je Station; der Aufruf schlägt nur fehl, wenn keine passende Station nutzbar ist |
| `IMosmixLParser`, `IMosmixSParser` | `ParseAsync(string mosmixId, CancellationToken)` | `MosmixParseResult` mit `Run`, `Freshness`, `RunAge` und den weggelassenen und nicht abgebildeten Elementen | Ein veralteter Lauf wird mit `Freshness` `Stale` zurückgegeben; nie ein Ausweichen auf das andere Produkt |
| `IMqttMessageSerializer` | `SerializeAsync<TRoot>(TRoot root, CancellationToken)` | `MqttMessage` mit Topic, JSON-Nutzdaten, QoS und Retain | Keine; er verbindet sich nicht mit einem Broker |

- **Stations- und Standortauswahl** benötigt `SearchParameters` aus `ISearchParameterFactory.Create(latitude, longitude, altitudeM, maxDistanceKm, maxAltitudeDifferenceM)`; `ICoordinateParser` liest Dezimalgrad sowie Grad, Minuten und Sekunden. Die MOSMIX_S-Verfügbarkeit stammt aus einem lokalen Index, den Ihre Anwendung nach eigenem Zeitplan mit `IMosmixSEvidenceRefresher.RefreshAsync` aktualisiert. Führen Sie eine Auswahl aus, wenn eine Station gewählt wird, nicht nach Zeitplan. Referenz: [README, Stations- und Standortauswahl](README.md#stations--und-standortauswahl-bibliothek).
- **OsthessenWetter** prüft das Muster (`*` und `?`) gegen die Stationsnamen der Messwertseite; fügen Sie für jede Station einen Eintrag `OsthessenWetterStationMetadata` mit ihren Koordinaten zu `OsthessenWetterOptions.Stations` hinzu. Datumsangaben werden abgeleitet und als `estimated_date` gekennzeichnet. Die Quelle veröffentlicht keine Lizenz: Produktive Nutzung und Weiterverbreitung ihrer Werte, zum Beispiel über MQTT, werden nicht akzeptiert, bis die Quellenbedingungen geklärt sind. Referenz: [README, OsthessenWetter-Beobachtungen](README.md#osthessenwetter-beobachtungen-bibliothek).
- **MOSMIX-Vorhersagen** sind Vorhersageläufe mit `issued_at`, `retrieved_at` und einem `valid_at` je Schritt, niemals Beobachtungen. Die MOSMIX-ID ist eine Katalog-ID aus vier oder fünf Großbuchstaben oder Ziffern, wie `10637`, und hat nichts mit CDC-Stations-IDs zu tun. Referenz: [README, MOSMIX-Vorhersagen](README.md#mosmix-vorhersagen-bibliothek).
- **Die Ergebnisse serialisieren**: Der JSON- und der XML-Serialisierer akzeptieren genau die Export-Wurzeln `ObservationDataset`, `ForecastRun`, `CdcStationList`, `MosmixLocationList` und den Umschlag für mehrere Ergebnisse `ExportDocument`; jeder andere Typ schlägt mit `ExportSerializationException` fehl. Serialisieren Sie jeden Datensatz in `OsthessenWetterParseResult.Datasets` und den `Run` eines `MosmixParseResult`, und behandeln Sie `StationFailures`, `Freshness` und die weggelassenen Elemente in Ihrer Anwendung. Der MQTT-Serialisierer akzeptiert die vier Wurzeln außer `ExportDocument`.
- **MQTT**: Konfigurieren Sie in `MqttTopicOptions` eine Topic-Vorlage je Nachrichtentyp, zum Beispiel `haus/weather/measurements/{source}/state` für Beobachtungen; das Veröffentlichen der entstehenden `MqttMessage` mit einem MQTT-Client Ihrer Wahl ist Aufgabe Ihrer Anwendung. Referenz: [README, MQTT-Serialisierer](README.md#mqtt-serialisierer-bibliothek).

Quellenbedingungen: DWD-Daten sind offene Daten unter CC BY 4.0. Nennen Sie „Deutscher Wetterdienst (DWD)“ überall, wo Sie sie anzeigen oder weiterveröffentlichen. Der DWD speichert die anfragende IP-Adresse höchstens 7 Tage. Die MIT-Lizenz dieser Bibliothek umfasst nur den Code, nicht die Daten.

## 10. Wie diese Anleitung verifiziert wird

- `tests/WeatherParser.Package.Tests/LibraryUsageExampleTests.cs` kompiliert die drei Dateien der Abschnitte 3 und 4 gegen die gepackte Bibliothek und baut den Container mit `ValidateOnBuild` und `ValidateScopes`. Der Test löst jeden öffentlichen Einstiegspunkt ohne Anfrage auf, führt den Export offline gegen einen Fake-HTTP-Handler aus, der für die DWD-URIs aus Abschnitt 3 antwortet, und prüft das Ergebnis. Er prüft außerdem, dass die englische Fassung dieser Anleitung die drei Dateien wörtlich zeigt und dass das JSON aus Abschnitt 6 der Ausgabe des Serialisierers entspricht. Die Tests laufen in jedem CI-Lauf, der Code baut. Die Codebeispiele dieser Übersetzung sind mit denen der englischen Fassung identisch.
- `Program.cs` aus Abschnitt 5 und die Paketbefehle aus Abschnitt 2 wurden am 2026-11-02 in einer neuen Konsolenanwendung außerhalb des Repositorys mit leerem Paket-Cache geprüft: Sie stellte das lokale Paket `0.1.0` und seine Abhängigkeiten wieder her und baute ohne Warnungen. Diese Anwendung wurde nicht ausgeführt, weil die Ausführung den DWD-Server kontaktiert; das Live-Verhalten der Parser ist durch die genehmigten Live-Prüfungen abgedeckt, die in den Phasenprotokollen festgehalten sind ([PHASE_STATUS.md](../development/PHASE_STATUS.md), Englisch), nicht durch diese Anleitung.
