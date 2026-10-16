# Regeln für WeatherParser

[Deutsch](AGENTS.md) | [English](AGENTS.en.md)

**Phasenstatus (15.10.2026): P0 abgeschlossen; P1 mit Solution-/Projektstruktur begonnen. Fachimplementierung, Tests und Coverage-Abnahme stehen aus.**

Diese Regeln gelten für das gesamte Repository. Aktuelle ausdrückliche Nutzeranweisungen haben Vorrang. Verbindliche fachliche Anforderungen stehen in [REQUIREMENTS.md](REQUIREMENTS.md); Projektstatus und Nutzung in [README.md](README.md).

## Sprachregeln

- Alle Codekommentare auf Englisch verfassen: Inline-/Blockkommentare, XML-Dokumentationskommentare und Kommentare in Skripten sowie Konfigurationsbeispielen. Alle vom Programm erzeugten Laufzeitmeldungen sind Englisch.
- Dokumentation vollständig auf Deutsch und Englisch pflegen. Die Paare `README.md` / `README.en.md`, `REQUIREMENTS.md` / `REQUIREMENTS.en.md` sowie `AGENTS.md` / `AGENTS.en.md` und `DEVELOPMENT_PLAN.md` / `DEVELOPMENT_PLAN.en.md` müssen inhaltlich gleichwertig und gegenseitig verlinkt sein.
- Jede inhaltliche Änderung in beiden Sprachfassungen im selben Arbeitsstand durchführen. Identische Anforderungs-IDs, Befehle, Datenfelder, Einheiten und Beispiele erhalten. Neue Dokumente ebenfalls zweisprachig anlegen.
- Die englische MIT-Lizenz in `LICENSE` unverändert als rechtlichen Lizenztext erhalten. Sprachgleichheit und englische Kommentare bei Änderungsprüfungen berücksichtigen.

## Aktueller Auftrag und Umfang

- Das Projekt ist ein lokales Git-Repository mit Dokumentation und erster Bibliotheks-/Testprojektstruktur. Anwendungscode, .NET-Projekte und Implementierungsgerüste erst auf ausdrücklichen Auftrag erzeugen.
- Hauptprodukt ist eine wiederverwendbare NuGet-Bibliothek; die CLI ist nur ein Paket-Nutzungsbeispiel mit `--SelectDWD`, `--ParseDWD`, `--ParseOstHessen`, `--SelectMOSMIX` und `--ParseMOSMIX`, MQTT-Ausgabe sowie JSON- und XML-Export.
- Wetter-Messdaten und Vorhersagen strikt unterscheiden. Aktuell sind Messdaten, MOSMIX-Vorhersagen und die zugehörige Stationsauswahl als Anforderungen beauftragt; keine Türsteuerung, n8n-Workflows oder Home-Assistant-Installation ergänzen.
- Offene Entscheidungen aus REQUIREMENTS nicht unbemerkt zu bestätigten Anforderungen erklären. Änderungen am CLI-/MQTT-Vertrag dokumentieren.

## Zielplattform und Workflow-Rolle

