# WeatherParser

[Deutsch](README.md) | [English](README.en.md)

**Phasenstatus (15.10.2026): P0 abgeschlossen; P1.01 und P1.02-Prüfung abgeschlossen; SDK-Aktualisierung und übrige P1-Abnahmen stehen aus.**

WeatherParser ist primär eine geplante C#-NuGet-Bibliothek für DWD-/OsthessenWetter-Messdaten und MOSMIX-Vorhersagen. Veröffentlicht wird das Paket mit Parsern, Ausgabeentitäten, generischem MQTT-Serializer und DI-Integration. Die CLI ist nur ein Nutzungsbeispiel, das dieses Paket konsumiert.

## NuGet-Hauptprodukt und Beispiel-CLI

Das zur Veröffentlichung bestimmte Hauptprodukt ist eine wiederverwendbare NuGet-Bibliothek. Sie enthält DWD-/OsthessenWetter-/MOSMIX-Parser einschließlich benötigtem Quellenabruf und Normalisierung, die öffentlichen Ausgabeentitäten, einen generischen MQTT-Serializer sowie die Dependency-Injection-Integration. Die CLI ist ausschließlich ein Nutzungsbeispiel und konsumiert eine versionierte Paketreferenz; andere Anwendungen können das Paket ohne CLI verwenden.

Jeder Parser erhält seine benötigten Quell-URLs ausdrücklich über Konstruktorparameter, vorzugsweise typisiert als `Uri`. Mehrere Endpunkte werden getrennt benannt. Die DI-Integration übernimmt die vom Konsumenten gelieferten Endpunkte und übergibt sie an die jeweiligen Konstruktoren; sie erfindet keine fest eingebauten Quellen oder globalen URI-Singletons. HTTP, Zeit und weitere Services bleiben ebenfalls über Konstruktoren injizierbar. Die Bibliothek liest keine CLI-Argumente und keine anwendungsspezifischen Konfigurationsdateien oder Umgebungsvariablen selbstständig ein.

Der MQTT-Serializer ist quellenunabhängig: Er serialisiert geeignete Ausgabeentitäten und erzeugt eine Nachricht mit Topic, JSON-Payload, QoS und Retain-Einstellung. Topics bzw. Topic-Vorlagen und deren Zuordnung zu Nachrichtentypen stammen aus der Konfiguration des Konsumenten, nicht aus festen Wetterpfaden im Paket. Aufgelöste Publish-Topics müssen gültig sein; fehlende Zuordnungen und unaufgelöste Platzhalter erzeugen definierte Fehler. Die bisherigen `haus/weather/...`-Topics sind nur konfigurierbare Beispiele. Serializer und tatsächlicher Brokertransport sind getrennt; die Serialisierung verlangt keine Brokerverbindung und benötigt keine Zugangsdaten.

Die Bibliothek stellt eine dokumentierte DI-Registrierung für ihre Parser, Serializer und benötigten Services bereit. Der Konsument stellt Endpunkte, Topic-Konfiguration und externe Abhängigkeiten bereit. Jede Doing-Klasse besitzt ein Interface. Ausgabeentitäten bleiben eigene `[Serializable]`-Typen. Testbarkeit, UTC-Vertrag und JSON-/XML-Kompatibilität bleiben erhalten.

Das Beispiel-CLI übernimmt Argumente, Anwendungskonfiguration, Ein-Befehl-Prozessablauf, Exitcodes, Datei-/Konsolenexport und gegebenenfalls MQTT-Versand. Der Paketaufruf ist sein einziger Weg zur Parserlogik; keine Parserkopien. Öffentliche Paket-API, Paketversionierung und unabhängiger Paketkonsum stehen im Mittelpunkt der Abnahme. Paket-ID, Feed und Veröffentlichungsvorgang sind vor Veröffentlichung zu entscheiden; derzeit existiert kein gebautes oder veröffentlichtes Paket.
## Sprachen

Die Projektdokumentation wird vollständig auf Deutsch und Englisch gepflegt. Jede inhaltliche Änderung muss beide Sprachfassungen synchron aktualisieren. Anforderungen besitzen in beiden Fassungen dieselben IDs; CLI-Befehle, Datenfelder, Einheiten und Beispiele werden nicht übersetzt.

