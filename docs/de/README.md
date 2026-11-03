# WeatherParser

> Deutsche Übersetzung der englischen [README.md](../../README.md). Maßgeblich ist die englische Fassung; die Übersetzung wird vor jeder neuen Version mit ihr abgeglichen und kann bis dahin zurückliegen. Übersetzungsstand: englische Fassung von `features/develop/release-1.0.0` `8659f5b` (2026-11-03, Version 1.0.0). Die Abschnitte für Mitwirkende sind nicht übersetzt (siehe [Mitwirken und Entwicklung](#mitwirken-und-entwicklung)).

WeatherParser ist eine .NET-10-Bibliothek, die Wetterstationen in der Nähe eines Standorts findet, aktuelle Beobachtungen und Punktvorhersagen des Deutschen Wetterdienstes (DWD) und von OsthessenWetter liest, die Werte auf einen gemeinsamen Datenvertrag mit UTC-Zeiten normalisiert und die Ergebnisse als JSON- oder XML-Dokumente oder als MQTT-Nachrichten schreibt. Sie ist ein NuGet-Paket (`WeatherParser.Parsing`) für Ihre eigene Anwendung; ein optionales Beispiel-Kommandozeilenprogramm zeigt die Verwendung.

Für den Einstieg in die Bibliothek beginnen Sie mit [Erste Schritte](#erste-schritte) und der vollständigen Anleitung in [LIBRARY_USAGE.md](LIBRARY_USAGE.md).

Das Repository <https://github.com/witalij-schrajer/WeatherParser> ist öffentlich.

## Fähigkeiten

| Was Sie möchten | Schnittstelle | Ergebnis |
| --- | --- | --- |
| DWD-10-Minuten-Stationen rund um einen Punkt finden, mit ihren verfügbaren Messgrößen | `ICdcStationSelector` | `CdcStationList` |
| MOSMIX-Vorhersagestandorte rund um einen Punkt finden, mit ihrer Verfügbarkeit je Produkt | `IMosmixLocationSelector` | `MosmixLocationList` |
| Aktuelle DWD-10-Minuten-Beobachtungen einer Station | `IDwdObservationParser` | `ObservationDataset` |
| Aktuelle OsthessenWetter-Beobachtungen der passenden Stationen | `IOsthessenWetterParser` | `OsthessenWetterParseResult` |
| Den neuesten MOSMIX_L- oder MOSMIX_S-Vorhersagelauf eines Standorts | `IMosmixLParser`, `IMosmixSParser` | `MosmixParseResult` mit einem `ForecastRun` |
| Ein vollständiges JSON- oder XML-Dokument einer Export-Wurzel: `ObservationDataset`, `ForecastRun`, `CdcStationList`, `MosmixLocationList` oder der Umschlag für mehrere Ergebnisse `ExportDocument` | `IJsonExportSerializer`, `IXmlExportSerializer` | Das Dokument in Ihrem Stream; XML gültig gegen das mitgelieferte Schema. Serialisieren Sie die `Datasets` eines `OsthessenWetterParseResult` und den `Run` eines `MosmixParseResult`, nicht die Ergebnisobjekte selbst |
| Eine MQTT-Nachricht mit Topic, JSON-Nutzdaten, QoS und Retain | `IMqttMessageSerializer` | `MqttMessage`; das Veröffentlichen ist Aufgabe Ihrer Anwendung, die Bibliothek verbindet sich nie mit einem Broker |

Alle werden mit einem Aufruf registriert, `AddWeatherParsing()`, und erhalten ihre Endpunkte und Optionen von Ihrer Anwendung. Die Bibliothek hat keinen Scheduler: ein Aufruf ist ein Abruf, und Ihre Anwendung entscheidet, wann sie aufruft. Die Beispiel-CLI (`WeatherParser`, siehe [Beispiel-CLI](#beispiel-cli)) führt pro Aufruf genau eine dieser Operationen aus und wird aus dem Quellcode gebaut; sie ist nicht Teil des Pakets.

## Projektstatus

**Version 1.0.0 (Stand 2026-11-03).** Die öffentliche API folgt der semantischen Versionierung: Innerhalb von 1.x ändert sie sich nur kompatibel; eine inkompatible Änderung erfordert die Version 2.0.0.

- **Bibliothek** `WeatherParser.Parsing`: Stations- und Standortauswahl, DWD-10-Minuten-Beobachtungen, OsthessenWetter-Beobachtungen, MOSMIX_L- und MOSMIX_S-Vorhersagen, JSON- und XML-Export mit Ausgabeschema, ein MQTT-Nachrichten-Serialisierer und DI-Registrierung (`AddWeatherParsing`). Das Exportformat Version 1 ist stabil; es erhält nur additive, optionale Änderungen.
- **Beispiel-CLI** `WeatherParser`: die fünf Befehle mit JSON-, XML- und MQTT-Ausgabe (siehe [Beispiel-CLI](#beispiel-cli)).
- **Verifiziert** unter Windows, in der gehosteten CI und auf dem Zielsystem, einem NanoPi R76S mit Debian 13 ARM64 und .NET 10, einschließlich zeitgesteuerter Läufe gegen die Live-Quellen.
- **Verteilung**: Die Paketdatei ist dem GitHub-Release `v1.0.0` mit ihren `SHA256SUMS` beigefügt; eine Veröffentlichung auf nuget.org ist eine eigene Entscheidung des Maintainers (siehe [Release und Installation](#release-und-installation)).
- **Einschränkung**: OsthessenWetter-Werte dürfen nicht weiterverbreitet werden, bis die Quellenbedingungen geklärt sind (siehe [OsthessenWetter-Werte](#osthessenwetter-werte)).

Bekannte Einschränkungen stehen in [CHANGELOG.md](../../CHANGELOG.md) (Englisch); die Entwicklungshistorie steht in [PHASE_STATUS.md](../development/PHASE_STATUS.md) (Englisch).

## Erste Schritte

Sie benötigen das .NET-10-SDK, die Laufzeit 10.0.14 oder neuer und das Paket `WeatherParser.Parsing` 1.0.0. Entnehmen Sie die Datei `WeatherParser.Parsing.1.0.0.nupkg` dem GitHub-Release `v1.0.0` (geprüft gegen dessen `SHA256SUMS`) oder bauen Sie sie wie in [CONTRIBUTING.md Abschnitt 4](../../CONTRIBUTING.md#4-pack-and-package-consumer) (Englisch) beschrieben. Fügen Sie den Ordner, der sie enthält, als Paketquelle neben nuget.org hinzu, das die Abhängigkeiten der Bibliothek liefert:

```bash
dotnet new nugetconfig
dotnet nuget add source <Ordner mit der .nupkg> --name weatherparser-local --configfile nuget.config
dotnet add package WeatherParser.Parsing --version 1.0.0
dotnet add package Microsoft.Extensions.DependencyInjection --version 10.0.14
dotnet add package Microsoft.Extensions.Logging.Console --version 10.0.14
```

Auszug aus der Anleitung: Registrieren Sie die Bibliothek mit allen Endpunkt- und Optionstypen (hier im `AddMyWeatherParsing` der Anleitung zusammengefasst), bauen Sie den Container mit Validierung, dann parsen und serialisieren Sie:

```csharp
services.AddMyWeatherParsing(httpClient, TimeProvider.System, evidenceCacheDirectory);
await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
// In an application class with IDwdObservationParser parser and IJsonExportSerializer serializer injected:
var dataset = await _parser.ParseAsync(stationId, cancellationToken).ConfigureAwait(false);
await _serializer.SerializeAsync(dataset, destination, cancellationToken).ConfigureAwait(false);
```

`AddWeatherParsing()` registriert die Dienste aller Quellen, und die Container-Validierung prüft sie alle; Ihre Anwendung registriert deshalb jeden Endpunkt- und Optionstyp, auch für Quellen, die sie nicht nutzt. [LIBRARY_USAGE.md](LIBRARY_USAGE.md) enthält die vollständige Registrierung, die Anwendungsklasse, `Program.cs`, eine Beispielausgabe, die Fehlerbehandlung und die anderen Quellen; ihr Beispiel wird von den Pakettests offline kompiliert und ausgeführt.

## Gemeinsame Beobachtungsdaten

Alle Quellen verwenden denselben Datenvertrag. Zeitstempel sind immer **UTC im Format `YYYY-MM-DDTHH:mm:ss`, ohne `Z` oder Offset**. Konsumenten müssen diese Konvention ausdrücklich berücksichtigen; eine Umrechnung in Ortszeit erfolgt nur zur Anzeige.

Zu den Größen gehören Temperatur in °C, Luftdruck in hPa und mmHg, relative Luftfeuchte in %, Windrichtung in Grad, Windgeschwindigkeit in km/h, Beleuchtungsstärke in Lux, Globalstrahlung in W/m² und Regenintensität in mm/h. Fehlende Werte werden weggelassen. `0` ist ein tatsächlicher Messwert.

hPa und mmHg beziehen sich auf denselben Originalwert und dieselbe Druckreferenz. Lux und W/m² sind getrennte Größen. Gemittelte Werte behalten ihre Messzeiträume. Vorhersagen sind eigene Vorhersageläufe, niemals Beobachtungen.

Die untersuchte OsthessenWetter-Übersicht liefert je Station nur eine Uhrzeit. Das Projekt erlaubt, das Datum aus der Abrufzeit und der Quellzeitzone `Europe/Berlin` abzuleiten, dann nach UTC umzurechnen und **das Datum als geschätzt zu kennzeichnen**. Sommerzeitumstellungen und mehrdeutige Zeiten erfordern eine ausdrückliche Behandlung; das Abrufdatum beweist nicht, dass die Beobachtung aktuell ist.

## Eine Datenquelle hinzufügen

Die Bibliothek hat keinen Plugin-Mechanismus. Eine neue Quelle ist entweder ein Beitrag zur Bibliothek, registriert in `AddWeatherParsing`, oder ein Adapter in Ihrer eigenen Anwendung, der die öffentlichen Bausteine der Bibliothek verwendet (begrenzter Download, Zeitnormalisierung, Einheitenumrechnung, der Builder für Beobachtungsdatensätze) und ihre Exportentitäten erzeugt. Ein neuer Beobachtungsanbieter kann den bestehenden Vertrag nutzen; eine neue Größe oder ein anderer Vorhersageanbieter als MOSMIX erfordert eine Vertragsänderung. [EXTENDING_DATA_SOURCES.md](EXTENDING_DATA_SOURCES.md) nennt die zuerst festzuhaltenden Fakten der Quelle, die bestehenden Muster, die erforderlichen Tests und die Dokumentation.

## Ein Problem melden

Melden Sie Fehler und Wünsche nach Datenquellen in den [Issues dieses Repositorys](https://github.com/witalij-schrajer/WeatherParser/issues). [REPORTING_ISSUES.md](REPORTING_ISSUES.md) beschreibt, was eine hilfreiche Meldung enthält und was niemals veröffentlicht werden darf (Zugangsdaten, private Adressen, unlizenzierte Kopien von Quelldaten). Beschreiben Sie Sicherheitsprobleme nicht in einem Issue: Melden Sie sie vertraulich über die private Meldung von Sicherheitslücken von GitHub, wie es die Anleitung beschreibt.

## Bibliotheksreferenz

Die folgenden Abschnitte beschreiben jeden Teil der Bibliothek im Detail: Aufruf, Optionen und Grenzen, die exportierten Größen, Fehler- und Hinweis-IDs und die Quellenbedingungen. Sie sind die Referenz zu [LIBRARY_USAGE.md](LIBRARY_USAGE.md):

- [Stations- und Standortauswahl](#stations--und-standortauswahl-bibliothek)
- [DWD-Beobachtungen](#dwd-beobachtungen-bibliothek)
- [OsthessenWetter-Beobachtungen](#osthessenwetter-beobachtungen-bibliothek)
- [MOSMIX-Vorhersagen](#mosmix-vorhersagen-bibliothek)
- [MQTT-Serialisierer](#mqtt-serialisierer-bibliothek)

## Stations- und Standortauswahl (Bibliothek)

Die Bibliothek wählt CDC-10-Minuten-Stationen (`ICdcStationSelector`) und MOSMIX-Vorhersagestandorte (`IMosmixLocationSelector`) rund um einen Bezugspunkt aus. `SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)` liefert eine `CdcStationList` oder `MosmixLocationList` (Export-Wurzeln des Vertrags v1, serialisierbar mit `IJsonExportSerializer` / `IXmlExportSerializer`). Suchparameter entstehen mit `ISearchParameterFactory.Create(latitude, longitude, altitudeM, maxDistanceKm, maxAltitudeDifferenceM)`; Koordinatentexte (Dezimalgrad oder Grad, Minuten und Sekunden mit Hemisphäre) liest `ICoordinateParser`.

### Registrierung in der Anwendung

Die Ausschnitte dieses und der folgenden Abschnitte zeigen den Teil der Registrierung, den jede Quelle hinzufügt. Die vollständige Registrierung aller erforderlichen Typen, von den Pakettests kompiliert und validiert, steht in [LIBRARY_USAGE.md](LIBRARY_USAGE.md#3-die-bibliothek-registrieren).

```csharp
services.AddWeatherParsing();
services.AddLogging();                       // or any ILogger<T> registration
services.AddSingleton(new HttpClient { Timeout = Timeout.InfiniteTimeSpan });
services.AddSingleton(TimeProvider.System);
services.AddSingleton(new CdcCatalogEndpoints
{
    AirTemperatureStationList = new Uri("https://example.invalid/cdc/air_temperature/now/stations.txt"),
    AirTemperatureNowDirectory = new Uri("https://example.invalid/cdc/air_temperature/now/"),
    WindStationList = new Uri("https://example.invalid/cdc/wind/now/stations.txt"),
    WindNowDirectory = new Uri("https://example.invalid/cdc/wind/now/"),
    PrecipitationStationList = new Uri("https://example.invalid/cdc/precipitation/now/stations.txt"),
    PrecipitationNowDirectory = new Uri("https://example.invalid/cdc/precipitation/now/"),
    SolarStationList = new Uri("https://example.invalid/cdc/solar/now/stations.txt"),
    SolarNowDirectory = new Uri("https://example.invalid/cdc/solar/now/"),
});
services.AddSingleton(new MosmixCatalogEndpoints
{
    CatalogUri = new Uri("https://example.invalid/mosmix/stations.cfg"),
    MosmixLSingleStationsDirectory = new Uri("https://example.invalid/mosmix/single_stations/"),
    MosmixSAllStationsUri = new Uri("https://example.invalid/mosmix/MOSMIX_S_LATEST_240.kmz"),
    EvidenceCacheDirectory = "/var/lib/weatherparser/evidence",
    MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
});
```

- `AddWeatherParsing` registriert jeden Bibliotheksdienst als Singleton, aber keine `Uri`, keine Endpunktoptionen, keinen `HttpClient`, keinen `TimeProvider` und keinen Logger; die Anwendung stellt sie bereit, und eine fehlende Registrierung schlägt bei der Container-Validierung fehl (`ValidateOnBuild`).
- `services.AddLogging()` benötigt das Paket `Microsoft.Extensions.Logging` in der nutzenden Anwendung; die Bibliothek selbst hängt nur von `Microsoft.Extensions.Logging.Abstractions` ab.
- Listen- und Katalogendpunkte sind absolute HTTP(S)-URIs und werden unverändert verwendet (eine Query ist erlaubt); Verzeichnisendpunkte müssen mit `/` enden und dürfen keine Query und kein Fragment haben, sonst `ENDPOINT-INVALID` bei der Konstruktion; Dateinamen je Station werden an das Verzeichnis angehängt.
- Grenzen je Katalog: `MaxEvidenceCandidates` (Standard 100, 1 bis 10000) und `SelectionDeadline` (Standard 120 s, größer als null und höchstens 1 Stunde), bei der Konstruktion validiert (`SELECTION-LIMIT-INVALID`); mehr Kandidaten im Suchgebiet als `MaxEvidenceCandidates` schlagen vor jeder Anfrage je Station mit `SELECTION-TOO-MANY-CANDIDATES` fehl.

### HTTP-Transport

- Die Bibliothek stellt je Datei einen GET ohne Wiederholungen, begrenzt auf 30 s je Anfrage und auf 8 MiB je Liste oder Katalog, 1 MiB je Datei einer Station und 64 MiB für die MOSMIX_S-Datei aller Stationen.
- Handler-Einstellungen (Weiterleitungen, Dekomprimierung, Proxy, Header-Grenzen) liegen in der Verantwortung der Anwendung.
- Setzen Sie `HttpClient.Timeout` auf `Timeout.InfiniteTimeSpan` oder über 30 s – ein kürzeres Client-Timeout ist erlaubt, und sein Ablauf wird als `CATALOG-TIMEOUT` gemeldet.

### MOSMIX_S-Nachweiscache

- Die MOSMIX_S-Verfügbarkeit stammt aus einem gespeicherten Index, den `IMosmixSEvidenceRefresher.RefreshAsync(CancellationToken)` aus der Datei aller Stationen aufbaut.
- Die Anwendung ruft die Aktualisierung nach ihrem eigenen externen Zeitplan auf (zum Beispiel ein Cron-Job oder ein n8n-Workflow; beides ist keine Abhängigkeit, die Bibliothek hat keinen Scheduler).
- Einstellungen `MosmixSAllStationsUri` (absolute HTTP(S)-Datei-URI), `EvidenceCacheDirectory` (absoluter lokaler Pfad eines Verzeichnisses, das nur der Cache verwendet), `MosmixSEvidenceMaxAge` (Pflicht, kein Standard, 1 Minute bis 365 Tage) und `MosmixSRefreshDeadline` (Standard 10 Minuten, größer als null und höchstens 1 Stunde); eine fehlende oder außerhalb des Bereichs liegende Einstellung schlägt bei der Konstruktion mit `EVIDENCE-CACHE-SETTINGS-INVALID` fehl, ohne Dateisystemzugriff.
- Das Verzeichnis muss existieren und beschreibbar sein: Ein fehlendes oder nicht beschreibbares Verzeichnis wird erst bei der Verwendung erkannt, von der Aktualisierung als `EVIDENCE-CACHE-FAILED` und von einer Auswahl als fehlender Index (`MOSMIX-S-EVIDENCE-MISSING`).
- Eine Auswahl ohne nutzbaren Index schlägt vor jeder Anfrage mit `MOSMIX-S-EVIDENCE-MISSING` fehl.
- Ein Index, der älter als `MosmixSEvidenceMaxAge` ist, wird weiter verwendet, und eine Warnung `MOSMIX-S-EVIDENCE-STALE` nennt sein Alter und die Grenze.
- MOSMIX_L-Dateien werden je Standort im Suchgebiet abgerufen; HTTP 404 bedeutet `unavailable`.

### Fehler- und Hinweis-IDs

Ausnahmen (`ErrorId`):
- `CoordinateFormatException` `COORDINATE-INVALID`
- `SearchParameterException` `SEARCH-PARAMETER-INVALID`
- `CatalogFormatException` `CATALOG-FORMAT-INVALID`, `CDC-EVIDENCE-INVALID`, `MOSMIX-EVIDENCE-INVALID`
- `CatalogRetrievalException` `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, `CATALOG-TOO-LARGE`
- `SelectionLimitException` `SELECTION-TOO-MANY-CANDIDATES`, `SELECTION-DEADLINE-EXCEEDED`, `SELECTION-LIMIT-INVALID`
- `EvidenceCacheException` `EVIDENCE-CACHE-SETTINGS-INVALID`, `EVIDENCE-CACHE-FAILED`, `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`, `MOSMIX-S-EVIDENCE-MISSING`
- `ParserEndpointValidationException` `ENDPOINT-INVALID`

Hinweise (Namen der Log-Ereignisse):
- `SELECTION-EMPTY` (Information)
- `MOSMIX-LOCATION-NO-ALTITUDE`, `CDC-STATION-NO-ALTITUDE`, `CATALOG-RECORD-SKIPPED`, `CDC-STATION-CONFLICT`, `MOSMIX-S-EVIDENCE-STALE` (Warning)

Ein Abbruch durch den Aufrufer erscheint als `OperationCanceledException` und hat Vorrang vor der Auswahlfrist.

### Quellenbedingungen und Abrufhinweise

- Die Daten stammen vom Open-Data-Server [opendata.dwd.de](https://opendata.dwd.de/) des Deutschen Wetterdienstes (DWD) unter [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) gemäß den [Rechtlichen Hinweisen des DWD](https://www.dwd.de/EN/service/legal_notice/legal_notice_node.html).
- Wer Auswahlergebnisse oder andere vom DWD abgeleitete Daten weiterveröffentlicht (zum Beispiel über MQTT oder als Dateiexport), muss die Quellenangabe „Deutscher Wetterdienst (DWD)“ beibehalten.
- Der DWD speichert die anfragende IP-Adresse höchstens 7 Tage.
- Eine Auswahl ist ein Vorgang zur Konfigurationszeit – führen Sie sie aus, wenn eine Station oder ein Standort gewählt oder überprüft wird, nicht nach Zeitplan, und bei Wiederholung höchstens einmal pro Tag und Suche.
- Die Nachweisanfragen je Station gehören zu diesem Lauf und sind nicht zum Abfragen in Intervallen gedacht (Empfehlung des Projekts, keine Zusicherung des Anbieters).

## DWD-Beobachtungen (Bibliothek)

Die Bibliothek parst die DWD-10-Minuten-`now`-Beobachtungen einer ausdrücklich angegebenen CDC-Station (`IDwdObservationParser`). `ParseAsync(string stationId, CancellationToken cancellationToken)` liefert ein `ObservationDataset` (Export-Wurzel des Vertrags v1, serialisierbar mit `IJsonExportSerializer` / `IXmlExportSerializer`). Die Stations-ID hat genau fünf ASCII-Ziffern mit führenden Nullen, zum Beispiel `00044`; sie wird vor jeder Anfrage geprüft, und die Bibliothek ersetzt sie nie durch eine andere oder die nächstgelegene Station. Ein Aufruf ist ein Parse-Vorgang: Die Bibliothek liest die vier CDC-Stationslisten, löst die Station auf und liest das `now`-Archiv der Station für jedes Produkt, dessen Liste sie enthält. Es gibt keinen Scheduler und keine Wiederholung; die Zeitplanung bleibt extern. Die Beispiel-CLI führt dies mit `--ParseDWD` aus (siehe [Beispiel-CLI](#beispiel-cli)).

### Registrierung in der Anwendung

Die Registrierung aus [Stations- und Standortauswahl](#stations--und-standortauswahl-bibliothek) wird um die Beobachtungsoptionen ergänzt:

```csharp
services.AddSingleton(new DwdObservationOptions
{
    ObservationDeadline = TimeSpan.FromSeconds(120),
});
```

- `DwdObservationOptions` muss von jeder Anwendung von `AddWeatherParsing` registriert werden, auch von einer, die nur Stationen auswählt, weil die Container-Validierung (`ValidateOnBuild`) die Abhängigkeiten des Parsers prüft; `new DwdObservationOptions()` liefert den Standard.
- `ObservationDeadline` (Standard 120 s) begrenzt einen Parse-Vorgang: Sie muss größer als null und höchstens 1 Stunde sein, sonst schlägt die Konstruktion mit `OBSERVATION-LIMIT-INVALID` fehl (null, negative Werte, `Timeout.InfiniteTimeSpan` und Werte über 1 Stunde werden abgelehnt); ihr Ablauf lässt den Parse-Vorgang mit `OBSERVATION-DEADLINE-EXCEEDED` fehlschlagen.
- Der Parser verwendet die acht URIs von `CdcCatalogEndpoints` wieder; deren `SelectionDeadline` und `MaxEvidenceCandidates` gelten nicht für Beobachtungen.
- Höchstens acht Anfragen je Parse-Vorgang: die vier Stationslisten und höchstens vier Archive (`10minutenwerte_TU_<id>_now.zip`, `..._wind_...`, `..._nieder_...`, `..._SOLAR_...` unterhalb der `now`-Verzeichnisse), höchstens vier parallel, mit den Grenzen aus [HTTP-Transport](#http-transport). Ein Archiv, das mit HTTP 404 antwortet, ist ein fehlendes Produkt (`OBSERVATION-PRODUCT-MISSING`), und die anderen Produkte werden weiter verwendet; jeder andere Abruf- oder Formatfehler lässt den Parse-Vorgang fehlschlagen.

### Exportierte Größen

| DWD-Spalte | Exportfeld | Umrechnung und Zeitraum |
| --- | --- | --- |
| `TT_10` (°C, 2 m, momentan) | `temperature_c` | keine; kein Zeitraum |
| `RF_10` (%) | `humidity_percent` | keine; kein Zeitraum |
| `PP_10` (hPa in Stationshöhe) | `pressure_hpa`, `pressure_mmhg` | `pressure_reference` `sensor`; beide aus einem Wert; kein Zeitraum |
| `FF_10` (m/s, Mittel der vorangegangenen 10 Minuten) | `wind_speed_kmh` | × 3,6; Zeitraum der vorangegangenen 10 Minuten |
| `DD_10` (Grad, Mittel der vorangegangenen 10 Minuten) | `wind_direction_deg` | 360 wird als 0 (Nord) exportiert; Zeitraum der vorangegangenen 10 Minuten |
| `RWS_10` (mm, Summe der vorangegangenen 10 Minuten) | `rain_rate_mm_h` | mm je 10 Minuten × 6; Zeitraum der vorangegangenen 10 Minuten |
| `GS_10` (J/cm², Summe der vorangegangenen 10 Minuten) | `solar_irradiance_w_m2` | × 10000 nach J/m², ÷ 600 s; Zeitraum der vorangegangenen 10 Minuten |

- Jede Größe nimmt die neueste Zeile mit einem nutzbaren Wert und behält ihr eigenes `measured_at` (UTC, aus `MESS_DATUM`); das `measured_at` des Datensatzes ist das neueste davon. Es gibt keinen Aktualitätsfilter: Das Archiv einer Station, die nicht mehr meldet, liefert weiterhin ihre alten Werte; Konsumenten bewerten deshalb `measured_at`.
- `-999` in jeder Schreibweise ist ein fehlender Wert, und die Größe wird weggelassen (`OBSERVATION-READING-OMITTED`); `0` ist ein echter Wert.
- Die Windrichtung wird weggelassen, wenn die Windgeschwindigkeit derselben Zeile 0 ist (Windstille) oder die Richtung 0 ist, weil die DWD-Daten Nord als 360 schreiben und keine Kennung für Windstille dokumentieren (eine Annahme des Projekts).
- `quality` ist die DWD-Qualitätsstufe `QN` der Zeile, unverändert: `1` nur formale Kontrolle, `2` mit individuell festgelegten Kriterien kontrolliert, `3` automatische Kontrolle und Korrektur ([DWD-Datensatzbeschreibungen](#datenquellen)); Daten mit `QN` 1 können erhebliche Fehler enthalten.
- Nicht exportiert: `TM5_10`, `TD_10`, `RWS_DAU_10`, `RWS_IND_10`, `DS_10`, `SD_10`, `LS_10` (kein Exportfeld) und `illuminance_lux` (keine DWD-10-Minuten-Quelle).
- Einschränkungen: Die Qualitätskontrolle der `now`-Daten ist noch nicht abgeschlossen, Werte können sich also noch ändern; der Zeitstempel von `RWS_10` hängt vom Messgerät ab (bei einem Pluvio-Ott-Niederschlagsmesser endet das physikalische Intervall etwa 5 Minuten früher) und wird nicht korrigiert – der exportierte Zeitraum ist der nominelle.

### Fehler- und Hinweis-IDs

Ausnahmen (`ErrorId`):
- `DwdStationException` `STATION-ID-INVALID` (vor jeder Anfrage), `STATION-UNKNOWN` (die Station steht in keiner Liste; keine Archivanfrage), `STATION-METADATA-CONFLICT` (die Einträge der Station in den Listen weichen voneinander ab, ihr Standort lässt sich also nicht angeben)
- `ObservationLimitException` `OBSERVATION-LIMIT-INVALID`, `OBSERVATION-DEADLINE-EXCEEDED`
- aus der Auswahl wiederverwendet: `CatalogRetrievalException` `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, `CATALOG-TOO-LARGE`; `CatalogFormatException` `CATALOG-FORMAT-INVALID`, `CDC-EVIDENCE-INVALID`; `ParserEndpointValidationException` `ENDPOINT-INVALID`
- `ObservationDatasetRejectedException`, wenn kein Messwert nutzbar ist (keine `ErrorId`; `OmittedReadings` listet die weggelassenen Größen mit ihren Gründen)

Hinweise (Namen der Log-Ereignisse, Warning): `OBSERVATION-PRODUCT-MISSING`, `OBSERVATION-READING-OMITTED` (Größe und Grund, einschließlich `CalmWind` und `ZeroDirection`), `CDC-STATION-CONFLICT`, `CATALOG-RECORD-SKIPPED`. Log-Meldungen enthalten die Stations-ID und die Abrufzeit in UTC, niemals Messwerte.

Ein Abbruch durch den Aufrufer erscheint als `OperationCanceledException` und hat Vorrang vor der Beobachtungsfrist, die wiederum Vorrang vor jedem anderen Fehler hat.

### Quellenbedingungen und Abrufhinweise

- Es gelten die Hinweise zu Quellenangabe und Datenschutz aus [Quellenbedingungen und Abrufhinweise](#quellenbedingungen-und-abrufhinweise): Daten des Deutschen Wetterdienstes (DWD) unter CC BY 4.0, weiterveröffentlichte Beobachtungen behalten die Quellenangabe „Deutscher Wetterdienst (DWD)“, und der DWD speichert die anfragende IP-Adresse höchstens 7 Tage.
- Die Werte sind 10-Minuten-Werte; der DWD aktualisiert die `now`-Daten in einem Abstand von weniger als einer Stunde (DWD-Datensatzbeschreibungen).
- Empfehlung des Projekts, keine Regel des Anbieters (der DWD dokumentiert keine Begrenzung der Anfragerate): Planen Sie Parse-Vorgänge extern (zum Beispiel Cron oder ein n8n-Workflow; beides ist keine Abhängigkeit) und parsen Sie eine Station höchstens einmal alle 10 Minuten.

## OsthessenWetter-Beobachtungen (Bibliothek)

Die Bibliothek parst die Messwertseite von OsthessenWetter (`IOsthessenWetterParser`). `ParseAsync(string stationNamePattern, CancellationToken cancellationToken)` liefert ein `OsthessenWetterParseResult`: `Datasets` enthält ein `ObservationDataset` (Export-Wurzel des Vertrags v1, serialisierbar mit `IJsonExportSerializer` / `IXmlExportSerializer`) je nutzbarer passender Station, und `StationFailures` listet jede passende Station ohne Datensatz (`StationName`, `ErrorId`, `Reason`); beide folgen der Reihenfolge der Übersichtstabelle der Seite. Das Muster wird ohne Beachtung der Groß- und Kleinschreibung gegen den ganzen Stationsnamen der Übersicht geprüft: `*` steht für beliebig viele Zeichen, `?` für genau eines, jedes andere Zeichen für sich selbst; es darf nicht leer sein oder nur aus Leerraum bestehen, hat höchstens 128 Zeichen und wird vor jeder Anfrage geprüft. Jede passende Station wird zurückgegeben, nie nur die erste. Ein Aufruf ist ein Abruf der Seite; es gibt keinen Scheduler und keine Wiederholung, die Zeitplanung bleibt also extern. Die Beispiel-CLI führt dies mit `--ParseOstHessen` aus (siehe [Beispiel-CLI](#beispiel-cli)).

### Registrierung in der Anwendung

Die Registrierung aus [Stations- und Standortauswahl](#stations--und-standortauswahl-bibliothek) und [DWD-Beobachtungen](#dwd-beobachtungen-bibliothek) wird um den OsthessenWetter-Endpunkt und die Optionen ergänzt:

```csharp
services.AddSingleton(new OsthessenWetterEndpoints
{
    PageUri = new Uri("https://example.org/messwerte/"), // the measurement page, see Data sources
});
services.AddSingleton(new OsthessenWetterOptions
{
    PageDeadline = TimeSpan.FromSeconds(60),
    Stations =
    {
        new OsthessenWetterStationMetadata { Name = "<overview station name>", Latitude = 50.55m, Longitude = 9.68m, AltitudeM = 300m },
    },
});
```

- `OsthessenWetterEndpoints` und `OsthessenWetterOptions` müssen von jeder Anwendung von `AddWeatherParsing` registriert werden, auch von einer, die OsthessenWetter nicht nutzt, weil die Container-Validierung (`ValidateOnBuild`) die Abhängigkeiten des Parsers prüft; `new OsthessenWetterOptions()` liefert die Standardwerte ohne Stationen.
- `PageUri` muss eine absolute HTTP- oder HTTPS-URI ohne Benutzerinformation sein; sonst schlägt das Auflösen des Parsers mit `OSTHESSEN-OPTIONS-INVALID` fehl. Diagnosen nennen den Endpunkt und seinen Host, nie die Query.
- `PageDeadline` (Standard 60 s) begrenzt einen Parse-Vorgang: Sie muss größer als null und höchstens 1 Stunde sein, sonst schlägt das Auflösen des Parsers mit `OSTHESSEN-OPTIONS-INVALID` fehl; ihr Ablauf lässt den Parse-Vorgang mit `OSTHESSEN-DEADLINE-EXCEEDED` fehlschlagen. Die Anfrage selbst ist zusätzlich durch das 30-Sekunden-Timeout je Anfrage aus [HTTP-Transport](#http-transport) begrenzt: Bei einer `PageDeadline` über 30 s, wie dem Standard, schlägt eine hängende Anfrage zuerst als `OSTHESSEN-PAGE-UNAVAILABLE` fehl; bei einer kürzeren `PageDeadline` läuft zuerst die Frist ab, und der Parse-Vorgang schlägt mit `OSTHESSEN-DEADLINE-EXCEEDED` fehl.
- `Stations` liefert den Standort jeder Station der Übersicht, weil die Seite keine Koordinaten zeigt: `Name` ist der Name wie in der Übersicht der Seite angezeigt (exakt verglichen nach Unicode-Normalform C), `Latitude` von -90 bis 90, `Longitude` von -180 bis 180 (Dezimalgrad), `AltitudeM` optional (fehlt bei null, wird nie durch 0 ersetzt); Namen sind eindeutig. Ein ungültiger Eintrag lässt das Auflösen des Parsers vor jeder Anfrage mit `OSTHESSEN-OPTIONS-INVALID` fehlschlagen. Eine passende Station ohne Eintrag ist ein Stationsfehler `OSTHESSEN-STATION-METADATA-MISSING`.
- Ein GET je Parse-Vorgang, höchstens 2 MiB, keine Wiederholungen. Ein Status ohne Erfolg (einschließlich 404), ein Transportfehler, ein zu großer Inhalt und das Anfrage-Timeout lassen den Parse-Vorgang mit `OSTHESSEN-PAGE-UNAVAILABLE` fehlschlagen. Das HTML wird mit AngleSharp ohne Skripte und ohne Laden von Ressourcen geparst.

### Exportierte Größen

| Spalte der Seite | Exportfeld | Zeit und Zeitraum |
| --- | --- | --- |
| Übersicht `Temp.2m` (Hauptwert, °C) | `temperature_c` | Zeit der Übersichtszeile; kein Zeitraum |
| Übersicht `Luftfeuchte` (%) | `humidity_percent` | Zeit der Übersichtszeile; kein Zeitraum |
| Windtabelle `Wind Ø 10 Min.` (km/h) | `wind_speed_kmh` | Zeit der Windzeile; Zeitraum der vorangegangenen 10 Minuten |

- Stations-ID: der Name der Übersicht in Kleinbuchstaben, ä, ö, ü und ß als ae, oe, ue und ss geschrieben und jede andere Folge von Zeichen außerhalb von a–z und 0–9 durch ein `-` ersetzt (keines am Anfang oder Ende), zum Beispiel `Müs-Hohhütte` → `mues-hohhuette`; `source` ist `osthessenwetter:<Stations-ID>`, und `location.name` behält den Originalnamen. Zwei Namen der Übersicht mit derselben Stations-ID lassen den ganzen Parse-Vorgang mit `OSTHESSEN-STATION-ID-COLLISION` fehlschlagen; geprüft werden alle Namen der Übersicht, nicht nur die passenden, sodass eine ID nie vom Muster abhängt.
- Zeiten: Die Seite zeigt nur eine Uhrzeit (`HH:mm Uhr`). Das Datum wird in `Europe/Berlin` aus `retrieved_at` abgeleitet, das einmal nach dem Eintreffen der Seite erfasst wird; eine angezeigte Zeit, die mehr als 5 Minuten nach der lokalen Abrufzeit liegt, gehört zum Vortag. Jeder Messwert und jeder Datensatz trägt `timestamp_quality` `estimated_date`, jeder Messwert `source_timezone` `Europe/Berlin`. Eine angezeigte Zeit in einer Lücke oder Überlappung der Sommerzeitumstellung lässt sich nicht auflösen (`OSTHESSEN-TIME-UNRESOLVED`), und die Messwerte dieser Zeile werden nicht exportiert. Ältere Anzeigen lassen sich nicht erkennen und werden nicht gefiltert: Konsumenten bewerten `measured_at`.
- Werte: eine Zahl mit optionalem Minuszeichen und Dezimalpunkt, unabhängig von der Kultur gelesen, gefolgt von der Einheit der Spalte. Eine leere Zelle ist ein fehlender Wert, und die Größe wird weggelassen (`OBSERVATION-READING-OMITTED`); jeder andere Text ist unlesbar (`OSTHESSEN-VALUE-UNREADABLE`). `0` ist ein echter Wert. Ausgaberundung wie bei allen Beobachtungen (0,1 °C, 1 %, 1 km/h, exakte Mitten von null weg gerundet).
- Die Windtabelle wird über den Namen mit der Übersicht verknüpft (ohne Beachtung der Groß- und Kleinschreibung, ß gleich ss, ein abschließendes `;` wird ignoriert). Eine fehlende Windtabelle oder eine geänderte Windkopfzeile ergibt `OSTHESSEN-WIND-TABLE-MISSING` und keine Windwerte; eine Station mit keiner, mehr als einer oder einer abweichenden Windzeile ergibt `OSTHESSEN-WIND-ROW-UNMATCHED` und keinen Windwert, während die anderen Stationen ihre behalten.
- Stationsfehler: eine Station, die die Seite als offline kennzeichnet, auch wenn Werte angezeigt werden (`OSTHESSEN-STATION-OFFLINE`); eine Station ohne Metadaten (`OSTHESSEN-STATION-METADATA-MISSING`); eine Station ohne nutzbaren Messwert (`OSTHESSEN-STATION-NO-READING`). Ein Stationsfehler ist nie ein Datensatz.
- Es gibt kein Element `quality`, weil die Seite keinen Qualitätscode zeigt.
- Nicht exportiert: die Böen, der 60-Minuten-Mittelwind, der Niederschlag, die Temperaturänderung und -tendenz, Tageshöchst- und -tiefstwerte und jeder andere Abschnitt der Seite.
- Einschränkung: Unterstützt wird nur der im [OsthessenWetter-Vertrag](../p6/P6_OSTHESSEN_CONTRACT.md) (Englisch) dokumentierte Stand der Seite; eine geänderte Übersichtstabelle lässt den Parse-Vorgang mit `OSTHESSEN-FORMAT-CHANGED` fehlschlagen.

### Fehler- und Hinweis-IDs

Ausnahmen: `OsthessenWetterException` mit `ErrorId` `OSTHESSEN-PATTERN-INVALID` (vor jeder Anfrage), `OSTHESSEN-PATTERN-NO-MATCH`, `OSTHESSEN-PAGE-UNAVAILABLE`, `OSTHESSEN-DEADLINE-EXCEEDED`, `OSTHESSEN-FORMAT-CHANGED`, `OSTHESSEN-STATION-ID-COLLISION`, `OSTHESSEN-NO-USABLE-STATION` (passende Stationen, aber keine liefert einen Datensatz; `StationFailures` listet sie) und `OSTHESSEN-OPTIONS-INVALID`.

Stationsfehler (in `StationFailures` und als Namen der Log-Ereignisse): `OSTHESSEN-STATION-OFFLINE`, `OSTHESSEN-STATION-METADATA-MISSING`, `OSTHESSEN-STATION-NO-READING`.

Hinweise (Namen der Log-Ereignisse, Warning): `OSTHESSEN-WIND-TABLE-MISSING`, `OSTHESSEN-WIND-ROW-UNMATCHED`, `OSTHESSEN-VALUE-UNREADABLE`, `OSTHESSEN-TIME-UNRESOLVED`, `OBSERVATION-READING-OMITTED` (Größe und Grund). Log-Meldungen enthalten die ID, den Stationsnamen, die Quelle `osthessenwetter` und die Abrufzeit in UTC, niemals Messwerte, Seitentext oder den Endpunkt. Hinweise zur ganzen Seite werden immer protokolliert, Stationshinweise nur für passende Stationen.

Ein Abbruch durch den Aufrufer erscheint als `OperationCanceledException` und hat Vorrang vor der Seitenfrist, die wiederum Vorrang vor jedem anderen Fehler hat.

### Quellenbedingungen und Abrufhinweise

- OsthessenWetter veröffentlicht keine Lizenz für seine Messwerte und keine maschinenlesbare Schnittstelle. Das Impressum der Website besagt, dass die Vervielfältigung oder Verwendung ihrer Texte und anderer Inhalte in anderen Veröffentlichungen ohne ausdrückliche Zustimmung des Autors nicht gestattet ist, und es übernimmt keine Gewähr für Aktualität, Richtigkeit oder Vollständigkeit.
- **Produktive Nutzung und Weiterverbreitung der Werte, zum Beispiel über MQTT, werden von diesem Projekt nicht akzeptiert, bis die Zustimmung des Betreibers oder anderweitig geklärte Bedingungen festgehalten sind** ([Vertrag OW-14](../p6/P6_OSTHESSEN_CONTRACT.md), Englisch). Die Bibliothek ist nur mit synthetischen Seiten implementiert und getestet.
- Quellenangabe: Nennen Sie die Quelle „Osthessen-Wetter.de“ überall, wo Werte angezeigt oder weiterveröffentlicht werden.
- Empfehlung des Projekts, keine Regel des Anbieters (die Website dokumentiert keine): Planen Sie Parse-Vorgänge extern (zum Beispiel Cron oder ein n8n-Workflow; beides ist keine Abhängigkeit), rufen Sie die Seite höchstens einmal alle 10 Minuten ab und setzen Sie einen identifizierenden User-Agent am `HttpClient` der Anwendung (die Bibliothek setzt keinen).

## MOSMIX-Vorhersagen (Bibliothek)

Die Bibliothek parst die DWD-MOSMIX-Punktvorhersagen eines Standorts (`IMosmixLParser` für `MOSMIX_L`, `IMosmixSParser` für `MOSMIX_S`). `ParseAsync(string mosmixId, CancellationToken cancellationToken)` liefert ein `MosmixParseResult`: `Run` enthält einen `ForecastRun` (Export-Wurzel des Vertrags v1, serialisierbar mit `IJsonExportSerializer` / `IXmlExportSerializer`), `OmittedValues` die vom Lauf-Builder weggelassenen Werte mit ihren Gründen, `NotExportedElements` die Namen der nicht abgebildeten Elemente der Datei, `AbsentElements` die abgebildeten Elemente, die die Datei nicht enthält, `Freshness` (`Fresh` oder `Stale`) und `RunAge` (`retrieved_at − issued_at`). Die beiden Parser sind getrennte Dienste hinter getrennten Schnittstellen: Keiner nimmt einen Produktparameter, und ein Fehler eines Produkts weicht nie auf das andere aus ([P7-Vorhersagevertrag](../p7/P7_FORECAST_CONTRACT.md), Englisch).

### Registrierung in der Anwendung

Die Registrierung aus [Stations- und Standortauswahl](#stations--und-standortauswahl-bibliothek), [DWD-Beobachtungen](#dwd-beobachtungen-bibliothek) und [OsthessenWetter-Beobachtungen](#osthessenwetter-beobachtungen-bibliothek) wird um einen Endpunkt- und einen Optionstyp je Produkt ergänzt:

```csharp
services.AddSingleton(new MosmixLForecastEndpoints
{
    SingleStationsDirectory = new Uri("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/"),
});
services.AddSingleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) });
services.AddSingleton(new MosmixSForecastEndpoints
{
    AllStationsUri = new Uri("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
});
services.AddSingleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(2) });
```

- Alle vier Typen müssen von jeder Anwendung von `AddWeatherParsing` registriert werden, auch von einer, die keine MOSMIX-Vorhersagen nutzt, weil die Container-Validierung (`ValidateOnBuild`) die Abhängigkeiten der Parser prüft. Sie sind unabhängig von den `MosmixCatalogEndpoints` der Auswahl.
- `SingleStationsDirectory` ist das Verzeichnis `single_stations/` von `MOSMIX_L`: eine absolute HTTP- oder HTTPS-URI, die mit `/` endet, ohne Benutzerinformation, Query oder Fragment; die Datei eines Standorts wird darunter als `<id>/kml/MOSMIX_L_LATEST_<id>.kmz` zusammengesetzt. `AllStationsUri` ist die `MOSMIX_S`-Datei aller Stationen und wird unverändert verwendet (eine Query ist erlaubt, wird aber nie protokolliert). Eine ungültige URI lässt das Auflösen der Quelle mit `MOSMIX-OPTIONS-INVALID` fehlschlagen. Diagnosen nennen den Endpunkt als Schema, Host und Pfad.
- `ParseDeadline` (`MOSMIX_L` Standard 60 s, `MOSMIX_S` Standard 5 Minuten; größer als null, höchstens 1 Stunde) begrenzt einen Parse-Vorgang einschließlich Download und Lesen des Streams; ihr Ablauf lässt den Parse-Vorgang mit `MOSMIX-DEADLINE-EXCEEDED` fehlschlagen. Für `MOSMIX_S` entspricht die Anfragegrenze aus [HTTP-Transport](#http-transport) der Parse-Frist statt der festen 30 s, weil die Datei aller Stationen (etwa 36 MB) bei jedem Parse-Vorgang bis zum Ende gestreamt wird; für `MOSMIX_L` gilt die 30-Sekunden-Anfragegrenze.
- `MaxRunAge` hat keinen Standard und muss gesetzt werden (größer als null, höchstens 240 Stunden); ein nicht gesetzter Wert lässt das Auflösen des Parsers mit `MOSMIX-OPTIONS-INVALID` fehlschlagen. Richtwert (kein Standard): `MOSMIX_S` 2 h, `MOSMIX_L` 8 h. Ein Lauf, dessen Alter (`retrieved_at − issued_at`) über der Grenze liegt, wird trotzdem zurückgegeben, mit `Freshness` `Stale`, dem Element `run_age_exceeded` `true` und einer Warnung `MOSMIX-RUN-STALE`, die Alter und Grenze nennt; bis zur Grenze ist der Lauf `Fresh` und das Element `false`. Die P7-Parser setzen `run_age_exceeded` immer; das Element fehlt nur bei Erzeugern, die es nicht kennen.
- Die MOSMIX-ID muss `^[A-Z0-9]{4,5}$` entsprechen (die Katalogform, exakt beibehalten, nie in Großbuchstaben umgewandelt); jeder andere Wert schlägt vor jeder Anfrage mit `MOSMIX-ID-INVALID` fehl, ohne die ID in Logs oder Meldungen. Für `MOSMIX_L` ist ein HTTP 404 der Standortdatei `MOSMIX-ID-UNKNOWN`; für `MOSMIX_S` ist ein in der Datei aller Stationen fehlender Standort `MOSMIX-ID-UNKNOWN`, nachdem die ganze Datei gelesen wurde.
- Ein GET je Parse-Vorgang, keine Wiederholungen: `MOSMIX_L` höchstens 1 MiB komprimiert und 16 MiB entpackt, `MOSMIX_S` höchstens 64 MiB komprimiert und 2 GiB gestreamt, nie als Ganzes im Speicher gehalten. Das Archiv muss genau ein `.kml`-Element enthalten, dessen CRC-32 geprüft wird; DTDs und externe Entitäten werden abgelehnt. Status ohne Erfolg, Transportfehler und die Anfragegrenze → `MOSMIX-RETRIEVAL-FAILED`; Größe → `MOSMIX-TOO-LARGE`; eine ungültige Datei → `MOSMIX-FORMAT-INVALID`, eine fehlende Ausgabezeit → `MOSMIX-ISSUE-TIME-MISSING`, eine `MOSMIX_L`-Datei eines anderen Standorts → `MOSMIX-LOCATION-MISMATCH`.

### Exportierte Größen

| MOSMIX-Element (Einheit) | Exportfeld | Umrechnung und Zeitraum |
| --- | --- | --- |
| `TTT` (K) | `temperature_c` | K − 273,15; kein Zeitraum |
| `PPPP` (Pa) | `pressure_hpa`, `pressure_mmhg` | Pa ÷ 100, beide aus demselben ungerundeten Wert; `pressure_reference` `unknown` |
| `DD` (°) | `wind_direction_deg` | unverändert; 0 bis 360 |
| `FF` (m/s) | `wind_speed_kmh` | × 3,6; kein Zeitraum |
| `FX1` (m/s, Maximum der letzten Stunde) | `wind_gust_kmh` | × 3,6; `[valid_at − 1 h, valid_at]`, Aggregation `maximum` |
| `Rad1h` (kJ/m² der letzten Stunde) | `solar_irradiance_w_m2` | kJ/m² je 1 h → W/m²; `[valid_at − 1 h, valid_at]`, Aggregation `mean` |
| `RR1c` (kg/m² der letzten Stunde) | `rain_rate_mm_h`, `precipitation_amount_mm` | mm je 1 h (`mean`) und die Summe (`sum`); `[valid_at − 1 h, valid_at]` |

- `issued_at` ist die Ausgabezeit der Datei, `retrieved_at` wird einmal nach dem Lesen der Datei erfasst (ganze Sekunden), `valid_at` ist jeder Zeitschritt; alle in UTC ohne Suffix. Ein Vorhersagelauf hat nie `measured_at`. `source` des Laufs ist `dwd-mosmix-l:<id>` oder `dwd-mosmix-s:<id>`, `product` `MOSMIX_L` oder `MOSMIX_S`; `location` (Name und Koordinaten) stammt aus der Datei, nie aus dem Katalog.
- Ein Token `-` ist ein fehlender Wert an seinem eigenen Schritt und verschiebt nichts; ein Wert außerhalb seines plausiblen Bereichs wird mit Grund weggelassen (`FORECAST-VALUE-OMITTED`); `0` ist ein echter Wert. Ausgaberundung wie bei allen Größen (0,1 °C, 1 hPa und 1 mmHg, 1 km/h für Windgeschwindigkeit und Böe; exakte Mitten von null weg gerundet); `precipitation_amount_mm` ist wie `rain_rate_mm_h` ungerundet (C-122, C-123).
- Es werden keine Luftfeuchte und keine Beleuchtungsstärke exportiert (die Dateien enthalten weder relative Feuchte noch Lux); sie werden nie mit Nullen aufgefüllt. Jedes andere Element der Datei (Wahrscheinlichkeiten, Bewölkung, Sichtweite, Wettercodes, längere Summen) wird in `NotExportedElements` aufgeführt und einmal als `MOSMIX-ELEMENT-NOT-EXPORTED` protokolliert; ein abgebildetes Element, das in der Datei fehlt, wird in `AbsentElements` aufgeführt und als `MOSMIX-ELEMENT-ABSENT` protokolliert. Ein Lauf ohne exportierbaren Wert schlägt mit `MOSMIX-NO-USABLE-VALUE` fehl, das die Auslassungen und die nicht exportierten Namen enthält.
- Der Horizont ist die Zeitachse der Datei selbst (die Dateien vom 2026-10-26 enthielten 240 stündliche Schritte für `MOSMIX_S` und 247 für `MOSMIX_L`; der DWD aktualisiert die ersten 24 Stunden jedes Laufs stündlich und die späteren Schritte nur in den vier Hauptläufen); eine neue Datei beweist nicht, dass jeder Schritt neu berechnet wurde.
- Schemakompatibilität: `run_age_exceeded`, `wind_gust_kmh` und `precipitation_amount_mm` sind additive optionale Elemente des Schemas v1, nach den bestehenden angefügt, und `IntervalAggregation` hat die Elemente `maximum` und `sum` erhalten (C-120 bis C-127). Dokumente ohne sie bleiben gegen jedes v1-Schema gültig, aber Dokumente, die sie enthalten, sind nur gegen das neu erzeugte Schema gültig: Das v1-Schema hat kein `xs:any`; ein Konsument muss deshalb mit dem `schemas/export/v1/weatherparser-export-v1.xsd` validieren, das mit der erzeugenden Paketversion ausgeliefert wird, nicht mit einer älteren Kopie, und Konsumenten mit vollständigen Fallunterscheidungen (`switch`) über die Aggregations-Enumeration müssen die zwei neuen Elemente behandeln.

### Fehler- und Hinweis-IDs

Ausnahmen: `MosmixForecastException` mit `ErrorId` `MOSMIX-ID-INVALID` (vor jeder Anfrage), `MOSMIX-ID-UNKNOWN`, `MOSMIX-RETRIEVAL-FAILED`, `MOSMIX-DEADLINE-EXCEEDED`, `MOSMIX-TOO-LARGE`, `MOSMIX-FORMAT-INVALID`, `MOSMIX-ISSUE-TIME-MISSING`, `MOSMIX-LOCATION-MISMATCH`, `MOSMIX-NO-USABLE-VALUE` (mit `OmittedValues` und `NotExportedElements`) und `MOSMIX-OPTIONS-INVALID`; `Product` nennt das Produkt. Jeder Fehler eines Parse-Vorgangs wird einmal als Fehler protokolliert, mit seiner ID, dem Produkt, der MOSMIX-ID (nicht bei einer ungültigen ID), dem bereinigten Endpunkt und der UTC-Zeit; `MOSMIX-OPTIONS-INVALID` wird geworfen, wenn die Quelle oder der Parser konstruiert wird, also bei der ersten Auflösung, und von der Bibliothek nicht protokolliert, die Anwendung meldet ihn also selbst. `ValidateOnBuild` erkennt eine fehlende Registrierung der vier Optionstypen beim Bauen, führt aber die Konstruktoren nicht aus; eine ungültige URI oder ein nicht gesetztes `MaxRunAge` zeigt sich also erst bei der Auflösung; eine Anwendung, die dies beim Start erkennen möchte, löst `IMosmixLParser` und `IMosmixSParser` nach dem Bauen des Containers einmal auf.

Hinweise (Namen der Log-Ereignisse, Warning): `MOSMIX-RUN-STALE`, `MOSMIX-ELEMENT-ABSENT`, `MOSMIX-ELEMENT-NOT-EXPORTED`, `FORECAST-VALUE-OMITTED` (Gültigkeitszeit, Größe und Grund). Log-Meldungen enthalten nie Vorhersagewerte, Dateitext, Namen von Archivelementen oder Query-Strings.

Ein Abbruch durch den Aufrufer erscheint als `OperationCanceledException` mit dem Token des Aufrufers und hat Vorrang vor der Parse-Frist, die wiederum Vorrang vor jedem anderen Fehler hat.

### Quellenbedingungen und Abrufhinweise

- Daten: Deutscher Wetterdienst (DWD), offene Daten ohne Registrierung, wiederverwendbar unter CC BY 4.0 mit Quellenangabe; nennen Sie „Deutscher Wetterdienst“ überall, wo Vorhersagen angezeigt oder weiterveröffentlicht werden. Der DWD dokumentiert keine Begrenzung der Anfragerate und speichert die IP-Adresse des Anfragenden höchstens 7 Tage.
- Aktualisierungszyklen (Zyklen des Anbieters, kein Zeitplan dieser Bibliothek): `MOSMIX_S` ein Lauf pro Stunde, veröffentlicht etwa 45 Minuten nach der vollen Stunde; `MOSMIX_L` Läufe um 03, 09, 15 und 21 UTC, verfügbar gegen 04:20, 10:15, 16:20 und 22:15 UTC. `..._LATEST_...` bezeichnet die neueste veröffentlichte Datei, die kurz nach einem Laufzeitpunkt noch der vorherige Lauf sein kann; das Aktualitätskennzeichen meldet das, die Bibliothek wartet und wiederholt nie.
- Empfehlung des Projekts: Planen Sie Parse-Vorgänge extern (zum Beispiel Cron oder ein n8n-Workflow; beides ist keine Abhängigkeit): `MOSMIX_S` höchstens einmal je stündlichem Lauf, etwa 45 Minuten nach der vollen Stunde – jeder Parse-Vorgang lädt die ganze Datei aller Stationen (etwa 36 MB) für einen Standort herunter; `MOSMIX_L` höchstens einmal je Hauptlauf und Standort (etwa 17 KB je Standort). Setzen Sie einen identifizierenden User-Agent am `HttpClient` der Anwendung (die Bibliothek setzt keinen).

## MQTT-Serialisierer (Bibliothek)

Die Bibliothek macht aus einer Export-Wurzel eine MQTT-Nachricht, ohne jeden Brokerzugriff: `IMqttMessageSerializer.SerializeAsync<TRoot>(TRoot root, CancellationToken cancellationToken)` liefert eine unveränderliche `MqttMessage` mit `Topic`, `Payload` (UTF-8-JSON, eine private Kopie), `QualityOfService` (`AtMostOnce` = 0, `AtLeastOnce` = 1, `ExactlyOnce` = 2), `Retain` und `MessageType`. Der Serialisierer ist generisch: Er enthält keine anbieterspezifische Logik und kein fest verdrahtetes Topic, und er hat keinen MQTT-Client, keine Brokerverbindung, keine TLS-Einstellung und keine Zugangsdaten ([P8-MQTT-Vertrag](../p8/P8_MQTT_CONTRACT.md), Englisch). Das Veröffentlichen der Nachricht ist Aufgabe der Anwendung; der Beispiel-Brokertransport gehört zur Beispiel-CLI (P3).

| Wurzeltyp (`TRoot`) | Nachrichtentyp | Standard-QoS | Standard-Retain |
| --- | --- | --- | --- |
| `ObservationDataset` | `Observation` | 1 | ja |
| `ForecastRun` | `Forecast` | 1 | ja |
| `CdcStationList` | `CdcStationList` | 1 | nein |
| `MosmixLocationList` | `MosmixLocationList` | 1 | nein |

Der Nachrichtentyp folgt genau dem statischen Typ `TRoot`; `ExportDocument` (der Umschlag für mehrere Ergebnisse) und jeder andere Typ, auch eine unterstützte Wurzel, die als `object` übergeben wird, schlagen mit `MQTT-MESSAGE-TYPE-UNSUPPORTED` fehl. Die Standardwerte sind die vereinbarte Semantik: Beobachtungen und vollständige Vorhersageläufe QoS 1 und retained, Stationslisten QoS 1 und nicht retained, damit eine alte Suchantwort nie für die aktuelle Auswahl gehalten wird.

### Registrierung in der Anwendung

Jede Anwendung von `AddWeatherParsing` registriert eine `MqttTopicOptions`, auch eine, die MQTT nicht nutzt (dann ein leeres `new MqttTopicOptions()`, mit dem jede Serialisierung mit `MQTT-MAPPING-MISSING` fehlschlägt). Die folgenden Topics sind die Beispiele aus REQ-061, als Konfiguration der Anwendung geschrieben; sie sind keine Standardwerte der Bibliothek:

```csharp
services.AddSingleton(new MqttTopicOptions
{
    Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
    {
        [MqttMessageType.Observation] = new() { TopicTemplate = "haus/weather/measurements/{source}/state" },
        [MqttMessageType.Forecast] = new() { TopicTemplate = "haus/weather/forecasts/{source}/state" },
        [MqttMessageType.CdcStationList] = new() { TopicTemplate = "haus/weather/stations/dwd/selection" },
        [MqttMessageType.MosmixLocationList] = new() { TopicTemplate = "haus/weather/stations/mosmix/selection" },
    },
});
```

- Eine Zuordnung hat `TopicTemplate` (Pflicht), `QualityOfService` und `Retain` (beide optional; null übernimmt den Standard aus der Tabelle oben). `MaxPayloadBytes` (optional, 1 bis 268 435 450) senkt die Grenze der Nutzdaten.
- Die Optionen werden vollständig validiert, wenn `IMqttMessageSerializer` zum ersten Mal aufgelöst wird, und als Momentaufnahme gehalten; spätere Änderungen am registrierten Objekt wirken nicht. Jeder Verstoß schlägt mit `MQTT-OPTIONS-INVALID` fehl, den die Bibliothek nicht protokolliert. `ValidateOnBuild` erkennt eine fehlende Registrierung, führt aber den Konstruktor nicht aus; eine Anwendung, die Konfigurationsfehler beim Start erkennen möchte, löst `IMqttMessageSerializer` nach dem Bauen des Containers einmal auf.
- Die vollständige Liste der Typen, die eine Anwendung neben `AddWeatherParsing` registriert: `HttpClient`, `TimeProvider`, `ILogger<>`, `CdcCatalogEndpoints`, `MosmixCatalogEndpoints`, `DwdObservationOptions`, `OsthessenWetterOptions`, `OsthessenWetterEndpoints`, `MosmixLForecastEndpoints`, `MosmixLForecastOptions`, `MosmixSForecastEndpoints`, `MosmixSForecastOptions` und `MqttTopicOptions`; die Container-Validierung prüft die Abhängigkeiten jedes Dienstes, deshalb sind alle nötig, auch für eine Anwendung, die nur eine Quelle nutzt.

### Topic-Vorlagen und Platzhalter

Eine Vorlage ist literaler Text mit Platzhaltern `{name}`; es gibt keine Escape-Syntax, `{` und `}` kommen also nur um einen bekannten Platzhalter vor. Die Werte stammen ausschließlich aus der Wurzel:

| Nachrichtentyp | Platzhalter |
| --- | --- |
| `Observation` | `{source}` (`dwd:00044`, `osthessenwetter:<slug>`), `{provider}` (der Text von `source` vor dem ersten `:`), `{station_id}` |
| `Forecast` | `{source}` (`dwd-mosmix-l:<id>`, `dwd-mosmix-s:<id>`), `{provider}` (`dwd-mosmix-l`, `dwd-mosmix-s`), `{mosmix_id}`, `{product}` (`MOSMIX_L`, `MOSMIX_S`) |
| `CdcStationList`, `MosmixLocationList` | `{provider}` (der Text der Listen-`source` vor dem ersten `:`; eine `source` ohne ein solches Präfix schlägt mit `MQTT-PLACEHOLDER-VALUE-INVALID` fehl) |

Bei der Validierung der Optionen geprüfte Regeln (`MQTT-OPTIONS-INVALID`):
- ein unbekannter Platzhalter oder einer, den der Nachrichtentyp nicht hat; eine unausgeglichene, verschachtelte oder leere Klammer; eine leere Vorlage;
- `+`, `#`, U+0000, Steuerzeichen (U+0001 bis U+001F, U+007F bis U+009F), Unicode-Nichtzeichen oder ungepaarte Surrogate im literalen Text; eine Vorlage, die mit `$` beginnt; leere Topic-Ebenen, die immer entstehen (`//`, ein führender oder ein abschließender `/`); literale Bytes plus ein Byte je Platzhalter über 65 535;
- Eindeutigkeit innerhalb eines Nachrichtentyps, damit der Retained-Zustand einer Quelle, eines Produkts oder eines Standorts nie durch einen anderen ersetzt wird: Eine `Observation`-Vorlage enthält `{source}` oder `{provider}` und `{station_id}`, jeweils als ganze Topic-Ebene; eine `Forecast`-Vorlage enthält `{source}` oder `{product}` und `{mosmix_id}`, jeweils als ganze Topic-Ebene (ein Platzhalter, der seine Ebene mit anderem Text teilt, wie in `o/x{source}`, zählt nicht);
- Eindeutigkeit über Nachrichtentypen hinweg: Der literale Text vor dem ersten Platzhalter jeder zugeordneten Vorlage muss sich von dem jeder anderen Vorlage an einer Position innerhalb beider unterscheiden, sodass kein Präfix Präfix eines anderen ist (`weather/{source}` zusammen mit `weather/{product}/{mosmix_id}` wird abgelehnt; die Beispiele aus REQ-061 werden akzeptiert).

Bei der Serialisierung muss jeder Platzhalterwert nicht leer und frei von `/`, `+`, `#`, U+0000, Steuerzeichen, Nichtzeichen und ungepaarten Surrogaten sein (`MQTT-PLACEHOLDER-VALUE-INVALID`); Werte werden unverändert eingesetzt, nie maskiert. Das aufgelöste Topic darf höchstens 65 535 UTF-8-Bytes haben, keinen Wildcard, kein führendes `$` und keine leere Ebene (`MQTT-TOPIC-INVALID`).

### Nutzdaten

- Die Nutzdaten sind genau die Bytes des JSON-Exports derselben Wurzel (`IJsonExportSerializer`: UTF-8 ohne Byte-Order-Mark, die Regeln C-11, C-15 und C-92); MQTT transportiert nie XML. Ein Vorhersagelauf ist immer eine `Forecast`-Nachricht und hat kein `measured_at`.
- Die Wurzel wird vom JSON-Serialisierer validiert, bevor ein Platzhalterwert gelesen wird; eine ungültige Wurzel (ein nicht zugewiesenes Pflichtelement, ein Datensatz ohne nutzbare Messwerte, ein Lauf ohne Schritte) schlägt deshalb mit `MQTT-SERIALIZATION-FAILED` und der `ExportSerializationException` als innerer Ausnahme fehl und wird nie zu einer Nachricht.
- Grenze: Die Nutzdaten dürfen höchstens 268 435 450 − T Bytes für ein Topic von T UTF-8-Bytes haben (das MQTT-Maximum der Restlänge abzüglich Topic, Paketkennung und einer leeren MQTT-5.0-Eigenschaftslänge), oder `MaxPayloadBytes`, wenn das kleiner ist; darüber schlägt der Serialisierer mit `MQTT-PAYLOAD-TOO-LARGE` fehl. Eigenschaften, die ein Transport hinzufügt, und Grenzen des Brokers (zum Beispiel eine konfigurierte maximale Paketgröße) verringern den Spielraum weiter und sind Sache des Transports. Die Grenze beschränkt die Annahme der Nachricht, nicht den Speicher: Der JSON-Serialisierer puffert das ganze Dokument, bevor die Grenze geprüft wird, der Spitzenspeicher folgt also der Größe der Wurzel. Als gemessenes Beispiel, keine Kapazitätsgrenze: Ein synthetischer `MOSMIX_L`-Lauf an der Grenze von 1000 Schritten mit jedem abgebildeten Element ergab Nutzdaten von 724 316 Bytes; längere Namen oder andere Werte ändern die Größe.
- Die Nutzdaten einer gültigen Wurzel sind nie leer; eine Nachricht der Bibliothek löscht also nie eine Retained-Nachricht.

### Duplikate

QoS 1 kann eine Nachricht mehr als einmal zustellen. Eine Nachricht ist genau dann ein Duplikat einer anderen, wenn die Nutzdaten byteweise identisch sind (gleiche Eingabe ergibt gleiche Bytes). Zeitstempel allein sind kein Duplikatschlüssel: Weder das `measured_at` des Datensatzes noch die `measured_at`-Werte je Größe kennzeichnen einen Stand einer Beobachtung, weil ein korrigierter Wert seinen Zeitstempel behalten kann und das `measured_at` des Datensatzes nur der neueste Messwert ist. Dieselben Messwerte, später abgerufen, tragen ein anderes `retrieved_at` und sind eine neue Nachricht, kein Duplikat. Bei Vorhersagen kennzeichnen `source` und `issued_at` den Lauf; eine spätere Nachricht desselben Laufs mit einem anderen `retrieved_at` ist kein Duplikat und ersetzt die frühere Retained-Nachricht.

### Fehler-IDs

`MqttMessageException` trägt `ErrorId` und, soweit bekannt, `MessageType`; der Serialisierer schreibt keine Log-Einträge, der Aufrufer protokolliert Fehler also selbst. Vorrang innerhalb eines Aufrufs: eine Null-Wurzel (`ArgumentNullException`), Abbruch durch den Aufrufer (`OperationCanceledException`, nie eingepackt), `MQTT-MESSAGE-TYPE-UNSUPPORTED`, `MQTT-MAPPING-MISSING`, `MQTT-SERIALIZATION-FAILED`, `MQTT-PLACEHOLDER-VALUE-INVALID`, `MQTT-TOPIC-INVALID`, `MQTT-PAYLOAD-TOO-LARGE`; Konfigurationsfehler sind `MQTT-OPTIONS-INVALID`. Meldungen nennen den Nachrichtentyp und die Regel (und den Namen des Platzhalters), nie Inhalt der Nutzdaten, Platzhalterwerte oder Vorlagentext.

### Einschränkungen

- **OsthessenWetter-Werte (OW-14):** Der Serialisierer akzeptiert jeden gültigen Beobachtungsdatensatz, auch des Anbieters `osthessenwetter`, aber OsthessenWetter-Werte dürfen nicht veröffentlicht werden, weder über MQTT noch anders, bis die Zustimmung des Betreibers oder anderweitig geklärte Bedingungen festgehalten sind. Die Tests des Projekts verwenden ausschließlich synthetische OsthessenWetter-Daten.
- DWD-Daten behalten bei der Weiterveröffentlichung die Quellenangabe „Deutscher Wetterdienst (DWD)“ (siehe die Quellenbedingungen der Abschnitte oben).
- Schemakompatibilität: MQTT-Nutzdaten sind JSON-Dokumente des Exportvertrags v1; Konsumenten, die den XML-Export validieren, verwenden das Schema der erzeugenden Paketversion (siehe [MOSMIX-Vorhersagen](#mosmix-vorhersagen-bibliothek)), und vollständige Fallunterscheidungen über `IntervalAggregation` müssen `maximum` und `sum` behandeln.

## Beispiel-CLI

Die Beispiel-CLI `WeatherParser` (Projekt `examples/WeatherParser.Cli`, Assembly `WeatherParser.dll`) zeigt, wie eine Anwendung das Paket nutzt: Sie referenziert `WeatherParser.Parsing` als versioniertes Paket und enthält keinen kopierten Bibliothekscode. Ihr Vertrag ist der [P3-CLI-Vertrag](../p3/P3_CLI_CONTRACT.md) (Englisch; CL-01 bis CL-22, genehmigt am 2026-10-29). Alle Befehle, Ausgabemodi, der MQTT-Transport und das Abbruchverhalten sind implementiert. Die Tests laufen offline mit lokalen Fake-Quellen und einem Fake-Brokerendpunkt unter Windows und in den gehosteten CI-Jobs (Windows x64, Ubuntu x64, Debian 13 ARM64); in P3 liefen die Broker-Integrationstests nur unter Windows, gegen einen lokalen Mosquitto ohne TLS und ohne Zugangsdaten ([P3-Verifikationsprotokoll](../p3/P3_VERIFICATION.md), Englisch); TLS mit eigener CA und authentifizierte Verbindungen wurden in P9.09 gegen einen Mosquitto-2.1.2-Testbroker unter Windows und auf dem NanoPi R76S verifiziert ([P9.09-Protokoll](../p9/P9_09_BROKER_AND_SHUTDOWN_CHECKS.md), Englisch). Die Live-Quellen und der NanoPi R76S wurden in P3.11 geprüft ([P3.11-Protokoll](../p3/P3_11_INTEGRATED_VERIFICATION.md), Englisch). Bauen und starten Sie sie wie in [CONTRIBUTING.md](../../CONTRIBUTING.md#example-cli-examplesweatherparsercli-p3) (Englisch) beschrieben; nach einem Release-Build startet sie mit `dotnet <Ausgabe>/WeatherParser.dll`.

```text
WeatherParser --<Befehl> [Optionen]
```

Jeder Aufruf führt genau einen Befehl aus und endet dann mit einem Exit-Code (REQ-006). Die CLI hat keinen Scheduler, keinen Dienstmodus und keine Wiederholungen über Aufrufe hinweg; planen Sie sie extern (siehe [Zeitplanung](#zeitplanung)). Sie liest nichts von der Standardeingabe.

### Befehle und Optionen

Befehle und Optionen werden mit zwei führenden Bindestrichen geschrieben und ohne Beachtung der Groß- und Kleinschreibung erkannt; jede Option wird höchstens einmal angegeben, und ein Wert beginnt nie mit `--`. Genau ein Befehl ist erforderlich.

| Befehl | Pflichtoptionen | Optionale Optionen |
|---|---|---|
| `--SelectDWD` | `--Latitude`, `--Longitude`, `--Altitude`, `--MaxDistance`, `--MaxAltDifference` | – |
| `--ParseDWD` | – | `--StationID` |
| `--ParseOstHessen` | `--StationNamePattern` | – |
| `--SelectMOSMIX` | `--Latitude`, `--Longitude`, `--Altitude`, `--MaxDistance`, `--MaxAltDifference` | `--RefreshMosmixSEvidence` |
| `--ParseMOSMIX` | `--Product`, `--MOSMIXID` | – |

- `--SelectDWD` findet CDC-Stationen rund um einen Bezugspunkt; `--SelectMOSMIX` findet MOSMIX-Vorhersagestandorte mit denselben Filtern im MOSMIX-Katalog. `--Latitude` und `--Longitude` akzeptieren Dezimalgrad oder Grad, Minuten und Sekunden mit Hemisphäre (ein Argument, zum Beispiel `"50° 33′ 18″ N"`); `--Altitude` und `--MaxAltDifference` sind Meter, `--MaxDistance` Kilometer, alle unabhängig von der Kultur gelesen. Eine leere Auswahl ist ein Erfolg.
- `--ParseDWD` parst die 10-Minuten-Beobachtungen einer DWD-Station: `--StationID` (fünf Ziffern mit führenden Nullen) oder, ohne sie, `Dwd.ReferenceStationId` aus der Konfiguration. Ohne beides schlägt der Befehl vor jeder Anfrage mit `CLI-DWD-STATION-MISSING` fehl; die CLI wählt nie selbst eine Station. Eine angegebene `--StationID` hat immer Vorrang, auch ein leerer Wert (zum Beispiel eine nicht gesetzte Shell-Variable): Er schlägt vor jeder Anfrage mit `STATION-ID-INVALID` (Exit-Code 2) fehl, statt auf die Konfiguration auszuweichen.
- `--ParseOstHessen` parst jede OsthessenWetter-Station, deren Name zum Muster passt (`*` und `?`); Koordinaten und Höhe jeder Station stammen aus `OsthessenWetter.Stations`.
- `--ParseMOSMIX` parst den neuesten Vorhersagelauf eines MOSMIX-Standorts; `--Product` ist `MOSMIX_S` oder `MOSMIX_L` (kein Ausweichen auf das andere Produkt).
- `--RefreshMosmixSEvidence` aktualisiert zuerst den gespeicherten MOSMIX_S-Nachweisindex (ein Download der vollständigen MOSMIX_S-Datei, begrenzt durch `Mosmix.Catalog.MosmixSRefreshDeadline`) und wählt dann aus; eine fehlgeschlagene Aktualisierung lässt den Befehl mit der ID der Bibliothek fehlschlagen, und es läuft keine Auswahl. Ohne die Option verwendet die Auswahl den gespeicherten Index: Ohne nutzbaren Index schlägt sie vor jeder Anfrage mit `MOSMIX-S-EVIDENCE-MISSING` fehl (Exit-Code 1); ein Index, der älter als `Mosmix.Catalog.MosmixSEvidenceMaxAge` ist, wird weiter verwendet und protokolliert eine Warnung `MOSMIX-S-EVIDENCE-STALE` (CL-17). Führen Sie die Auswahl einmal mit der Option aus, um den Index anzulegen.

Stations-IDs, MOSMIX-IDs und Muster prüft die Bibliothek vor jeder Anfrage; ein solcher Fehler ist ein Bedienfehler (Exit-Code 2) mit der ID der Bibliothek.

### Globale Optionen

| Option | Bedeutung |
|---|---|
| `--Output mqtt\|json\|xml` | Ausgabemodus; Standard `mqtt` |
| `--OutputFile <Pfad>` | Das JSON- oder XML-Dokument in eine neue Datei schreiben (nur mit `json` oder `xml`); ohne die Option geht das Dokument auf die Standardausgabe |
| `--Config <Pfad>` | Konfigurationsdatei; sonst die Umgebungsvariable `WEATHERPARSER_CONFIG` |
| `--Verbose` | Auch Debug-Meldungen protokollieren |
| `--Help` | Die Hilfe anzeigen, allein oder mit einem Befehl, und mit 0 enden, ohne eine Konfiguration zu lesen |

```text
WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --Output json
WeatherParser --SelectDWD --Latitude "50° 33′ 18″ N" --Longitude "9° 40′ 26″ E" --Altitude 300 --MaxDistance 50 --MaxAltDifference 200
WeatherParser --ParseDWD --StationID <DWD-STATIONS-ID> --Output json --OutputFile weather.json
WeatherParser --ParseDWD
WeatherParser --ParseOstHessen --StationNamePattern "Fulda*" --Output xml --OutputFile weather.xml
WeatherParser --SelectMOSMIX --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --RefreshMosmixSEvidence
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> --Output json
WeatherParser --ParseMOSMIX --Product MOSMIX_S --MOSMIXID <MOSMIX-ID>
```

Die Beispielkoordinaten zeigen die zwei Eingabeformate und sind nicht exakt gleichwertig; das Muster dient der Veranschaulichung und garantiert keinen Treffer.

### Ausgabe

- **JSON und XML** (`--Output json|xml`): ein vollständiges Dokument je Aufruf, UTF-8 ohne Byte-Order-Mark, von den Serialisierern der Bibliothek aus denselben Entitäten wie die MQTT-Nutzdaten erzeugt. Die Auswahlbefehle schreiben ihre eigenständige Liste (`cdc_station_list`, `mosmix_location_list`); die Parse-Befehle schreiben ein `export_document` mit den Beobachtungen oder Vorhersageläufen und einer Liste `errors` für fehlgeschlagene Einträge (eine fehlgeschlagene Station mit ihrer `source`). Ein Fehler auf Befehlsebene schreibt kein Dokument. Mit `--OutputFile` darf das Ziel nicht existieren, und sein Verzeichnis muss existieren (geprüft vor jeder Anfrage); das Dokument wird in eine temporäre Datei `<Ziel>.<zufällig>.tmp` im selben Verzeichnis geschrieben und dann ohne Überschreiben an das Ziel verschoben, sodass eine Zieldatei immer vollständig ist. Nach einem Fehler oder Abbruch erscheint kein Ziel, und die temporäre Datei wird gelöscht; schlägt dieses Löschen fehl, nennt eine Warnung `CLI-OUTPUT-FAILED` die verbliebene temporäre Datei, die dann von Hand entfernt werden muss. Das Dokument wird vor dem Schreiben vollständig im Speicher gehalten. Der JSON- und der XML-Modus verwenden die Brokereinstellungen nicht, lesen keine Zugangsdaten und kontaktieren nie einen Broker; der Abschnitt `Mqtt` wird trotzdem als Teil der Konfigurationsdatei gelesen und muss deshalb wohlgeformt sein.
- **MQTT** (`--Output mqtt`, Standard): eine Nachricht je Beobachtungsdatensatz, je vollständigem Vorhersagelauf oder je Stationsliste, vom MQTT-Serialisierer der Bibliothek mit den konfigurierten Topic-Vorlagen serialisiert (siehe [Topic-Vorlagen und Platzhalter](#topic-vorlagen-und-platzhalter)), QoS 1, retained für Beobachtungen und Vorhersagen, nicht retained für Stationslisten. Die Topic-Zuordnung des Nachrichtentyps des Befehls wird vor jeder Anfrage geprüft (`MQTT-MAPPING-MISSING` → `CLI-CONFIG-INVALID`). Die Brokerverbindung wird erst geöffnet, wenn die Ergebnisse vorliegen; eine Nachricht gilt erst nach der Bestätigung des Brokers mit einem Erfolgs-Reason-Code als veröffentlicht. Nach der letzten Nachricht nennt eine Zeile `CLI-PUBLICATION-SUMMARY` die Anzahl der veröffentlichten, fehlgeschlagenen und nicht versuchten Nachrichten.

Logs gehen immer auf die Standardfehlerausgabe, nie auf die Standardausgabe.

### Konfiguration

Die Konfiguration ist eine JSON-Datei, angegeben mit `--Config <Pfad>` oder, ohne die Option, mit der Umgebungsvariable `WEATHERPARSER_CONFIG`; ohne beides schlägt der Befehl mit `CLI-CONFIG-MISSING` fehl. Elementnamen sind die Eigenschaftsnamen der Optionstypen der Bibliothek und unterscheiden Groß- und Kleinschreibung; unbekannte Elemente, Kommentare und abschließende Kommas werden abgelehnt (`CLI-CONFIG-INVALID`); ein fehlendes Element behält den Standard der Bibliothek. Das eingecheckte Beispiel `examples/weatherparser.example.json` zeigt jeden Abschnitt mit Platzhalter-Hosts (`example.invalid`); kopieren Sie es in eine lokale Datei außerhalb des Repositorys oder unter einen ignorierten Namen (`weatherparser.json`, `*.local.json`) und setzen Sie dort die echten Endpunkte. Seine Dateipfade (`EvidenceCacheDirectory`, `CredentialsFile`) sind Linux-Pfade für das Debian-Zielsystem; unter Windows verwenden Sie absolute Windows-Pfade.

| Abschnitt | Elemente | Verwendet von |
|---|---|---|
| `Dwd` | `ReferenceStationId`; `Catalog` (die URIs der CDC-Stationslisten und -Verzeichnisse und die Grenzen von `CdcCatalogEndpoints`); `Observation` (`DwdObservationOptions`, zum Beispiel `ObservationDeadline`) | `--SelectDWD`, `--ParseDWD` |
| `OsthessenWetter` | `PageUri`, `PageDeadline`, `Stations` (Name, Breite, Länge, optional `AltitudeM`), `PublicationPermitted` | `--ParseOstHessen` |
| `Mosmix` | `Catalog` (`MosmixCatalogEndpoints`, einschließlich `MosmixSRefreshDeadline` und der Einstellungen des Nachweiscaches; `EvidenceCacheDirectory` muss ein absoluter Pfad zu einem existierenden, beschreibbaren Verzeichnis sein); `L` (`SingleStationsDirectory`, `ParseDeadline`, `MaxRunAge`); `S` (`AllStationsUri`, `ParseDeadline`, `MaxRunAge`); `MaxRunAge` ist für `--ParseMOSMIX` des jeweiligen Produkts Pflicht (größer als null, höchstens 240 Stunden) | `--SelectMOSMIX`, `--ParseMOSMIX` |
| `Mqtt` | `Broker` (unten); `Topics` mit `Observation`, `Forecast`, `CdcStationList`, `MosmixLocationList`, jeweils nur mit `TopicTemplate`; `MaxPayloadBytes` | nur MQTT-Modus |
| `Limits` | `ConnectTimeout` (Standard 10 s, 1 s bis 2 min), `PublishTimeout` (10 s je Versuch, 1 s bis 2 min), `MaxPublishAttempts` (3, 1 bis 5), `RetryDelay` (1 s, 0 bis 30 s), `CommandDeadline` (10 min, 1 min bis 1 h), `ShutdownGracePeriod` (5 s, 1 s bis 30 s) | jeder Befehl |
| `Http` | `UserAgent` (1 bis 200 druckbare ASCII-Zeichen, genau wie geschrieben gesendet; Standard `WeatherParser/<version>`) | jeder abrufende Befehl |

Die Bibliothek validiert ihre eigenen Einstellungen vor jeder Anfrage (zum Beispiel `ENDPOINT-INVALID`, `OSTHESSEN-OPTIONS-INVALID`, `MOSMIX-OPTIONS-INVALID`); eine solche ID wird unverändert mit `CLI-CONFIG-INVALID` und Exit-Code 2 protokolliert. QoS und Retain sind nicht konfigurierbar. HTTP: ein Client je Prozess ohne automatische Weiterleitungen (eine Weiterleitung ist ein Abruffehler), ohne automatische Dekomprimierung und ohne clientseitiges Timeout; jede Anfrage wird durch die eigenen Timeouts und Fristen der Bibliothek und durch `Limits.CommandDeadline` begrenzt.

### MQTT-Broker und Zugangsdaten

`Mqtt.Broker` hat `Host` (im MQTT-Modus Pflicht), `Port` (Standard 8883 mit TLS, 1883 ohne), `ProtocolVersion` (`5.0`, Standard, oder `3.1.1`), `ClientId` (1 bis 23 Buchstaben und Ziffern; Standard `weatherparser` gefolgt von 10 zufälligen Hexadezimalzeichen je Aufruf), `KeepAlive` (Standard 30 s, 5 s bis 5 min), `CredentialsFile`, `Tls.CaFile` und `AllowUnencrypted`. Der Client ist MQTTnet 5.2.0.1603; jede Verbindung beginnt mit einer sauberen Sitzung (Clean Session).

- **TLS** ist standardmäßig an und prüft die Zertifikatskette und den Hostnamen; es gibt keine Einstellung, die die Prüfung abschaltet. Ohne `Tls.CaFile` wird der Vertrauensspeicher des Systems verwendet; mit ihr (eine PEM-Datei) ist diese CA der einzige Vertrauensanker. `AllowUnencrypted: true` schaltet TLS ab und protokolliert je Aufruf eine Warnung; verwenden Sie es nur in einem vertrauenswürdigen lokalen Netz.
- **Zugangsdaten** stammen entweder aus den Umgebungsvariablen `WEATHERPARSER_MQTT_USERNAME` und `WEATHERPARSER_MQTT_PASSWORD` (beide oder keine) oder aus der mit `Mqtt.Broker.CredentialsFile` benannten JSON-Datei mit genau den Elementen `Username` und `Password`; beide Quellen gleichzeitig schlagen mit `CLI-CREDENTIALS-INVALID` fehl. Unter Linux muss die Datei mit den Zugangsdaten dem Benutzer gehören, unter dem die CLI läuft (die effektive Benutzer-ID), und darf keine Berechtigungsbits für Gruppe oder andere haben (zum Beispiel Modus `0600`), sonst schlägt der Befehl vor jeder Anfrage oder Brokerverbindung mit `CLI-CREDENTIALS-INVALID` fehl ([CL-06](../p3/P3_CLI_CONTRACT.md), Englisch); die CLI öffnet die Datei einmal, prüft Eigentümer und Modus der geöffneten Datei und liest dieselbe Datei, sie lässt sich also zwischen Prüfung und Lesen nicht austauschen. Unter Windows erfolgt keine Prüfung von Eigentümer oder Berechtigungen. Zugangsdaten werden nie als Argumente angenommen, nie in die Konfigurationsdatei geschrieben, nie protokolliert und nur im MQTT-Modus gelesen. Benennen Sie die Datei `*.credentials.json` (von Git ignoriert) oder bewahren Sie sie außerhalb des Repositorys auf.
- **Versuche**: Die erste Verbindung erhält höchstens `MaxPublishAttempts` Versuche, jeder begrenzt durch `ConnectTimeout`; eine abgewiesene Verbindung (zum Beispiel falsche Zugangsdaten) ist endgültig. Jede Nachricht erhält höchstens `MaxPublishAttempts` Versuche, jeder begrenzt durch `PublishTimeout`, mit `RetryDelay` dazwischen und einer erneuten Verbindung, wenn die Verbindung verloren ging; eine Ablehnung durch den Broker (ein Fehler-Reason-Code) ist endgültig; eine bestätigte Nachricht wird nie erneut gesendet. Eine wiederholte QoS-1-Nachricht kann Abonnenten zweimal erreichen.
- **Einschränkung von MQTT 3.1.1**: Ein 3.1.1-Broker bestätigt ohne Reason-Code und kann daher eine Nachricht bestätigen, die er anschließend verwirft (zum Beispiel wegen einer fehlenden Berechtigung); mit 5.0 wird eine solche Ablehnung als `CLI-MQTT-MESSAGE-REFUSED` gemeldet. Verwenden Sie 5.0, wo der Broker es unterstützt.

Geben Sie dem Brokerbenutzer der CLI nur die Veröffentlichungsrechte für seine konfigurierten Topics.

### OsthessenWetter-Werte

Die Quellenbedingungen von OsthessenWetter sind nicht geklärt (OW-14). `--ParseOstHessen --Output mqtt` schlägt deshalb vor jeder Anfrage mit `CLI-OSTHESSEN-PUBLICATION-NOT-PERMITTED` (Exit-Code 2) fehl, sofern `OsthessenWetter.PublicationPermitted` nicht `true` ist. Setzen Sie es erst, wenn die Zustimmung des Betreibers oder anderweitig geklärte Bedingungen festgehalten sind. JSON- und XML-Ausgabe bleiben für den lokalen Benutzer möglich; die Werte dürfen nicht weiterverbreitet werden.

### Exit-Codes

| Exit-Code | Bedeutung |
|---|---|
| `0` | Vollständiger Erfolg, auch eine leere Auswahl und `--Help` |
| `1` | Vollständiger Fehlschlag zur Laufzeit: Kein angeforderter Eintrag war erfolgreich (Abruf-, Befehls-, Ausgabe-, Verbindungs- oder Veröffentlichungsfehler, eine abgelaufene Befehlsfrist ohne veröffentlichte Nachricht, ein unerwarteter Fehler) |
| `2` | Bedien- oder Konfigurationsfehler, vor jedem Abruf erkannt |
| `3` | Teilweiser Fehlschlag: Mindestens ein angeforderter Eintrag wurde exportiert oder veröffentlicht, und mindestens einer schlug fehl |
| `4` | Abgebrochen durch SIGINT, SIGTERM oder Strg+C, auch wenn vor dem Signal einige Einträge erfolgreich waren |

Ein Eintrag, dessen Ausgabe oder Veröffentlichung fehlschlug, zählt als fehlgeschlagen. Hinweise (zum Beispiel `SELECTION-EMPTY`, `MOSMIX-RUN-STALE`) werden protokolliert und ändern den Exit-Code nicht.

### Fehler-IDs der CLI

Jede Log-Zeile trägt eine ID. Die eigenen IDs der CLI sind unten aufgeführt; die IDs der Bibliothek (zum Beispiel `STATION-ID-INVALID`, `OSTHESSEN-PAGE-UNAVAILABLE`, `MQTT-MAPPING-MISSING`) werden unverändert weitergereicht, in den Logs und in `errors[].error_id` des Exportdokuments. Eine ID vergibt die CLI für einen Fehler der Bibliothek ohne ID: `OBSERVATION-NO-USABLE-READING`, eine `--ParseDWD`-Station ohne nutzbaren Messwert (die `ObservationDatasetRejectedException` der Bibliothek, C-60), ein Eintragsfehler mit der Quelle `dwd:<Stations-ID>` (Ergänzung zu CL-09 vom 2026-10-31).

| Fehler-ID | Stufe | Bedeutung |
|---|---|---|
| `CLI-ARGUMENT-INVALID` | Error | Unbekannte, wiederholte oder falsch platzierte Option, fehlender oder ungültiger Wert (Exit-Code 2) |
| `CLI-COMMAND-MISSING` | Error | Kein Befehl angegeben (Exit-Code 2) |
| `CLI-COMMAND-CONFLICT` | Error | Mehr als ein Befehl angegeben (Exit-Code 2) |
| `CLI-CONFIG-MISSING` | Error | Keine Konfigurationsdatei angegeben, oder die Datei existiert nicht (Exit-Code 2) |
| `CLI-CONFIG-INVALID` | Error | Die Konfiguration ist unlesbar, fehlerhaft oder außerhalb des Bereichs, oder die Bibliothek lehnt eine Einstellung ab (Exit-Code 2) |
| `CLI-CREDENTIALS-INVALID` | Error | Broker-Zugangsdaten fehlen, sind unvollständig, doppelt angegeben oder nicht geschützt (Exit-Code 2) |
| `CLI-DWD-STATION-MISSING` | Error | `--ParseDWD` ohne `--StationID` und ohne `Dwd.ReferenceStationId` (Exit-Code 2) |
| `CLI-OSTHESSEN-PUBLICATION-NOT-PERMITTED` | Error | `--ParseOstHessen` im MQTT-Modus ohne `OsthessenWetter.PublicationPermitted` (Exit-Code 2) |
| `CLI-OUTPUT-EXISTS` | Error | Die Ausgabedatei existiert (Exit-Code 2) oder wurde während des Befehls angelegt (Exit-Code 1) |
| `CLI-OUTPUT-FAILED` | Error, Warning | Ungültiger Pfad oder fehlendes Verzeichnis (Exit-Code 2), Schreibfehler (Exit-Code 1); eine Warnung nennt eine temporäre Datei, die nicht gelöscht werden konnte |
| `CLI-MQTT-CONNECT-FAILED` | Error, Warning | Keine Verbindung zum Broker nach den Versuchen, oder abgewiesen; eine Warnung je fehlgeschlagenem Versuch |
| `CLI-MQTT-PUBLISH-FAILED` | Error, Warning | Eine Nachricht wurde nicht veröffentlicht (Verbindung verloren oder nicht wiederhergestellt); eine Warnung je fehlgeschlagenem Versuch |
| `CLI-MQTT-PUBLISH-TIMEOUT` | Error, Warning | Keine Bestätigung innerhalb von `PublishTimeout` im letzten Versuch; eine Warnung je fehlgeschlagenem Versuch |
| `CLI-MQTT-MESSAGE-REFUSED` | Error | Der Broker lehnte eine Nachricht mit einem Fehler-Reason-Code ab (MQTT 5.0) |
| `CLI-CANCELLED` | Error, Warning | Ein Beendigungssignal brach den Befehl ab (Exit-Code 4); eine Warnung beim Eintreffen des Signals |
| `CLI-DEADLINE-EXCEEDED` | Error, Warning | `Limits.CommandDeadline` ist abgelaufen; eine Warnung vor dem Abruf, wenn sie kürzer ist als die Bibliotheksfrist des Befehls |
| `CLI-UNEXPECTED` | Error | Ein unerwarteter Fehler, zum Beispiel eine Nachricht mit unerwartetem QoS- oder Retain-Kennzeichen (Exit-Code 1) |
| `CLI-PUBLICATION-SUMMARY` | Information | Die Anzahl der veröffentlichten, fehlgeschlagenen und nicht versuchten Nachrichten nach einer MQTT-Veröffentlichung |

Log-Zeilen haben die Form `<UTC-Zeit> UTC <LEVEL> <ID> command=<Befehl> [source=<Quelle>] <Meldung>`, eine Zeile je Eintrag (Zeilenumbrüche in einer Meldung werden durch Leerzeichen ersetzt), zum Beispiel `2026-10-30T06:00:00 UTC ERROR CLI-CONFIG-MISSING command=ParseDWD No configuration file given; use --Config <path> or the environment variable WEATHERPARSER_CONFIG.` Die Meldungen selbst sind englisch (REQ-009). Eine Zeile ohne ID zeigt `-`. Ausnahmen erscheinen als Typ und Meldung, nie mit Stacktrace; Zugangsdaten und Konfigurationsobjekte werden nie protokolliert. Die Mindeststufe ist Information; `--Verbose` ergänzt Debug.

### Abbruch und Fristen

SIGINT, SIGTERM und Strg+C brechen den laufenden Befehl über ein Token ab, das an jeden Aufruf von Bibliothek, Ausgabe und Transport übergeben wird: Die temporäre Datei wird gelöscht (oder in einer Warnung genannt, wenn sie sich nicht löschen lässt), die Veröffentlichungszusammenfassung wird protokolliert, `CLI-CANCELLED` wird protokolliert, und der Exit-Code ist 4; ein unterbrochener Export oder eine unterbrochene Veröffentlichung wird nie als erfolgreich gemeldet. Ein Signal, das erst nach vollständiger Ausgabe eintrifft (die Ausgabedatei an ihren Platz verschoben, das Dokument auf stdout geschrieben oder jede Nachricht vom Broker bestätigt, zum Beispiel während der abschließenden Trennung vom Broker), unterbricht nichts: Die Warnung `CLI-CANCELLED` des Signals wird protokolliert, aber das vollständige Ergebnis und sein Exit-Code bleiben unverändert. Ein zweites Signal oder das Ende von `Limits.ShutdownGracePeriod` nach dem ersten beendet den Prozess sofort mit 4. Unter Windows liefert .NET ein Herunterfahren des Systems (`CTRL_SHUTDOWN_EVENT`) als SIGTERM; die CLI hält das Herunterfahren dann bis zum Ende ihres Prozesses auf, damit der Befehl seine Aufräumarbeiten abschließen kann. `Limits.CommandDeadline` begrenzt Abruf, Serialisierung, Ausgabe und Veröffentlichung zusammen; läuft sie ab, wird `CLI-DEADLINE-EXCEEDED` protokolliert, und der Exit-Code ist 1, oder 3, wenn bereits Nachrichten veröffentlicht wurden. Die eigenen Fristen der Bibliothek bleiben in Kraft; eine Befehlsfrist, die kürzer ist als die Bibliotheksfrist des Befehls, wird vor dem Abruf als Warnung gemeldet. Nach einem Abbruch oder einer abgelaufenen Frist läuft ein Aufräumschritt: Eine offene Brokerverbindung wird regulär geschlossen, begrenzt durch `Limits.ConnectTimeout`; der Prozess kann deshalb bis zu dieser Dauer nach der Befehlsfrist enden (ein zweites Signal oder die Karenzzeit beendet ihn sofort).

### Zeitplanung

Starten Sie die CLI aus einem Scheduler und werten Sie ihren Exit-Code aus; für WeatherParser selbst muss kein Scheduler installiert werden (REQ-007, REQ-008). Beispiel für einen Cron-Eintrag (alle 10 Minuten, Platzhalterpfade), der das Log aufbewahrt und jeden Exit-Code außer 0 an das Systemprotokoll meldet:

```text
*/10 * * * * WEATHERPARSER_CONFIG=/etc/weatherparser/weatherparser.json /opt/weatherparser/current/WeatherParser --ParseDWD < /dev/null >> /var/log/weatherparser/weatherparser.log 2>&1 || logger -t weatherparser "--ParseDWD ended with exit code $?"
```

Installationsstruktur, Dienstbenutzer, Log-Behandlung und weitere Beispiele zur Zeitplanung stehen in [OPERATIONS.md](OPERATIONS.md). In n8n kann ein Knoten „Execute Command“ `/opt/weatherparser/current/WeatherParser --ParseDWD --Output json` ausführen, das JSON-Dokument von der Standardausgabe lesen und nach dem Exit-Code verzweigen (0 Erfolg, 3 teilweise, 1, 2 oder 4 Fehlschlag). Wählen Sie das Intervall je Quelle: DWD-10-Minuten-Daten höchstens alle 10 Minuten, `MOSMIX_L` nach seinen Läufen um 03, 09, 15 und 21 UTC, `MOSMIX_S` stündlich und eine Aktualisierung des MOSMIX_S-Nachweises selten (sie lädt die vollständige MOSMIX_S-Datei herunter).

## Rolle im Workflow und Ausführung

Die Beispiel-CLI ist eine unabhängig aufrufbare Komponente des gesamten Wetterdaten- und Automatisierungsworkflows. Ihre Aufgaben sind Abruf, Parsen, Stationsauswahl, Normalisierung und die angeforderte Ausgabe. Nachgelagerte Speicherung, Anzeige und weitere Aktionen gehören zu anderen Komponenten.

**Jeder Aufruf führt genau einen Befehl aus und endet dann.** Zeitplanung und Wiederholungen über Aufrufe hinweg sind externe Aufgaben, zum Beispiel in n8n oder Cron. WeatherParser hat keinen eingebauten Scheduler und läuft nicht als dauerhafter Dienst. Ein Befehl kann Ergebnisse für mehrere Stationen liefern. Die CLI muss eine nicht interaktive Ausführung, aussagekräftige Exit-Codes und getrennte Daten- und Log-Ausgabe unterstützen.

Beispiel-Workflows:

- n8n oder Cron → WeatherParser → MQTT → Dashboard oder andere Konsumenten.
- Externer Workflow → WeatherParser → JSON-/XML-Datei → nachgelagerte Verarbeitung.

Es besteht keine feste Abhängigkeit von n8n; andere Programme und Shell-Skripte können die Komponente aufrufen.

## JSON- und XML-Export

Alle fünf Befehle der [Beispiel-CLI](#beispiel-cli) unterstützen `--Output mqtt|json|xml`, mit MQTT als Standard. JSON- und XML-Export benötigen keinen MQTT-Broker. `--OutputFile <Pfad>` schreibt eine UTF-8-Datei; ohne Pfad geht das vollständige Dokument auf die Standardausgabe. Logs bleiben auf der Standardfehlerausgabe. Bestehende Dateien werden nie überschrieben.

```text
WeatherParser --ParseDWD --StationID <DWD-STATIONS-ID> --Output json --OutputFile weather.json
WeatherParser --ParseOstHessen --StationNamePattern "Fulda*" --Output xml --OutputFile weather.xml
WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --Output json
```

Beide Exportformate enthalten dieselben Beobachtungs- oder Vorhersagedaten und UTC-Zeitstempel wie MQTT, einschließlich der Kennzeichen für geschätzte Daten. Ergebnisse mehrerer Stationen, Stationslisten und Vorhersageschritte werden in einem vollständigen Dokument zusammengefasst. Die MQTT-Nutzdaten des Bibliotheksserialisierers bleiben JSON.

### Exportentitäten und Serialisierung

Jeder fachliche Exportobjekttyp hat seine eigene stark typisierte C#-Entität mit `[Serializable]`, einschließlich verschachtelter Exportobjekte. Dazu gehören Stationslisten und -einträge, Beobachtungsdatensätze, Vorhersageläufe und -schritte sowie Qualitäts- und Zeitraummetadaten. Wiederverwendbare Wertentitäten dürfen geteilt werden.

JSON verwendet `System.Text.Json.JsonSerializer`; XML verwendet `System.Xml.Serialization.XmlSerializer`. Beide Formate und das MQTT-JSON verwenden dieselben vorbereiteten Exportentitäten. Serialisiererspezifische Regeln steuern Feldnamen, weggelassene Werte und UTC ohne Zeitzonensuffix. Serialisiererdienste verwenden Konstruktorinjektion. Die Bibliothek implementiert diese Entitäten und beide Serialisierer (P2.02 bis P2.08); das XML-Ausgabeschema wird aus denselben Abbildungen erzeugt und liegt im Paket unter `schemas/export/v1/weatherparser-export-v1.xsd` (P2.09).

## MOSMIX-Vorhersagen

Die Bibliotheksparser beider Produkte sind implementiert und in [MOSMIX-Vorhersagen (Bibliothek)](#mosmix-vorhersagen-bibliothek) dokumentiert. Die CLI-Befehle `--SelectMOSMIX` und `--ParseMOSMIX` sind Teil der [Beispiel-CLI](#beispiel-cli).

`--SelectMOSMIX` verwendet dieselben Standort- und Filterparameter wie `--SelectDWD`, durchsucht aber den DWD-MOSMIX-Stationskatalog. Die Ergebnisse enthalten MOSMIX-ID, Name, Koordinaten, Höhe, Entfernung, Höhenunterschied und nachgewiesene Vorhersageelemente. MOSMIX-IDs sind eigenständige, möglicherweise alphanumerische Kennungen und werden nicht aus CDC-Stations-IDs abgeleitet.

`--ParseMOSMIX --Product MOSMIX_S|MOSMIX_L --MOSMIXID <id>` ruft den neuesten verfügbaren Vorhersagelauf des Standorts ab. Ausgabezeit (`issued_at`), Abrufzeit (`retrieved_at`) und Vorhersagezeiten (`forecasts[].valid_at`) bleiben getrennt und werden als UTC ohne Zeitzonensuffix ausgegeben. Intervallgrößen behalten ihre Bezugszeiträume. Beide Produktparser sind erforderlich; ihre Elementabbildungen und Aktualitätsgrenzen sind in [MOSMIX-Vorhersagen (Bibliothek)](#mosmix-vorhersagen-bibliothek) dokumentiert.

MQTT-Vorhersagen verwenden `haus/weather/forecasts/<source>/state`; MOSMIX-Stationslisten verwenden `haus/weather/stations/mosmix/selection`. Das Präfix ist konfigurierbar. Die Beispiel-CLI konfiguriert den Bibliotheksserialisierer für eine Retained-Nachricht je vollständigem Lauf, Standort und Produkt; ein getrennter Brokertransport sendet sie. JSON- und XML-Exporte verwenden denselben normalisierten Vorhersagevertrag.

```text
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> --Output json --OutputFile forecast.json
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> --Output xml --OutputFile forecast.xml
```

### Getrennte MOSMIX-Parser und Aktualisierungsfrequenz

Die Bibliothek stellt zwei getrennte Parser mit getrennten Schnittstellen bereit: `MosmixSParser` / `IMosmixSParser` für `MOSMIX_S` und `MosmixLParser` / `IMosmixLParser` für `MOSMIX_L`. Beide erhalten ihre eigenen Endpunkte und Optionen über Dependency Injection und haben eine unabhängige Konfiguration. Gemeinsame KML/KMZ-Hilfsfunktionen dürfen wiederverwendet werden, aber ein umschaltbarer Parser kann die zwei öffentlichen Verträge nicht ersetzen. Fehlende Daten führen nie zu einem stillen Ausweichen auf das andere Produkt.

| Produkt | Ausgabefrequenz des DWD | Laufzeiten |
|---|---|---|
| `MOSMIX_S` | Stündlich | Jede Stunde ein neuer Produktlauf |
| `MOSMIX_L` | Viermal täglich | 03, 09, 15 und 21 UTC |

Dies beschreibt die Zyklen des Anbieters, keinen internen Scheduler und keine garantierte Verfügbarkeit der Datei genau zum Laufzeitpunkt. Halten Sie Ausgabe-, Abruf- und Gültigkeitszeiten getrennt. Lesen Sie den tatsächlichen Vorhersagehorizont aus der Zeitachse der Datei; eine neue Datei beweist nicht, dass alle enthaltenen Schritte neu berechnet wurden. Legen Sie Aktualitätsgrenzen und externe Abrufpläne je Produkt fest.

Das Beispiel behält `--ParseMOSMIX` und verlangt zusätzlich `--Product MOSMIX_S|MOSMIX_L`, um den Bibliotheksparser auszuwählen. Fehlende oder unbekannte Produkte werden vor dem Abruf abgelehnt. `--SelectMOSMIX` behält die bestehenden geografischen und Höhenoptionen und unterscheidet bekannte Verfügbarkeit und Elemente je Produkt; unbekannte Verfügbarkeit wird ausdrücklich gekennzeichnet. Ausgabemetadaten, Quellkennungen und konfigurierte MQTT-Topics müssen S und L unterscheiden, damit ein Retained-Zustand nicht versehentlich ersetzt wird.

Quelle, geprüft am 2026-10-14: [DWD-Verfahrensbeschreibung, Abschnitt 2.1.3/Tabelle 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). Die Zyklen wurden in P7.02 gegen die Dateien vom 2026-10-26 erneut geprüft ([P7-Vorhersagevertrag](../p7/P7_FORECAST_CONTRACT.md), Abschnitt 6, Englisch); prüfen Sie sie vor einem Release erneut.

## Zielplattform

Verbindliche Plattformvorgabe (2026-10-14): .NET Version 10 oder neuer und Debian Version 13 Trixie oder neuer. Das anfängliche Zielframework von Bibliothek und Beispiel ist `net10.0`; die Mindestarchitektur ist ARM64 (`linux-arm64`), insbesondere der NanoPi R76S. Neuere stabile .NET- und Debian-Versionen werden in die Matrix der verifizierten Kompatibilität aufgenommen; Vorabversionen sind nicht automatisch zugesichert. Das festgelegte SDK und die verifizierte Werkzeugkette stehen in [DEPENDENCIES.md](../../DEPENDENCIES.md) (Englisch), die Entwicklungsumgebung in [CONTRIBUTING.md](../../CONTRIBUTING.md) (Englisch) und die verifizierten Plattformen in [COMPATIBILITY.md](COMPATIBILITY.md); das Bereitstellungsmodell der Beispiel-CLI ist ein frameworkabhängiges `linux-arm64`-Publish auf der Laufzeit 10.0.14 oder neuer ([OPERATIONS.md](OPERATIONS.md), RL-07 A); weitere ARM-Varianten sind noch nicht entschieden. Bibliothek, tatsächlicher Paketkonsument und Beispiel werden auf Debian 13/ARM64 mit .NET 10 verifiziert. Plattformanforderungen werden von verifizierter Kompatibilität unterschieden; ein Laufzeiterfolg wird nie ohne dokumentierte Tests behauptet.

Quellen (geprüft am 2026-10-14): [.NET-Support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [.NET-10-Betriebssystemmatrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md), [Debian-Veröffentlichungen](https://www.debian.org/releases/).

## Release und Installation

Geplant gegenüber umgesetzt (REQ-090), Stand 2026-11-03:

| Punkt | Status |
|---|---|
| Paket `WeatherParser.Parsing` 1.0.0 mit XML-Dokumentation, Symbolpaket und Auflistung der öffentlichen API | Umgesetzt und vom Release-Build gebaut (P9.05, P9.06; Version 1.0.0 vom Maintainer am 2026-11-03 festgelegt) |
| Release-Abnahme des Kandidaten auf dem NanoPi R76S | Verifiziert in P9.10 auf Debian 13 ARM64 mit der Laufzeit 10.0.14, am Build derselben Bibliotheks- und CLI-Quellen als Version 0.1.0: das Paket ohne Quellcode genutzt, die CLI nach [OPERATIONS.md](OPERATIONS.md) installiert und als Dienstbenutzer offline und live ausgeführt, Signale, Update und Rollback sowie eine zeitgesteuerte Serie über 36,5 Stunden ([P9.10-Protokoll](../p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md), Englisch) |
| Veröffentlichung auf nuget.org | Eine eigene Entscheidung des Maintainers; die Paketdatei ist dem GitHub-Release `v1.0.0` beigefügt; das Repository ist seit dem 2026-11-03 öffentlich, die Paketmetadaten enthalten keine Repository-URL ([Protokoll](../REPOSITORY_VISIBILITY.md), Englisch) |
| Installation von nuget.org: `dotnet add package WeatherParser.Parsing --version 1.0.0` ohne lokale Paketquelle | Funktioniert erst, wenn das Paket dort veröffentlicht ist |
| Beispiel-CLI auf Debian 13 ARM64 | Umgesetzt; Bereitstellung in [OPERATIONS.md](OPERATIONS.md) dokumentiert (RL-07 A); die CLI ist nicht Teil des Pakets und wird aus dem Quellcode gegen das Paket gebaut (RL-08 A) |

Ohne eine Veröffentlichung auf nuget.org verwenden Konsumenten die mit Prüfsumme versehene `.nupkg` des GitHub-Releases aus einer lokalen Paketquelle (siehe [Erste Schritte](#erste-schritte)). Jede Änderung, die verifizierten Plattformen und die bekannten Einschränkungen mit ihren Verantwortlichen stehen in [CHANGELOG.md](../../CHANGELOG.md) (Englisch). Die Quellenbedingungen von DWD und OsthessenWetter sind unter [Datenquellen](#datenquellen) und in der Betriebsanleitung zusammengefasst; produktive Nutzung und Weiterverbreitung von OsthessenWetter-Werten werden bis zur Klärung nicht akzeptiert (OW-14).

## Datenquellen

- [DWD Climate Data Center: 10-Minuten-Beobachtungen](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/)
- [DWD: Datensatzbeschreibung Temperatur, Feuchte und Luftdruck](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/air_temperature/DESCRIPTION_obsgermany_climate_10min_air_temperature_en.pdf)
- [DWD: Datensatzbeschreibung Wind](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/wind/DESCRIPTION_obsgermany_climate_10min_wind_en.pdf)
- [DWD: Datensatzbeschreibung Niederschlag](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/precipitation/DESCRIPTION_obsgermany-climate-10min-precipitation_en.pdf)
- [DWD: Datensatzbeschreibung Solar](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/solar/DESCRIPTION_obsgermany_climate_10min_solar_en.pdf)
- [OsthessenWetter-Messwerte](https://osthessen-wetter.de/messwerte/)
- [DWD MOSMIX: Katalog, Formate und Elementdefinitionen](https://www.dwd.de/DE/leistungen/opendata/hilfe.html)
- [DWD MOSMIX_L: einzelne Vorhersagestandorte](https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/)

Der Code steht unter der [MIT-Lizenz](../../LICENSE). Sie gewährt keine Rechte an Wetterdaten oder Websites Dritter. DWD-Daten sind offene Daten unter CC BY 4.0 und behalten bei der Weiterveröffentlichung die Quellenangabe „Deutscher Wetterdienst (DWD)“; der Open-Data-Server des DWD speichert die anfragende IP-Adresse höchstens 7 Tage. OsthessenWetter veröffentlicht keine Lizenz für seine Werte, und sein Impressum untersagt die Wiederverwendung in anderen Veröffentlichungen ohne Zustimmung des Autors; produktive Nutzung und Weiterverbreitung seiner Werte werden deshalb bis zur Klärung nicht akzeptiert (OW-14). Quellenbedingungen, Quellenangabe und zulässige Abrufhäufigkeit müssen für jeden Adapter gesondert dokumentiert und vor der produktiven Nutzung geprüft werden. Eine öffentlich verfügbare OsthessenWetter-API wird nicht vorausgesetzt.

## Mitwirken und Entwicklung

Die Abschnitte der englischen README für Mitwirkende an diesem Repository (Designregeln, Entwicklungsumgebung, Projektstruktur und die zugrunde liegenden Entscheidungen) sind nicht übersetzt. Sie stehen ab [Contributing and development](../../README.md#contributing-and-development) in der englischen README; jedes Entwicklerverfahren steht in [CONTRIBUTING.md](../../CONTRIBUTING.md), die Regeln für Mitwirkende in [AGENTS.md](../../AGENTS.md) und die Anforderungen in [REQUIREMENTS.md](../../REQUIREMENTS.md), alle auf Englisch.

## Sprachen

Englisch ist die maßgebliche Sprache der Projektdokumentation. Die für Nutzer bestimmten Dokumente – diese README, die Anleitung zur Bibliotheksnutzung, die Betriebsanleitung, die Kompatibilitätsübersicht, die Anleitung zum Melden von Problemen und die Anleitung zum Hinzufügen von Datenquellen – gibt es zusätzlich als deutsche Übersetzung in `docs/de/`. Die Übersetzung wird vor jeder neuen Version mit der englischen Fassung abgeglichen (Entscheidung des Nutzers vom 2026-11-03); bei Abweichungen gilt die englische Fassung. Entwicklungsdokumentation (Pläne, Verträge, Protokolle, Anforderungen und Regeln für Mitwirkende) wird nicht übersetzt.

Programmausgaben (Hilfe, Logs, Status- und Fehlermeldungen) und Code-Kommentare sind englisch; Befehle, Optionen, Datenfelder, IDs und Einheiten sind sprachunabhängig. Der verbindliche MIT-Lizenztext in `LICENSE` bleibt englisch.

## Dokumentationsübersicht

| Dokument | Inhalt |
| --- | --- |
| [LIBRARY_USAGE.md](LIBRARY_USAGE.md) | Die Bibliothek Schritt für Schritt in einer Anwendung verwenden |
| [Bibliotheksreferenz](#bibliotheksreferenz) | Jede Quelle und der MQTT-Serialisierer im Detail |
| [EXTENDING_DATA_SOURCES.md](EXTENDING_DATA_SOURCES.md) | Eine Datenquelle hinzufügen |
| [REPORTING_ISSUES.md](REPORTING_ISSUES.md) | Ein Problem melden oder eine Quelle anfragen |
| [Beispiel-CLI](#beispiel-cli), [OPERATIONS.md](OPERATIONS.md) | Das Beispiel-Kommandozeilenprogramm und sein Betrieb auf Debian 13 ARM64 |
| [CHANGELOG.md](../../CHANGELOG.md) (Englisch) | Änderungen je Version, Kompatibilität und bekannte Einschränkungen |
| [COMPATIBILITY.md](COMPATIBILITY.md), [DEPENDENCIES.md](../../DEPENDENCIES.md) (Englisch) | Verifizierte Plattformen; Versionen und Lizenzen der Werkzeugkette und der Abhängigkeiten |
| [CONTRIBUTING.md](../../CONTRIBUTING.md), [AGENTS.md](../../AGENTS.md), [REQUIREMENTS.md](../../REQUIREMENTS.md) (Englisch) | Entwicklerverfahren, Regeln für Mitwirkende, Anforderungen |
| [PHASE_STATUS.md](../development/PHASE_STATUS.md), [DEVELOPMENT_PLAN.md](../../DEVELOPMENT_PLAN.md) (Englisch) | Entwicklungshistorie und Pläne |

Der Code steht unter der [MIT-Lizenz](../../LICENSE); die Lizenz umfasst keine Wetterdaten oder Websites (siehe [Datenquellen](#datenquellen)).