- WeatherParser ist ein eigenständiger Baustein im Gesamtworkflow, zuständig für Quellenabruf, Parsing, Stationsauswahl bzw. Aufbereitung und Ausgabe. Speicherung, Anzeige, Gerätesteuerung und weitere Workflow-Aktionen gehören nicht in diesen Baustein.
- Verbindliche Plattformvorgabe (14.10.2026): .NET ab Version 10 und Debian ab Version 13 Trixie. Initiales Ziel-Framework für Bibliothek und CLI-Beispiel ist `net10.0`; Mindestarchitektur ist ARM64 (`linux-arm64`), insbesondere NanoPi R76S. Neuere stabile .NET-/Debian-Versionen werden in die geprüfte Kompatibilitätsmatrix aufgenommen; Preview-Versionen sind nicht automatisch zugesichert. Visual Studio 2026 und SDK 10.0.303 sind als lokale Ausgangsbasis festgestellt; Paketversionen, weitere ARM-Varianten und Deployment-Modell bleiben festzulegen. Bibliothek, echter Paketkonsument und Beispiel müssen auf Debian 13/ARM64 mit .NET 10 geprüft werden. Plattformanforderung und tatsächlich nachgewiesene Kompatibilität bleiben getrennt; ohne dokumentierten Test kein behaupteter Laufzeiterfolg.
- Pro Prozessaufruf genau einen CLI-Befehl ausführen und danach mit auswertbarem Exitstatus beenden. Kein eigener Scheduler, kein Dauerpolling und keine periodischen Hintergrundtimer. Mehrere Ergebnisse innerhalb eines Befehls sind erlaubt; technische Wiederholungen bleiben begrenzt.
- Zeitplanung, laufübergreifende Wiederholungen und Workflow-Orchestrierung extern belassen, etwa bei n8n oder cron. Nichtinteraktiven Aufruf, saubere Ausgabekanäle und kontrollierte Abbruchbehandlung gewährleisten. Keine feste n8n-Abhängigkeit einführen und keinen Orchestrator installieren.
- Build-/Publish-Erfolg und tatsächlichen Debian-/ARM-Laufzeitnachweis unterscheiden. Zielplattformprüfung samt Serializer- und Ein-Befehl-Verhalten vor Freigabe dokumentieren; fehlende Hardwaretests ausdrücklich offen lassen.

## Open Source und Git

- Der Code wird unter MIT veröffentlicht. Die Datei `LICENSE` erhalten; fremden Code und Abhängigkeiten nur mit geklärter, passender Lizenz aufnehmen.
- Öffentliches Repository: keine Passwörter, Tokens, privaten Schlüssel, Broker-Zugangsdaten, privaten Betriebslogs, privaten Konfigurationsdateien oder personenbezogenen Testdaten committen.
- Beispielkonfigurationen ausschließlich mit Platzhaltern; Secret-Dateien vor ihrer Erzeugung in `.gitignore` aufnehmen. Secrets weder loggen noch in Test-Snapshots ablegen.
- Quellenbedingungen und notwendige Attribution separat dokumentieren. MIT für den Code bedeutet keine automatische Lizenz für Wetterdaten, Webseiten oder Bilder.
- GitHub-Repository, Remote, Push, Release und Veröffentlichung nur im ausdrücklich beauftragten Umfang einrichten. Keine automatische Veröffentlichung aufgrund des allgemeinen Open-Source-Ziels.
- Vor Änderungen Arbeitsstand prüfen, fremde Änderungen erhalten, keine destruktiven Resets oder Bereinigungen. Aussagekräftige kleine Commits, sofern beauftragt; keinen Commit als durchgeführt melden, wenn nur Dateien angelegt wurden.

## NuGet-Hauptprodukt und Beispiel-CLI

Das zur Veröffentlichung bestimmte Hauptprodukt ist eine wiederverwendbare NuGet-Bibliothek. Sie enthält DWD-/OsthessenWetter-/MOSMIX-Parser einschließlich benötigtem Quellenabruf und Normalisierung, die öffentlichen Ausgabeentitäten, einen generischen MQTT-Serializer sowie die Dependency-Injection-Integration. Die CLI ist ausschließlich ein Nutzungsbeispiel und konsumiert eine versionierte Paketreferenz; andere Anwendungen können das Paket ohne CLI verwenden.

Jeder Parser erhält seine benötigten Quell-URLs ausdrücklich über Konstruktorparameter, vorzugsweise typisiert als `Uri`. Mehrere Endpunkte werden getrennt benannt. Die DI-Integration übernimmt die vom Konsumenten gelieferten Endpunkte und übergibt sie an die jeweiligen Konstruktoren; sie erfindet keine fest eingebauten Quellen oder globalen URI-Singletons. HTTP, Zeit und weitere Services bleiben ebenfalls über Konstruktoren injizierbar. Die Bibliothek liest keine CLI-Argumente und keine anwendungsspezifischen Konfigurationsdateien oder Umgebungsvariablen selbstständig ein.