**Alle Codekommentare werden auf Englisch verfasst**, einschließlich Inline-/Blockkommentaren, XML-Dokumentationskommentaren sowie Kommentaren in Skripten und Konfigurationsbeispielen. Alle vom Programm erzeugten Laufzeitmeldungen sind Englisch. Der verbindliche MIT-Lizenztext in `LICENSE` bleibt auf Englisch.

## Projektstatus

Stand: 15.10.2026. Das lokale Git-Repository, die Dokumentation und eine Solution mit Bibliotheks-/Testprojekt sind angelegt. **Es gibt noch keine Fachimplementierung, Testmethoden oder ausführbare CLI-Befehle.** Die folgenden Aufrufe beschreiben die geplante Schnittstelle; sie sind noch nicht ausführbar. Die Veröffentlichung auf GitHub als Open-Source-Projekt ist vorgesehen, aber noch nicht erfolgt.

## Rolle im Gesamtworkflow und Betrieb

Die Beispiel-CLI ist ein separat aufrufbarer Baustein im gesamten Wetterdaten- und Automationsworkflow. Er übernimmt Abruf, Parsing, Stationsauswahl bzw. Aufbereitung und die gewünschte Ausgabe. Nachfolgende Speicherung, Anzeige und weitere Aktionen liegen bei anderen Workflow-Bausteinen.

**Pro Aufruf wird genau ein Befehl ausgeführt; anschließend beendet sich das Programm.** Zeitliche Steuerung und laufübergreifende Wiederholungen erfolgen extern, beispielsweise durch n8n oder cron. WeatherParser enthält keinen eigenen Scheduler und läuft nicht als dauerhafter Dienst. Mehrere Stationsergebnisse innerhalb eines einzelnen Befehls bleiben möglich. Die CLI muss nichtinteraktiv mit auswertbaren Exitcodes sowie getrennten Daten- und Logausgaben nutzbar sein.

Beispielabläufe:

- n8n oder cron → WeatherParser → MQTT → Dashboard bzw. weitere Verbraucher.
- Externer Workflow → WeatherParser → JSON-/XML-Datei → nachgelagerte Verarbeitung.

Eine feste Abhängigkeit von n8n besteht nicht; auch andere Programme oder Shell-Skripte können den Baustein aufrufen.

## Zielplattform

Verbindliche Plattformvorgabe (14.10.2026): .NET ab Version 10 und Debian ab Version 13 Trixie. Initiales Ziel-Framework für Bibliothek und CLI-Beispiel ist `net10.0`; Mindestarchitektur ist ARM64 (`linux-arm64`), insbesondere NanoPi R76S. Neuere stabile .NET-/Debian-Versionen werden in die geprüfte Kompatibilitätsmatrix aufgenommen; Preview-Versionen sind nicht automatisch zugesichert. Visual Studio 2026 und SDK 10.0.303 sind als lokale Ausgangsbasis festgestellt; Paketversionen, weitere ARM-Varianten und Deployment-Modell bleiben festzulegen. Bibliothek, echter Paketkonsument und Beispiel müssen auf Debian 13/ARM64 mit .NET 10 geprüft werden. Plattformanforderung und tatsächlich nachgewiesene Kompatibilität bleiben getrennt; ohne dokumentierten Test kein behaupteter Laufzeiterfolg.

Quellen (geprüft am 14.10.2026): [.NET-Support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [.NET-10-OS-Matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md), [Debian-Versionen](https://www.debian.org/releases/).

## Geplante Befehle des CLI-Nutzungsbeispiels

```text
WeatherParser --<command> [optionen]
```

| Befehl | Zweck | Fachliche Optionen |
|---|---|---|
| `--SelectDWD` | Passende DWD-Stationen suchen und als MQTT-Stationsliste veröffentlichen | `--Latitude`, `--Longitude`, `--Altitude`, `--MaxDistance`, `--MaxAltDifference` |
| `--ParseDWD` | Messdaten einer DWD-Station abrufen und veröffentlichen | `--StationID`; alternativ konfigurierte Bezugsmessstation |
| `--ParseOstHessen` | Messdaten der passenden OsthessenWetter-Stationen abrufen und veröffentlichen | `--StationNamePattern` |
| `--SelectMOSMIX` | Passende MOSMIX-Vorhersagestandorte suchen | `--Latitude`, `--Longitude`, `--Altitude`, `--MaxDistance`, `--MaxAltDifference` |
| `--ParseMOSMIX` | Wettervorhersage für einen MOSMIX-Standort abrufen | `--MOSMIXID`, `--Product` (`MOSMIX_S`/`MOSMIX_L`) |

Beispiele der geplanten Verwendung:

```text
WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200
WeatherParser --SelectDWD --Latitude "50° 33′ 18″ N" --Longitude "9° 40′ 26″ E" --Altitude 300 --MaxDistance 50 --MaxAltDifference 200
WeatherParser --ParseDWD --StationID <DWD-STATIONS-ID>
WeatherParser --ParseDWD
WeatherParser --ParseOstHessen --StationNamePattern "Fulda*"
WeatherParser --SelectMOSMIX --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID>
WeatherParser --ParseMOSMIX --Product MOSMIX_S --MOSMIXID <MOSMIX-ID>
```

Die Beispielkoordinaten zeigen zwei Eingabeformate; die Werte sind nicht exakt identisch. Die Höhe ist eine Beispieleingabe. Das Stationsmuster ist ein Suchbeispiel und garantiert keinen Treffer. Die Schreibweisen `Longitude` und `AltitudeDifference` sind verbindlich; `Langitude` und `AltitudeDiffeference` aus der ursprünglichen Beschreibung sind Tippfehler.

Fehlt bei `ParseDWD` sowohl `StationID` als auch eine konfigurierte Bezugsmessstation, muss die Anwendung einen Fehler protokollieren und mit Fehlerstatus enden. Eine Station wird niemals stillschweigend gewählt. Ein expliziter CLI-Wert hat Vorrang vor dem Konfigurationswert.

## Geplanter JSON- und XML-Export

Alle fünf Befehle erhalten `--Output mqtt|json|xml`. Standard ist MQTT. JSON und XML können unabhängig von einem MQTT-Broker exportiert werden. `--OutputFile <pfad>` schreibt eine UTF-8-Datei; ohne Dateipfad wird das vollständige Dokument auf Standardausgabe ausgegeben. Logs bleiben auf Standardfehler. Bestehende Dateien werden nicht stillschweigend überschrieben.

```text
WeatherParser --ParseDWD --StationID <DWD-STATIONS-ID> --Output json --OutputFile weather.json
WeatherParser --ParseOstHessen --StationNamePattern "Fulda*" --Output xml --OutputFile weather.xml
WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --Output json
```

Die Aufrufe sind noch nicht implementiert. Beide Exportformate enthalten dieselben fachlichen Mess- bzw. Vorhersagedaten und UTC-Zeitstempel wie MQTT, einschließlich der Kennzeichnung geschätzter Datumsangaben. Mehrere Stationsergebnisse, Stationslisten bzw. Vorhersagezeitschritte werden in einem einzigen vollständigen Dokument zusammengefasst. MQTT-Nachrichten des Bibliotheks-Serializers bleiben JSON.

### Exportentitäten und Serialisierung

Jeder fachliche Exportobjekttyp erhält eine eigene stark typisierte C#-Entität mit `[Serializable]`, einschließlich verschachtelter Exportobjekte. Dazu gehören Stationslisten und ihre Einträge, Messdatensätze, Vorhersageläufe und -zeitschritte sowie Qualitäts-/Zeitraumdaten. Wiederverwendbare Wertentitäten können gemeinsam genutzt werden.

JSON wird über `System.Text.Json.JsonSerializer`, XML über `System.Xml.Serialization.XmlSerializer` erzeugt. Beide Formate verwenden dieselben aufbereiteten Exportentitäten; auch MQTT-JSON nutzt diese Entitäten. Feldnamen, fehlende Werte und UTC ohne Zeitzonensuffix werden durch die jeweiligen Serialisierungsregeln festgelegt. Serializer-Dienste werden per Konstruktor injiziert. Dies ist eine Architekturvorgabe; Entitäten und Anwendungscode sind noch nicht implementiert.

## Gemeinsame Messdaten

Alle Quellen verwenden denselben Datenvertrag. Zeitstempel sind immer **UTC im Format `YYYY-MM-DDTHH:mm:ss`, ohne `Z` oder Offset**. Empfänger müssen diese Konvention ausdrücklich berücksichtigen; erst die Anzeige rechnet in Ortszeit um.

Messgrößen: Temperatur in °C, Luftdruck in hPa und mmHg, relative Feuchtigkeit in %, Windrichtung in Grad, Windgeschwindigkeit in km/h, Beleuchtungsstärke in Lux, Sonnenstrahlung in W/m² und Regenintensität in mm/h. Fehlende Werte werden weggelassen. `0` bezeichnet einen tatsächlichen Messwert.

hPa und mmHg beziehen sich auf denselben Ausgangswert und dieselbe Druckreferenz. Lux und W/m² sind getrennte Größen. Zeitlich gemittelte Werte erhalten einen nachvollziehbaren Messzeitraum. Vorhersagen werden durch `--ParseMOSMIX` separat ausgegeben und niemals als Messdaten behandelt.

OsthessenWetter zeigt in der untersuchten Messwertübersicht je Station nur eine Uhrzeit. Nach Projektentscheidung darf das Datum anhand des Abrufzeitpunkts und der Quellzeitzone `Europe/Berlin` abgeleitet werden. Der vollständige Zeitstempel wird anschließend nach UTC umgerechnet und **als geschätzt gekennzeichnet**. Sommerzeitwechsel und nicht eindeutig auflösbare Uhrzeiten benötigen eine explizite Behandlung; das Abrufdatum beweist keine Aktualität des Messwerts.

## Geplante MOSMIX-Vorhersagen

`--SelectMOSMIX` verwendet dieselben Standort- und Filterparameter wie `--SelectDWD`, aber den DWD-MOSMIX-Stationskatalog. Die Liste enthält MOSMIX-ID, Name, Koordinaten, Höhe, Entfernung, Höhenunterschied und nachgewiesene Vorhersageelemente. MOSMIX-IDs sind eigenständige, auch alphanumerische Kennungen; sie werden nicht aus CDC-Messstations-IDs abgeleitet.

`--ParseMOSMIX --Product MOSMIX_S|MOSMIX_L --MOSMIXID <id>` liest den neuesten verfügbaren Vorhersagelauf für den angegebenen Standort. Ausgabezeitpunkt (`issued_at`), Abrufzeitpunkt (`retrieved_at`) und Vorhersagezeitpunkte (`forecasts[].valid_at`) bleiben getrennt und werden als UTC ohne Zeitzonensuffix ausgegeben. Intervallgrößen behalten ihre Bezugszeiträume. Beide Produktparser sind verbindlich; ihre konkrete Parameterabbildung und Aktualitätsgrenzen werden vor Implementierung dokumentiert.

Im MQTT-Modus gehen Vorhersagen an `haus/weather/forecasts/<source>/state`, MOSMIX-Stationslisten an `haus/weather/stations/mosmix/selection`; das Präfix ist konfigurierbar. Das CLI-Beispiel konfiguriert den Bibliotheks-Serializer für eine retained-Nachricht je vollständigem Vorhersagelauf, Standort und Produkt; den Versand übernimmt der Brokertransport. JSON- und XML-Export verwenden denselben normalisierten Vorhersagevertrag.

```text
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> --Output json --OutputFile forecast.json
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> --Output xml --OutputFile forecast.xml
```

### Getrennte MOSMIX-Parser und Aktualisierung

Die Bibliothek bietet zwei eigenständige Parser mit eigenen Interfaces: `MosmixSParser` / `IMosmixSParser` für `MOSMIX_S` und `MosmixLParser` / `IMosmixLParser` für `MOSMIX_L`. Beide erhalten ihre jeweiligen Quell-URLs über Konstruktorparameter und werden separat über die DI-Integration konfiguriert. Gemeinsame KML-/KMZ-Hilfslogik darf wiederverwendet werden; ein einzelner umschaltbarer Parser ersetzt die beiden öffentlichen Verträge nicht. Bei fehlenden Daten gibt es keinen stillschweigenden Wechsel zum anderen Produkt.

| Produkt | DWD-Ausgabehäufigkeit | Laufzeitangaben |
|---|---|---|
| `MOSMIX_S` | Stündlich | Neuer Produktlauf je Stunde |
| `MOSMIX_L` | Viermal täglich | 03, 09, 15 und 21 UTC |

Diese Angaben beschreiben die DWD-Produktzyklen, keinen internen Scheduler und keine garantierte Dateiverfügbarkeit exakt zur Laufzeit. Ausgabezeitpunkt, Abrufzeitpunkt und Vorhersagegültigkeit bleiben getrennt. Der tatsächliche Vorhersagehorizont kommt aus der Datei-Zeitachse; eine neue Dateiausgabe beweist nicht die Neuberechnung sämtlicher darin enthaltener Zeitschritte. Aktualitätsgrenzen und externe Abrufpläne müssen produktbezogen festgelegt werden.

Das CLI-Beispiel behält `--ParseMOSMIX` und verlangt zusätzlich `--Product MOSMIX_S|MOSMIX_L` zur eindeutigen Auswahl des Bibliotheksparsers. Fehlende oder unbekannte Produkte sind Fehler vor Quellenabruf. `--SelectMOSMIX` behält die bisherigen Geo-/Höhenparameter und weist die bekannte Verfügbarkeit/Elemente getrennt nach Produkt aus; unbekannte Verfügbarkeit bleibt gekennzeichnet. Ausgabemetadaten, Quellenkennung und konfigurierte MQTT-Topics müssen S und L unterscheiden, damit ihre retained-Nachrichten nicht unbeabsichtigt einander ersetzen.

Quelle, geprüft am 14.10.2026: [DWD-Verfahrensbeschreibung, Abschnitt 2.1.3 und Tabelle 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). Produktzyklen vor Implementierung/Release erneut prüfen; die Bibliothek enthält derzeit noch keinen implementierten Parser.

## Architektur und Tests

- C# und .NET ab Version 10 (`net10.0` als initiales TFM); SDK-Patch- und Paketversionen werden vor Implementierung fixiert.
- Jede Klasse mit ausführender Logik („Doing-Klasse“) implementiert ein passendes Interface. Abhängigkeiten werden über diese Interfaces per Konstruktor injiziert; Verdrahtung erfolgt zentral im Composition Root. Reine Daten-/Exportentitäten bleiben separat.
- Getrennte Verantwortlichkeiten für CLI, Konfiguration, Quellenabruf, Parsing, Stationsauswahl, Normalisierung, Serialisierung und MQTT.
- TDD nach Red → Green → Refactor, auch für Fehlerkorrekturen. MSTest, Moq und FluentAssertions; Testobjektaufbau in `[TestInitialize]`, `VerifyAll()` für verwendete Mocks in `[TestCleanup]`.
- Automatisierte Tests verwenden synthetische Fixtures und Mocks, keine verpflichtenden Live-Webseiten oder produktiven MQTT-Broker.
- Zeilen- und Branch-Coverage müssen jeweils mindestens 80 % erreichen. Beim erstmaligen Aufbau ohne messbare Zielbranch-Baseline gilt nur diese Mindestgrenze; der Zielbranch-Vergleich ist nicht anwendbar. Eine unerwartet fehlende, veraltete oder nicht vergleichbare Baseline im späteren Betrieb ist weiterhin ein Fehler. Reine Dokumentations-PRs dürfen je Kennzahl die gleiche Coverage wie der Zielbranch haben; Rückgänge sind nicht erlaubt. Für andere PRs muss jede Kennzahl unter 100 % Baseline strikt steigen; bei exakt 100 % Baseline werden exakt 100 % akzeptiert. Aktuelle Commit-IDs, vergleichbare Messbedingungen und ungerundete Zählerverhältnisse verwenden. Fehlende PR-Messungen, ungültige Zähler und Null-Nenner gelten nicht als Erfolg. Microsoft.CodeCoverage-Reports einschließlich der Codeblock-Aufschlüsselung sind Grundlage der Prüfung; Codeblock- und Branch-Coverage sind nicht gleichzusetzen. Messabbildung und Reportformat werden in P1 anhand echter Reports verifiziert.

Die Umsetzungsschritte, Abhängigkeiten und Phasenabnahmen stehen im [Entwicklungsplan](DEVELOPMENT_PLAN.md). Die erste Implementierungsphase ist im [Detailplan P1](PHASE_P1_PLAN.md) in Arbeitspakete und Testfälle zerlegt. Details und Abnahmekriterien stehen in [REQUIREMENTS.md](REQUIREMENTS.md). Regeln für die Mitarbeit stehen in [AGENTS.md](AGENTS.md). Restore-/Buildbefehle sind unten dokumentiert; Testmethoden sind noch nicht implementiert.

## Konfiguration und Veröffentlichung

Stationsauswahl, Quellenparameter, MQTT-Verbindung und Topics sollen konfigurierbar sein. Zugangsdaten gehören in Umgebungsvariablen beziehungsweise lokale, von Git ausgeschlossene Konfiguration. Das öffentliche Repository enthält ausschließlich Beispiele ohne Geheimnisse und ohne private Betriebsdaten.

Der separate Brokertransport des CLI-Beispiels verwendet eigene Zugangsdaten und gezielte Topic-Rechte. Pro Quelle sind eigene Messdaten- bzw. Vorhersagenachrichten vorgesehen; Mess- und Vorhersagedaten sowie beide Stationslisten erhalten getrennte Topics. Topic-Konfiguration und Detailverhalten sind in der Anforderungsliste beschrieben.

## Datenquellen

- [DWD Climate Data Center: 10-Minuten-Messdaten](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/)
- [DWD: Beschreibung der Temperatur-, Feuchte- und Luftdruckdaten](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/air_temperature/DESCRIPTION_obsgermany_climate_10min_air_temperature_en.pdf)
- [OsthessenWetter: Messwerte](https://osthessen-wetter.de/messwerte/)
- [DWD MOSMIX: Stationskatalog, Formate und Elementdefinitionen](https://www.dwd.de/DE/leistungen/opendata/hilfe.html)
- [DWD MOSMIX_L: Vorhersagen für Einzelstandorte](https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/)

Der Code steht unter der [MIT-Lizenz](LICENSE). Diese Lizenz erteilt keine Rechte an fremden Wetterdaten oder Webseiten. Quellenbedingungen, Attribution und zulässige Abrufhäufigkeit werden für jeden Adapter separat dokumentiert und vor produktivem Betrieb geprüft. Insbesondere wird eine frei verfügbare API von OsthessenWetter nicht vorausgesetzt.

## Verbindliche MOSMIX-Schemaquelle

[DWD pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) ist die verbindliche Grundlage für die MOSMIX-Quell-/XML-Datenstrukturen beider Parser. Typen, Namensräume, Elemente und Kardinalitäten werden daraus abgeleitet; ergänzende KML-Strukturen werden vor Implementierung geprüft. Die Schemafassung und ihre Prüfsumme sind für reproduzierbare Typenerzeugung bzw. Typableitung festzuhalten. Diese Quelltypen werden auf die separaten öffentlichen Ausgabeentitäten abgebildet; sie ersetzen nicht den normalisierten Forecast-Vertrag. Schema-/Mappingtests erfolgen testgetrieben. Es wurden noch keine C#-Datenstrukturen erzeugt.


## Ergänzende Entscheidungen vom 14.10.2026

- Entwicklungsumgebung: Visual Studio 2026. Lokal nachgewiesen: Community 18.8.3 und .NET SDK 10.0.303; dieser SDK-Stand ist die reproduzierbare Ausgangsbasis für die geplante `global.json` und CI, Ziel-Framework bleibt `net10.0`. Vor Implementierung Wartungsstand prüfen und ein erforderliches SDK-Update ausdrücklich dokumentieren. Visual Studio ist keine Laufzeitabhängigkeit unter Debian. [Microsoft SDK-/Visual-Studio-Zuordnung](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs).
- Sämtliche vom Programm erzeugten Meldungen sind Englisch: Hilfe, Validierungs-, Fehler-, Log- und Statusmeldungen. Datenfeldnamen bleiben sprachunabhängig; Originalnamen aus Quellen bleiben erhalten. Projektdokumentation bleibt Deutsch/Englisch.
- Beobachtungsdaten enthalten den Messzeitpunkt jeder ausgegebenen Messgröße in UTC ohne Zeitzonensuffix. Unterschiedliche Messzeitpunkte bleiben je Wert erhalten; zeitliche Zusammenführung, Auswahl und Bewertung übernimmt der Konsument. Geschätzte Quellzeiten bleiben entsprechend gekennzeichnet. Vorhersagen behalten ihre eigenen Ausgabe-/Gültigkeitszeiten.
- Erst nach Normalisierung und Umrechnung für die Ausgabe runden: Temperatur auf 0,1 °C, Druck auf 1 hPa bzw. 1 mmHg, relative Feuchtigkeit auf 1 Prozentpunkt und Windgeschwindigkeit auf 1 km/h. Beide Druckwerte aus demselben ungerundeten Ausgangswert berechnen; Rundungstoleranzen berücksichtigen. Exakte Mittelpunktwerte vom Nullpunkt weg runden (20,25 → 20,3; −20,25 → −20,3). Andere Größen erhalten keine neue Rundungsvorgabe.
- Für die öffentlichen XML-Exportentitäten werden reproduzierbar XSD-Dateien generiert. XML-Serializer-Vertragstests validieren Exporte gegen diese Schemas. Diese Ausgabeschemas sind von der DWD-MOSMIX-Quell-XSD getrennt; Generierung/Versionierung wird in P2 umgesetzt.
- Tests verwenden MSTest, Moq und FluentAssertions; Coverage wird mit Microsoft.CodeCoverage erfasst. Konkrete Versionen, Test-Runner-Integration und Plattformnachweise werden in P1 festgehalten.
- Coverage-Ausnahme: Jede Kennzahl mit exakt 100 % im Zielbranch darf im PR bei exakt 100 % bleiben. Für andere als reine Dokumentations-PRs bleibt unter 100 % der strikte Anstieg erforderlich; beim erstmaligen Start ohne Baseline entfällt der Vergleich. Jeweils mindestens 80 %. Angezeigte gerundete 100 % reichen nicht.
- Verbleibende CLI-Fragen werden bei der CLI-Entwicklung in P3 entschieden: Konfigurationsformat/-pfad, Umgebungsvariablen, Secret-Verwaltung, konkrete Exitcodes und CLI-Abbruch-/Timeoutgrenzen. Sie sind keine P0-/P1-Voraussetzung für die Bibliothek. Öffentliche Bibliotheks-Fehler-/Cancellation-Verträge bleiben davon getrennt; P1 darf für Testzwecke minimale Prozesskonventionen dokumentieren, ohne den endgültigen CLI-Vertrag vorwegzunehmen.

Diese Entscheidungen aktualisieren ausschließlich die Dokumentation; es wurden weder Anwendungscode noch XSD-Dateien generiert.


## P0-Präzisierungen vom 14.10.2026

- Abhängigkeiten: Bei der Aufnahme bzw. einem beauftragten Update die aktuellsten verfügbaren stabilen Versionen wählen, auf .NET-/Plattformkompatibilität und Lizenzbedingungen prüfen und die konkret aufgelösten Versionen reproduzierbar fixieren. Keine ungeprüften Floating-Versionen bei jedem Build; ein Konflikt wird dokumentiert statt unbemerkt eine ältere Version zu wählen.
- Coverage-Start: Mindestens 80 % je Kennzahl, ohne Zielbranch-Vergleich bei noch nicht vorhandener messbarer Baseline. Dokumentations-PRs dürfen Gleichstand haben. Die bestehende exakte 100-%-Ausnahme bleibt gültig. Reports von Microsoft.CodeCoverage mit Codeblock-Aufschlüsselung werden zur Prüfung verwendet.
- Rundung: Auf den nächstgelegenen vereinbarten Schritt runden; bei genau halben Schritten vom Nullpunkt weg (`MidpointRounding.AwayFromZero`), beispielsweise 20,25 → 20,3 und −20,25 → −20,3. Nach Umrechnung runden, nicht vorher.
- Quellen: Die konkreten DWD- und OsthessenWetter-Endpunkte liefert der Konsument über die Konstruktorparameter des jeweiligen Parsers. Quellenpfade werden weder fest eingebaut noch als offene globale P0-Konfigurationsentscheidung behandelt. Konkrete Quellenformate und Parameterabbildungen sind bei der jeweiligen Adapterimplementierung zu dokumentieren.
- OsthessenWetter: Ist die konfigurierte Seite nicht abrufbar, löst der Parser eine Exception mit nachvollziehbarer englischer Diagnose aus; keine Ersatzdaten oder vorgetäuschten Erfolge. Eine abrufbare Seite wird in der dokumentierten As-is-Struktur ausgelesen, einschließlich der dort angegebenen Messzeitpunkte. Keine zusätzliche Aktualitätsbewertung oder künstliche Korrektur der Quelldaten; der Konsument bewertet die Messzeitpunkte. Gemeinsame UTC-/Einheiten-/Rundungsregeln bleiben gültig. Bei nur einer Uhrzeit bleibt die ausdrücklich erlaubte Datumsableitung nach REQ-042 als geschätzt gekennzeichnet; ein vollständiger Quellzeitpunkt wird nicht ersetzt. CLI-Fehlerdarstellung und Exitcodes werden in P3 festgelegt.

Diese Festlegungen sind Dokumentationsentscheidungen; sie erzeugen noch keinen Anwendungscode.


## Verbindlicher Phasenstatus und Ablauf

**15.10.2026: P0 abgeschlossen; P1-Solution-/Projektstruktur erstellt. P1 ist noch nicht abgenommen.**

Reihenfolge bei unveränderten Phasen-IDs: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Erst die vollständige Bibliothek einschließlich Stationsauswahl, DWD-/OsthessenWetter-/MOSMIX_S-/MOSMIX_L-Parsern, Ausgabeentitäten, JSON/XML, XSDs, MQTT-Serializer und DI fertigstellen und abnehmen. Danach P3 mit CLI-Nutzungsbeispiel, Konfiguration und Beispiel-Brokertransport beginnen. Öffentliches Release bleibt P9.

P1 enthält nur einen technischen Paketkonsumenten als Integrationstest, kein CLI-/Anwendungsbeispiel. Vor P3 Quellenfunktionen direkt über die Bibliothek und ihre Tests abnehmen; kein CLI-Aufruf ist Voraussetzung für die Bibliotheksphasen. Die Phasenreihenfolge ersetzt frühere numerische Reihenfolge oder Abhängigkeiten auf P3. [Detailplan P1](PHASE_P1_PLAN.md). Der aktuelle Auftrag ergänzt die Projektstruktur; Fachimplementierung bleibt vertagt.


## Projektstruktur und lokaler Build

Stand: 15.10.2026. Die [WeatherParser.slnx](WeatherParser.slnx) enthält zwei Projekte:

```text
WeatherParser.slnx
src/
  WeatherParser.Parsing/           # Packable net10.0 library
tests/
  WeatherParser.Parsing.Tests/     # MSTest project with source reference
  WeatherParser.Package.Tests/    # Reserved for P1.04; no project yet
```

SDK 10.0.303 wird durch `global.json` fixiert. Visual Studio 2026 kann die `.slnx` direkt öffnen. `Directory.Build.props` setzt `net10.0`, Nullable, implizite Usings und deterministische Builds. `Directory.Packages.props` fixiert die geprüften stabilen Testpakete; siehe [Abhängigkeiten und Lizenzen](DEPENDENCIES.md). NuGet verwendet ausschließlich die öffentliche Quelle aus `NuGet.Config`.

Aus dem Repository-Verzeichnis:

```powershell
dotnet restore WeatherParser.slnx --locked-mode
dotnet build WeatherParser.slnx --configuration Release --no-restore
```

Lockdateien werden mitgeführt. Bei beauftragten Paketupdates einmal ohne `--locked-mode` neu auflösen und Änderungen prüfen. Buildausgaben, Testberichte und lokale Secrets sind von Git ausgeschlossen.

Die Projekte enthalten bewusst noch keine Parser, öffentlichen Fachtypen oder Testmethoden. Damit ist nur die Struktur angelegt: kein TDD-, Test- oder Coverage-Erfolg und keine vollständige P1-Abnahme. Das Paketkonsumentenprojekt wird mit lokalem Pack/Restore in P1.04 ergänzt; es erhält keine Quellreferenz. Das CLI-Beispiel entsteht erst nach der fertigen Bibliothek in P3.

Lokale Prüfung am 15.10.2026: Solution-Liste und Restore erfolgreich; Release-Build mit 0 Warnungen und 0 Fehlern. Beide Projekte besitzen Lockdateien. Der leere Strukturstand ist kein Test-/Coverage-Nachweis.


P1.01-Eingangsprüfung am 15.10.2026 abgeschlossen: [Protokoll und Ausgangsinventur](docs/p1/P1_01_BASELINE.md). Vorhandene Projektstruktur erhalten; kein neuer Fachcode und kein Test-/Coverage-Ergebnis behauptet. P1.02-Prüfung inzwischen abgeschlossen; aktueller Folgeschritt unten dokumentiert.


**P1.02-Prüfung (15.10.2026) abgeschlossen:** [Werkzeug-/Lizenz-/Kompatibilitätsbericht](docs/p1/P1_02_TOOLING_REVIEW.md). Alle 16 Testpakete geprüft; sechs direkte Versionen aktuell, keine gemeldeten NuGet-Schwachstellen/Abkündigungen. SDK 10.0.303 nicht mehr gewartet; vor P1.03-Abnahme auf ausgewähltes 10.0.402 aktualisieren und prüfen. Kein SDK-/Paketwechsel und keine neuen Tests in dieser Prüfung.