Der MQTT-Serializer ist quellenunabhängig: Er serialisiert geeignete Ausgabeentitäten und erzeugt eine Nachricht mit Topic, JSON-Payload, QoS und Retain-Einstellung. Topics bzw. Topic-Vorlagen und deren Zuordnung zu Nachrichtentypen stammen aus der Konfiguration des Konsumenten, nicht aus festen Wetterpfaden im Paket. Aufgelöste Publish-Topics müssen gültig sein; fehlende Zuordnungen und unaufgelöste Platzhalter erzeugen definierte Fehler. Die bisherigen `haus/weather/...`-Topics sind nur konfigurierbare Beispiele. Serializer und tatsächlicher Brokertransport sind getrennt; die Serialisierung verlangt keine Brokerverbindung und benötigt keine Zugangsdaten.

Die Bibliothek stellt eine dokumentierte DI-Registrierung für ihre Parser, Serializer und benötigten Services bereit. Der Konsument stellt Endpunkte, Topic-Konfiguration und externe Abhängigkeiten bereit. Jede Doing-Klasse besitzt ein Interface. Ausgabeentitäten bleiben eigene `[Serializable]`-Typen. Testbarkeit, UTC-Vertrag und JSON-/XML-Kompatibilität bleiben erhalten.

Das Beispiel-CLI übernimmt Argumente, Anwendungskonfiguration, Ein-Befehl-Prozessablauf, Exitcodes, Datei-/Konsolenexport und gegebenenfalls MQTT-Versand. Der Paketaufruf ist sein einziger Weg zur Parserlogik; keine Parserkopien. Öffentliche Paket-API, Paketversionierung und unabhängiger Paketkonsum stehen im Mittelpunkt der Abnahme. Paket-ID, Feed und Veröffentlichungsvorgang sind vor Veröffentlichung zu entscheiden; derzeit existiert kein gebautes oder veröffentlichtes Paket.

## Architektur

- Jede Klasse mit ausführender Logik („Doing-Klasse“) muss ein passendes Interface implementieren; Abhängigkeiten auf diese Klassen als Interfaces injizieren. Reine Daten-/Exportentitäten benötigen ohne ausführende Logik kein künstliches Interface.
- Alle Service-Abhängigkeiten über Konstruktorinjektion bereitstellen. Fachklassen dürfen konkrete Services weder selbst erzeugen noch aus einem `IServiceProvider` nachladen.
- Service-Registrierungen und Verdrahtung im zentralen Composition Root bündeln. Framework-Builder, unveränderliche Datenobjekte und Werte sind keine versteckten Service-Abhängigkeiten.
- HTTP, MQTT, Dateizugriff und Zeitversorgung hinter injizierbaren Grenzen halten. Parser und Normalisierung müssen mit lokalen Eingaben ohne Netzwerk testbar sein.
- CLI, Konfiguration, Quellenadapter, Stationsauswahl, Datenvertrag, JSON-/XML-Serialisierung, Exportziele und MQTT-Veröffentlichung getrennt halten. Keine allgemeinen statischen Service-Singletons oder versteckten globalen Zustände.
- Asynchrone Ein-/Ausgabe, CancellationToken und begrenzte Timeouts verwenden. Fehler nicht durch erfundene Werte oder stillschweigendes Ignorieren verdecken.
- .NET- und Paketversionen vor Implementierung anhand offizieller Quellen auf Kompatibilität, Wartung und Lizenz prüfen. Reproduzierbare Versionierung vorsehen; keine unnötigen Frameworks einführen.

## Datenvertrag

- Jeder ausgegebene Zeitstempel ist UTC im Format `YYYY-MM-DDTHH:mm:ss`, ohne `Z` oder Offset. UTC intern eindeutig halten; nie lokale Systemzeit ungeprüft serialisieren.
- DWD-Station aus CLI oder Konfiguration auflösen. Ohne beides: Fehler loggen, Fehlerstatus, kein HTTP und kein MQTT. Niemals automatisch die nächstgelegene Station als Ersatz verwenden.
- Koordinaten sowohl als Dezimalgrad als auch DMS mit Himmelsrichtung verarbeiten; kulturunabhängig und mit Bereichsprüfung.
- MOSMIX-Auswahl mit denselben Geo-/Höhenparametern, aber eigenem Katalog durchführen. MOSMIX-IDs als Zeichenketten erhalten; keine Gleichsetzung mit CDC-Messstations-IDs. `ParseMOSMIX` verlangt `MOSMIXID`.
- MOSMIX-Vorhersagen separat normalisieren: Quell-Ausgabezeitpunkt, Abruf, Gültigkeitszeiten und Intervalle erhalten; niemals als Messdaten oder mit `measured_at` ausgeben. Eigene Forecast-Topics und vollständigen retained-Lauf verwenden.
- MOSMIX_S und MOSMIX_L durch getrennte Parser/Interfaces mit jeweils eigenen Konstruktor-URLs und DI-Registrierungen bereitstellen; Elementdefinitionen und Aktualitätsgrenzen je Produkt dokumentieren. KML/KMZ mit Namensräumen und korrekter Parameter-/Zeitachsen-Zuordnung lesen; fehlende Werte nicht zeitlich verschieben. XML ohne externe Entitäten/DTD verarbeiten und Archivgrößen begrenzen.
- Fehlende bzw. ungültige Messwerte weglassen; `0` ist ein echter Wert. DWD-Fehlwertkennungen nicht als Messungen veröffentlichen.
- Druckeinheiten konsistent aus demselben Ausgangswert umrechnen, Druckreferenz erhalten. Keine pauschale Umrechnung zwischen Lux und W/m².
- Regenmenge, Regenintensität, Spitzenböe, Mittelwind sowie Strahlungsenergie/-leistung fachlich unterscheiden. Umrechnungen nur mit belegtem Zeitraum.
- Messungen unterschiedlicher Zeitpunkte nicht irreführend zusammenführen. Messzeiträume und Qualitätsinformationen gemäß REQUIREMENTS erhalten.
- Für OsthessenWetter ist das Ableiten des Datums erlaubt, muss aber ausdrücklich als `estimated_date` gekennzeichnet werden. Quellzeitzone `Europe/Berlin` berücksichtigen, nach UTC konvertieren; Mitternacht und Sommerzeitwechsel testen. Keine erfundene Sicherheit bei nicht eindeutig auflösbaren Zeiten.

- Jeden fachlichen Exportobjekttyp als eigene stark typisierte Entität definieren und sämtliche selbst definierten Exportentitäten, auch verschachtelte, ausdrücklich mit `[Serializable]` markieren. Typisierte Entitäten statt anonymer Objekte, `dynamic` oder untypisierter Export-Dictionaries verwenden.
- JSON mit `System.Text.Json.JsonSerializer`, XML mit `System.Xml.Serialization.XmlSerializer` erzeugen. Dieselben Exportentitäten für beide Formate und MQTT-JSON verwenden; keine manuelle Dokumentverkettung. Die Entitäten müssen mit beiden Serializern kompatibel sein und dürfen keine Dienste oder Secrets enthalten.
- Serializer hinter injizierbaren Diensten bzw. zentral verdrahteten Factories halten. `[Serializable]` ersetzt keine JSON-/XML-Feld-, Nullwert- oder Zeitformatregeln.
- JSON und XML aus demselben normalisierten Datenmodell erzeugen. UTC, fehlende Werte, echte Nullwerte, Quellen- und Qualitätsinformationen in beiden Formaten erhalten; XML-Inhalte korrekt escapen.
- Im JSON-/XML-Modus keinen MQTT-Broker verlangen oder kontaktieren. Ein vollständiges Dokument je Aufruf, Logs auf Standardfehler; Dateiexporte ohne stillschweigendes Überschreiben oder irreführend vollständige Teilergebnisse.

### Getrennte MOSMIX-Parser und Aktualisierung

Die Bibliothek bietet zwei eigenständige Parser mit eigenen Interfaces: `MosmixSParser` / `IMosmixSParser` für `MOSMIX_S` und `MosmixLParser` / `IMosmixLParser` für `MOSMIX_L`. Beide erhalten ihre jeweiligen Quell-URLs über Konstruktorparameter und werden separat über die DI-Integration konfiguriert. Gemeinsame KML-/KMZ-Hilfslogik darf wiederverwendet werden; ein einzelner umschaltbarer Parser ersetzt die beiden öffentlichen Verträge nicht. Bei fehlenden Daten gibt es keinen stillschweigenden Wechsel zum anderen Produkt.

| Produkt | DWD-Ausgabehäufigkeit | Laufzeitangaben |
|---|---|---|
| `MOSMIX_S` | Stündlich | Neuer Produktlauf je Stunde |
| `MOSMIX_L` | Viermal täglich | 03, 09, 15 und 21 UTC |

Diese Angaben beschreiben die DWD-Produktzyklen, keinen internen Scheduler und keine garantierte Dateiverfügbarkeit exakt zur Laufzeit. Ausgabezeitpunkt, Abrufzeitpunkt und Vorhersagegültigkeit bleiben getrennt. Der tatsächliche Vorhersagehorizont kommt aus der Datei-Zeitachse; eine neue Dateiausgabe beweist nicht die Neuberechnung sämtlicher darin enthaltener Zeitschritte. Aktualitätsgrenzen und externe Abrufpläne müssen produktbezogen festgelegt werden.

Das CLI-Beispiel behält `--ParseMOSMIX` und verlangt zusätzlich `--Product MOSMIX_S|MOSMIX_L` zur eindeutigen Auswahl des Bibliotheksparsers. Fehlende oder unbekannte Produkte sind Fehler vor Quellenabruf. `--SelectMOSMIX` behält die bisherigen Geo-/Höhenparameter und weist die bekannte Verfügbarkeit/Elemente getrennt nach Produkt aus; unbekannte Verfügbarkeit bleibt gekennzeichnet. Ausgabemetadaten, Quellenkennung und konfigurierte MQTT-Topics müssen S und L unterscheiden, damit ihre retained-Nachrichten nicht unbeabsichtigt einander ersetzen.

Quelle, geprüft am 14.10.2026: [DWD-Verfahrensbeschreibung, Abschnitt 2.1.3 und Tabelle 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). Produktzyklen vor Implementierung/Release erneut prüfen; die Bibliothek enthält derzeit noch keinen implementierten Parser.

## Tests

- Testgetrieben entwickeln: Red → Green → Refactor. Zuerst den erwarteten Fehler des neuen Tests nachweisen, dann minimal implementieren und anschließend unter grünen Tests refaktorieren. Fehlerkorrekturen beginnen mit einem reproduzierenden Regressionstest.
- MSTest verwenden; Mocking mit Moq, Assertions mit FluentAssertions.
- Testobjekt und gemeinsame Abhängigkeiten in `[TestInitialize]` aufbauen. Mocks vorzugsweise in einer `MockRepository` verwalten; erwartete Interaktionen explizit konfigurieren.
- In `[TestCleanup]` `VerifyAll()` für alle verwendeten Mocks ausführen. Test-spezifische Eingaben und Erwartungen dürfen im Test ergänzt werden.
- Aussagekräftige Verhaltens-, Fehler- und Grenzfalltests aus REQUIREMENTS pflegen. Keine Tests, die nur die Implementierung abschreiben oder ausschließlich triviale Getter prüfen.
- MOSMIX-Katalogfilter, ID-Systeme, KML/KMZ, Einheiten, Fehlwerte, Vorhersagezeitachsen, Forecast-Topics und Exportgleichheit explizit testen.
- `[Serializable]` aller Exportentitäten und vollständige Objektgraphen mit den echten JSON-/XML-Serializern testen; fachliche Gleichheit, UTC, fehlende Werte und Sonderzeichen prüfen. In Serializer-Vertragstests den Serializer nicht vollständig mocken.
- Zeilen- und Branch-Coverage müssen jeweils mindestens 80 % erreichen. Beim erstmaligen Aufbau ohne messbare Zielbranch-Baseline gilt nur diese Mindestgrenze; der Zielbranch-Vergleich ist nicht anwendbar. Eine unerwartet fehlende, veraltete oder nicht vergleichbare Baseline im späteren Betrieb ist weiterhin ein Fehler. Reine Dokumentations-PRs dürfen je Kennzahl die gleiche Coverage wie der Zielbranch haben; Rückgänge sind nicht erlaubt. Für andere PRs muss jede Kennzahl unter 100 % Baseline strikt steigen; bei exakt 100 % Baseline werden exakt 100 % akzeptiert. Aktuelle Commit-IDs, vergleichbare Messbedingungen und ungerundete Zählerverhältnisse verwenden. Fehlende PR-Messungen, ungültige Zähler und Null-Nenner gelten nicht als Erfolg. Microsoft.CodeCoverage-Reports einschließlich der Codeblock-Aufschlüsselung sind Grundlage der Prüfung; Codeblock- und Branch-Coverage sind nicht gleichzusetzen. Messabbildung und Reportformat werden in P1 anhand echter Reports verifiziert.
- Standardtests ohne Internet und ohne produktiven MQTT-Broker ausführen. HTTP, Zeit und MQTT mocken; synthetische Fixtures bevorzugen.
- Integrationstests getrennt und ausdrücklich aktivierbar halten. Keine produktiven MQTT-Nachrichten oder Änderungen an Zielsystemen als Nebeneffekt normaler Tests.
- Build und passende Tests nach Implementierungsänderungen ausführen. Nicht durchgeführte oder fehlgeschlagene Prüfungen offen nennen; keinen Hardware-/Live-Erfolg aus Mock-Tests ableiten.

## Quellenabruf und Protokollierung

- DWD-Formate und Einheiten anhand offizieller Dokumentation prüfen; OsthessenWetter anhand der tatsächlichen veröffentlichten Struktur. Unbekannte Felder oder Zeitbezüge nicht erraten.
- Kein Umgehen von Zugriffsschutz, keine ungebremsten Abrufe. Abrufhäufigkeit, Wiederholungen und Fehlermeldungen nachvollziehbar dokumentieren.
- Fehler mit stabilen Kennungen, UTC, Quelle/Station und Befehl protokollieren. Keine Secrets und keine ungefilterten Konfigurationsobjekte ausgeben.
- Logs auf Standardfehler, JSON-/XML-Standardausgabe ausschließlich für das Exportdokument. Erfolg im MQTT-Modus erst nach bestätigter Veröffentlichung, im Exportmodus erst nach vollständig erfolgreicher Ausgabe melden.
- MQTT-Sammlung von Fehlermeldungen ist eine Zukunftsnotiz, keine aktuell implementierte Funktion. Brokerprobleme müssen unabhängig davon lokal sichtbar bleiben.

## Dokumentation

- Den zweisprachigen Entwicklungsplan in `DEVELOPMENT_PLAN.md` / `DEVELOPMENT_PLAN.en.md` mit Anforderungen und tatsächlichem Fortschritt abgleichen. Phasen bleiben geplant, bis ihre Abnahmen nachgewiesen sind; offene Coverage-Start-/Grenzfallentscheidungen nicht eigenmächtig umgehen.

- README, REQUIREMENTS und tatsächlichen Stand konsistent halten. Geplante Befehle ausdrücklich als geplant kennzeichnen, bis sie implementiert und geprüft sind.
- Öffentliche Beispiele portabel und ohne private Pfade, Netzadressen oder Secrets gestalten.
- Geänderte Annahmen, Konfigurationsvorrang, Datenfelder, Topics, Einheiten und bekannte Quellenbeschränkungen dokumentieren.
- Keine zusätzlichen Funktionen allein deshalb implementieren, weil sie als spätere Erweiterung erwähnt sind.

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
