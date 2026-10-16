# WeatherParser – Entwicklungsplan

[Deutsch](DEVELOPMENT_PLAN.md) | [English](DEVELOPMENT_PLAN.en.md)

**Phasenstatus (16.10.2026): P0 abgeschlossen; P1.01-P1.04 abgeschlossen (Visual-Studio-Build-Einschränkung behoben; Reproduzierbarkeit auf sauberem Checkout offen); übrige P1-Abnahmen stehen aus.**

Stand: 14.10.2026. Grundlage: [REQUIREMENTS.md](REQUIREMENTS.md) und [AGENTS.md](AGENTS.md). P0 ist abgeschlossen; P1 hat mit der Projektstruktur begonnen. Es existieren Dokumentation, Solution sowie Bibliotheks-/Testprojektstruktur; noch keine Fachimplementierung. Dieser Plan ist keine Freigabe zum Erzeugen von Anwendungscode, zur GitHub-Veröffentlichung oder zur Installation auf R76S.

## Ziel und Ergebnis

WeatherParser wird eine eigenständig nutzbare C#-NuGet-Bibliothek als Hauptprodukt für Debian/ARM64. Die Beispiel-CLI konsumiert dieses Paket. Ein Aufruf führt genau einen der fünf Befehle aus und beendet sich nach erfolgreicher Ausgabe oder einem nachvollziehbaren Fehler. n8n, cron oder andere Aufrufer übernehmen Zeitplanung und Workflow-Verkettung.

Ergebnisumfang:

- DWD-Messstationsauswahl und aktuelle Messdaten.
- OsthessenWetter-Stationsauswahl per Muster und Messdaten mit gekennzeichneter Datumsableitung.
- MOSMIX-Standortauswahl und getrennte Wettervorhersagen.
- MQTT-JSON oder eigenständiger JSON-/XML-Export.
- Eigene `[Serializable]`-Exportentitäten, Interfaces für alle Doing-Klassen und Konstruktor-DI.
- TDD mit MSTest, Moq und FluentAssertions, englische Codekommentare und vollständige deutsche/englische Dokumentation.

## NuGet-Hauptprodukt und Beispiel-CLI

Das zur Veröffentlichung bestimmte Hauptprodukt ist eine wiederverwendbare NuGet-Bibliothek. Sie enthält DWD-/OsthessenWetter-/MOSMIX-Parser einschließlich benötigtem Quellenabruf und Normalisierung, die öffentlichen Ausgabeentitäten, einen generischen MQTT-Serializer sowie die Dependency-Injection-Integration. Die CLI ist ausschließlich ein Nutzungsbeispiel und konsumiert eine versionierte Paketreferenz; andere Anwendungen können das Paket ohne CLI verwenden.

Jeder Parser erhält seine benötigten Quell-URLs ausdrücklich über Konstruktorparameter, vorzugsweise typisiert als `Uri`. Mehrere Endpunkte werden getrennt benannt. Die DI-Integration übernimmt die vom Konsumenten gelieferten Endpunkte und übergibt sie an die jeweiligen Konstruktoren; sie erfindet keine fest eingebauten Quellen oder globalen URI-Singletons. HTTP, Zeit und weitere Services bleiben ebenfalls über Konstruktoren injizierbar. Die Bibliothek liest keine CLI-Argumente und keine anwendungsspezifischen Konfigurationsdateien oder Umgebungsvariablen selbstständig ein.

Der MQTT-Serializer ist quellenunabhängig: Er serialisiert geeignete Ausgabeentitäten und erzeugt eine Nachricht mit Topic, JSON-Payload, QoS und Retain-Einstellung. Topics bzw. Topic-Vorlagen und deren Zuordnung zu Nachrichtentypen stammen aus der Konfiguration des Konsumenten, nicht aus festen Wetterpfaden im Paket. Aufgelöste Publish-Topics müssen gültig sein; fehlende Zuordnungen und unaufgelöste Platzhalter erzeugen definierte Fehler. Die bisherigen `haus/weather/...`-Topics sind nur konfigurierbare Beispiele. Serializer und tatsächlicher Brokertransport sind getrennt; die Serialisierung verlangt keine Brokerverbindung und benötigt keine Zugangsdaten.

Die Bibliothek stellt eine dokumentierte DI-Registrierung für ihre Parser, Serializer und benötigten Services bereit. Der Konsument stellt Endpunkte, Topic-Konfiguration und externe Abhängigkeiten bereit. Jede Doing-Klasse besitzt ein Interface. Ausgabeentitäten bleiben eigene `[Serializable]`-Typen. Testbarkeit, UTC-Vertrag und JSON-/XML-Kompatibilität bleiben erhalten.

Das Beispiel-CLI übernimmt Argumente, Anwendungskonfiguration, Ein-Befehl-Prozessablauf, Exitcodes, Datei-/Konsolenexport und gegebenenfalls MQTT-Versand. Der Paketaufruf ist sein einziger Weg zur Parserlogik; keine Parserkopien. Öffentliche Paket-API, Paketversionierung und unabhängiger Paketkonsum stehen im Mittelpunkt der Abnahme. Paket-ID, Feed und Veröffentlichungsvorgang sind vor Veröffentlichung zu entscheiden; derzeit existiert kein gebautes oder veröffentlichtes Paket.

## Vorgehen und Freigaberegeln

Jede funktionale Änderung wird als kleiner, prüfbarer Schritt nach Red → Green → Refactor bearbeitet: Test schreiben, erwarteten Fehlschlag nachweisen, minimal implementieren, anschließend unter grünen Tests refaktorieren. Fehlerkorrekturen beginnen mit einem Regressionstest. Testaufbau erfolgt in `[TestInitialize]`, Mock-Prüfung mit `VerifyAll()` in `[TestCleanup]`.

Jeder Schritt aktualisiert beide Sprachfassungen und seine Anforderungszuordnung. Messdaten, Vorhersagen und unterschiedliche ID-Systeme bleiben getrennt. Mocks prüfen Service-Interaktionen; echte Serializer-Vertragstests prüfen tatsächliche JSON-/XML-Ausgaben. Offline-Tests und Live-/ARM-Abnahmen werden separat ausgewiesen.

Zeilen- und Branch-Coverage müssen jeweils mindestens 80 % erreichen. Beim erstmaligen Aufbau ohne messbare Zielbranch-Baseline gilt nur diese Mindestgrenze; der Zielbranch-Vergleich ist nicht anwendbar. Eine unerwartet fehlende, veraltete oder nicht vergleichbare Baseline im späteren Betrieb ist weiterhin ein Fehler. Reine Dokumentations-PRs dürfen je Kennzahl die gleiche Coverage wie der Zielbranch haben; Rückgänge sind nicht erlaubt. Für andere PRs muss jede Kennzahl unter 100 % Baseline strikt steigen; bei exakt 100 % Baseline werden exakt 100 % akzeptiert. Aktuelle Commit-IDs, vergleichbare Messbedingungen und ungerundete Zählerverhältnisse verwenden. Fehlende PR-Messungen, ungültige Zähler und Null-Nenner gelten nicht als Erfolg. Microsoft.CodeCoverage-Reports einschließlich der Codeblock-Aufschlüsselung sind Grundlage der Prüfung; Codeblock- und Branch-Coverage sind nicht gleichzusetzen. Messabbildung und Reportformat werden in P1 anhand echter Reports verifiziert.

**Coverage-Grenzfälle sind entschieden:** Beim Start mindestens 80 % je Kennzahl ohne Zielbranch-Vergleich. Reine Dokumentations-PRs dürfen Gleichstand haben. Die exakte 100-%-Ausnahme bleibt gültig. Keine Baseline erfinden; fehlende PR-Messungen bestehen weiterhin nicht.

## Plattformvorgabe

Verbindliche Plattformvorgabe (14.10.2026): .NET ab Version 10 und Debian ab Version 13 Trixie. Initiales Ziel-Framework für Bibliothek und CLI-Beispiel ist `net10.0`; Mindestarchitektur ist ARM64 (`linux-arm64`), insbesondere NanoPi R76S. Neuere stabile .NET-/Debian-Versionen werden in die geprüfte Kompatibilitätsmatrix aufgenommen; Preview-Versionen sind nicht automatisch zugesichert. Visual Studio 2026 und SDK 10.0.303 sind als lokale Ausgangsbasis festgestellt; Paketversionen, weitere ARM-Varianten und Deployment-Modell bleiben festzulegen. Bibliothek, echter Paketkonsument und Beispiel müssen auf Debian 13/ARM64 mit .NET 10 geprüft werden. Plattformanforderung und tatsächlich nachgewiesene Kompatibilität bleiben getrennt; ohne dokumentierten Test kein behaupteter Laufzeiterfolg.

Quellen (geprüft am 14.10.2026): [.NET-Support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [.NET-10-OS-Matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md), [Debian-Versionen](https://www.debian.org/releases/).

## Umsetzung in Phasen

| Phase | Inhalt und Ergebnis | Abhängigkeiten | Anforderungsbezug |
|---|---|---|---|
| P0 – abgeschlossen | Grundlegende Entscheidungen dokumentiert; Details Umsetzungsphasen zugeordnet | Keine | REQ-001–009, 054–056, 064, 086, 090–093, 102–106 |
| P1 | .NET-Grundstruktur, Testbasis und CI-Konzept/-Pipeline | P0; Implementierungsauftrag, für GitHub gesonderte Freigabe | REQ-004–007, 009–011, 070–073, 078, 080–086, 090, 093 |
| P2 | Exportentitäten, Einheiten-/Zeitnormalisierung und echte JSON-/XML-Serializer | P1 | REQ-050–056, 067–069, 074–078, 082, 105–108 |
| P4 | Gemeinsame Geo-Auswahl und beide Stationskataloge | P2 | REQ-020–024, 065–068, 100–102 |
| P5 | DWD-Messdaten als erster vollständiger Quellenlauf | P4 | REQ-030–032, 050–056, 082 |
| P6 | OsthessenWetter-Messdaten mit Zeitqualitätskennzeichnung | P5; geprüfter Quellenzugang | REQ-040–046, 050–056, 082, 092 |
| P7 | MOSMIX-Vorhersagen mit Ausgabe- und Gültigkeitszeiten | P5; P2-Vorhersagevertrag | REQ-051–055, 065–069, 100–108 |
| P8 | Bibliotheks-MQTT-Serializer und vollständige Bibliotheksintegration | P4–P7 | REQ-008, 050, 060–069, 072–073, 082, 105 |
| P3 | CLI-Nutzungsbeispiel, Konfiguration, Ausgabe und Brokertransport | P8; vollständige Bibliotheksabnahme | REQ-006–011, 030–031, 040, 064–069, 071–073, 103 |
| P9 | Debian-/ARM64-Abnahme, externer Workflow und Release-Vorbereitung | P3 | REQ-004–009, 080–086, 090–093 |

Die Phasen sind eine Abhängigkeitsplanung, keine Terminzusage. P6 und P7 sind fachlich unabhängig, sobald ihre Voraussetzungen vorliegen. Zunächst wird ein vollständiger DWD-Bibliothekslauf mit Serializer-Vertragstests nachgewiesen; die spätere MQTT-Integration verwendet denselben Ergebnisvertrag. P3 folgt erst nach fertiger Bibliothek.

### P0 – abgeschlossen am 14.10.2026

P0 ist durch ausdrücklichen Projektauftrag abgeschlossen. Architektur, Plattform, NuGet-Hauptprodukt, Konstruktor-Endpunkte, Zeit-/Rundungsvertrag, Programmsprache, Testwerkzeuge und Coverage-Grenzfälle sind entschieden. Der Abschluss betrifft die Vorplanung, keine implementierte Funktion.

| Verbleibende Detailarbeit | Zuständige Phase |
|---|---|
| Aktuelle stabile Abhängigkeiten auflösen, Lizenzen prüfen, SDK fixieren und Coverage-Reports nachweisen | P1 |
| Entitäten, XML-Namensräume, Schemaversionierung und XSD-Erzeugung | P2 |
| Quellenformate, Elementzuordnung, Attribution und Abrufbedingungen prüfen | jeweilige Quellenadapterphase |
| CLI-Konfiguration, Exitcodes, Bereitstellungsmodell und Beispiel | P3, nach fertiger Bibliothek |
| Öffentliche NuGet-ID, Releaseversionierung und Veröffentlichungsweg | P9, vor Veröffentlichung |

ARM64 bleibt die Mindestarchitektur; zusätzliche ARM32-Unterstützung ist eine mögliche spätere Erweiterung und blockiert P1 nicht. Diese zugeordneten Details eröffnen P0 nicht erneut. Implementierungs-/Laufzeitnachweise bleiben bis zu ihrer jeweiligen Abnahme offen.

### P1 – Grundstruktur, Testbasis und CI

Der [Detailplan P1](PHASE_P1_PLAN.md) konkretisiert Voraussetzungen, Projektgrenzen, zwölf Arbeitspakete, vierundzwanzig Testfälle, Coverage-Prüfung und Phasenabnahme. Er ist ebenfalls nur Planung und startet keine Implementierung.

P1 schafft die Bibliotheks-, Paket-, Test- und CI-Grundlage mit technischem NuGet-Integrationstest. Die fachlichen Bibliothekskomponenten folgen in P2/P4–P8; das CLI-Beispiel wird danach in P3 entwickelt. Die CLI konsumiert eine versionierte Paketreferenz, geprüft über eine lokale `.nupkg`. Die konkrete Anzahl der .NET-Projekte wird möglichst klein gehalten und in P0/P1 begründet. Jede ausführende Klasse bekommt ein passendes Interface; reine DTOs brauchen keines.

Geplant sind Pack-/API-/Konsumententests, SDK-/Paketfixierung, Secret-/Build-Ausschlüsse, reproduzierbarer Restore, Testprojekt, ein erster sinnvoller Red/Green-Verhaltenstest und die CI-Erfassung. Noch keine produktiven Quellenabrufe oder Geräteänderungen.

Die CI soll Build, Offline-Tests, maschinenlesbare Test-/Coverage-Ergebnisse und lesbare Berichte ausführen. PR- und aktueller Zielbranch-Commit werden mit vergleichbarem Messumfang erfasst, beide Commit-IDs dokumentiert und ungerundete Zeilen-/Branch-Verhältnisse geprüft. REQ-086 einschließlich der kennzahlbezogenen 100-%-Ausnahme anwenden; fehlende Berichte oder weniger als 80 % führen zum Fehlschlag. Ein Cross-Publish für ARM64 ergänzt die Pipeline, ersetzt aber keinen ARM-Laufzeittest. Öffentliche PRs erhalten keine produktiven Secrets; Ausführung nicht vertrauenswürdigen PR-Codes darf keine privilegierten Veröffentlichungsrechte erhalten.

**Abnahme:** Lokaler reproduzierbarer Build/Testlauf, dokumentierter TDD-Zyklus, geklärter Coverage-Start und zutreffender CI-Status. Eine Pipeline ohne ausgeführten GitHub-Lauf wird nicht als auf GitHub bestanden bezeichnet.

### P2 – Paketentitäten und Serialisierung

Geplante Entitätsfamilien: CDC-/MOSMIX-Stationslisten mit jeweiligen Einträgen und Suchparametern, Messdatensätze mit typisierten Wetterwerten, Vorhersageläufe mit Zeitpunkten/Intervallen/Werten sowie Qualitäts- und Exportfehlerdaten. Typnamen und komplette Verträge werden vor Implementierung festgelegt. Alle eigenen Exporttypen sind `[Serializable]`.

Tests entstehen zuerst für verfügbare/fehlende Werte, echte Nullwerte, Druckumrechnung und -referenzen, UTC ohne Suffix, geschätzte Daten, führende Nullen und Sonderzeichen. JSON wird mit `System.Text.Json.JsonSerializer`, XML mit `System.Xml.Serialization.XmlSerializer` aus denselben Entitäten erzeugt. Eingeschachtelte Objekte und vollständige Dokumente werden mit echten Serializern geprüft, nicht ausschließlich gemockt.

**Abnahme:** Fachlich gleichwertige, gültige JSON-/XML-Dokumente, korrekt ausgelassene fehlende Werte, keine Zeitzonen-/ID-Verluste und testbare Serializer-Dienste mit Interfaces/DI. Noch kein Netzwerk erforderlich.

### P4 – Koordinaten und Stationsauswahl

Gemeinsame Koordinaten-/Geo-Logik unterstützt Dezimalgrad und DMS, inklusive S/W, Grenzen und Kulturunabhängigkeit. Entfernung und absoluter Höhenfilter gelten einschließlich Randwerten.

DWD-CDC- und MOSMIX-Katalogadapter bleiben getrennt. Sie erhalten IDs/Metadaten und ermitteln produktbezogene Größenverfügbarkeit, ohne vollständige Sensorausstattung zu unterstellen. MOSMIX-IDs können alphanumerisch sein; fehlende Höhen dürfen den Höhenfilter nicht umgehen. Beide Listen sind stabil sortiert und haben definierte leere Ergebnisse.

**Abnahme:** Offline-Katalogfixtures, Filter-/Randwerttests und vollständige JSON-/XML-Ausgaben für `--SelectDWD` und `--SelectMOSMIX`. Ein ausdrücklich aktivierter Live-Abgleich ist getrennt von Offline-Erfolg zu dokumentieren.

### P5 – DWD-Messdaten

In einem ersten vertikalen Durchstich werden Bezugsmessstation, Abruf, CSV/ZIP-Parsing, Fehlwerte, Qualitätsdaten, Messzeiträume, Normalisierung und Export verbunden. Ein injizierbarer HTTP-Zugang hat begrenzte Timeouts/Downloadgrößen und behandelt fehlende Produkte kontrolliert. Zusätzliche Größen werden nur bei belegter Verfügbarkeit eingebunden.

**Abnahme:** `--ParseDWD` erzeugt für eine ausdrücklich konfigurierte/übergebene Station gültiges JSON und XML. Fehlende Station verursacht weder Abruf noch MQTT; fremde Stationen werden nicht ergänzt. Offline-Tests decken Header-/Formatfehler, falsche IDs, Einheiten, `-999` und unterschiedliche Messzeiten ab. Live-Datenqualität wird separat protokolliert.

### P6 – OsthessenWetter

Die OsthessenWetter-Seitenstruktur wird „as is“ in der zum Umsetzungszeitpunkt vorgefundenen Fassung verwendet. Bei der Implementierung sind Umsetzungsdatum, Quell-URL und die für das Parsing relevanten Strukturannahmen zu dokumentieren. Die Unterstützung bezieht sich auf diese dokumentierte Fassung; spätere Strukturänderungen können eine Parseranpassung erfordern. Erkennbare Formatabweichungen werden gemäß REQ-044 protokolliert; die Regelungen zu Zeitqualität und Quellenbedingungen bleiben bestehen.

Glob-Filter, mehrere Stationsresultate, Umlaute und kollisionsfreie Quellenkennungen werden mit synthetischen HTML-Fixtures getestet. Die Datumsableitung aus Abrufzeit und `Europe/Berlin` erhält `estimated_date`; Mitternacht, Sommerzeitlücken/-mehrdeutigkeit und veraltete bzw. Offline-Anzeigen werden berücksichtigt. Kein Umetikettieren von Böen/Regenmengen.

**Abnahme:** Alle passenden Stationen werden nachvollziehbar exportiert; keine Treffer, Formatwechsel oder ungeklärte Zeitangaben erzeugen definierte Fehler. Geschätzte Zeitpunkte bleiben für Verbraucher sichtbar. Ein Quellenzugang ohne geklärte Bedingungen gilt nicht als produktiv abgenommen.

### P7 – MOSMIX-Vorhersagen

### Getrennte MOSMIX-Parser und Aktualisierung

Die Bibliothek bietet zwei eigenständige Parser mit eigenen Interfaces: `MosmixSParser` / `IMosmixSParser` für `MOSMIX_S` und `MosmixLParser` / `IMosmixLParser` für `MOSMIX_L`. Beide erhalten ihre jeweiligen Quell-URLs über Konstruktorparameter und werden separat über die DI-Integration konfiguriert. Gemeinsame KML-/KMZ-Hilfslogik darf wiederverwendet werden; ein einzelner umschaltbarer Parser ersetzt die beiden öffentlichen Verträge nicht. Bei fehlenden Daten gibt es keinen stillschweigenden Wechsel zum anderen Produkt.

| Produkt | DWD-Ausgabehäufigkeit | Laufzeitangaben |
|---|---|---|
| `MOSMIX_S` | Stündlich | Neuer Produktlauf je Stunde |
| `MOSMIX_L` | Viermal täglich | 03, 09, 15 und 21 UTC |

Diese Angaben beschreiben die DWD-Produktzyklen, keinen internen Scheduler und keine garantierte Dateiverfügbarkeit exakt zur Laufzeit. Ausgabezeitpunkt, Abrufzeitpunkt und Vorhersagegültigkeit bleiben getrennt. Der tatsächliche Vorhersagehorizont kommt aus der Datei-Zeitachse; eine neue Dateiausgabe beweist nicht die Neuberechnung sämtlicher darin enthaltener Zeitschritte. Aktualitätsgrenzen und externe Abrufpläne müssen produktbezogen festgelegt werden.

Das CLI-Beispiel behält `--ParseMOSMIX` und verlangt zusätzlich `--Product MOSMIX_S|MOSMIX_L` zur eindeutigen Auswahl des Bibliotheksparsers. Fehlende oder unbekannte Produkte sind Fehler vor Quellenabruf. `--SelectMOSMIX` behält die bisherigen Geo-/Höhenparameter und weist die bekannte Verfügbarkeit/Elemente getrennt nach Produkt aus; unbekannte Verfügbarkeit bleibt gekennzeichnet. Ausgabemetadaten, Quellenkennung und konfigurierte MQTT-Topics müssen S und L unterscheiden, damit ihre retained-Nachrichten nicht unbeabsichtigt einander ersetzen.

Quelle, geprüft am 14.10.2026: [DWD-Verfahrensbeschreibung, Abschnitt 2.1.3 und Tabelle 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). Produktzyklen vor Implementierung/Release erneut prüfen; die Bibliothek enthält derzeit noch keinen implementierten Parser.


Die beiden getrennten Produktadapter verarbeiten KML/KMZ, Namensräume, Standortzuordnung, Quell-Ausgabezeit, Parameterreihen und Gültigkeitszeitachse. Tests für Fehlwerte und Reihenlängen verhindern Zeitverschiebungen. Energie-/Niederschlagsintervalle werden nur fachlich belegt normalisiert. Externe XML-Entitäten/DTDs bleiben deaktiviert; Archivgrenzen werden getestet.

**Abnahme:** `--ParseMOSMIX --Product MOSMIX_S|MOSMIX_L --MOSMIXID` liefert einen vollständigen Vorhersagelauf mit getrenntem `issued_at`, `retrieved_at` und `valid_at`, ohne `measured_at`. JSON/XML sind fachlich gleichwertig; falsche ID, veralteter Lauf, beschädigte Struktur und fehlende Parameter sind geprüft.

### P8 – MQTT-Serializer und vollständige Bibliotheksintegration

Den generischen MQTT-Serializer mit konfigurierbaren Topics ohne Brokerverbindung prüfen. Alle Bibliotheksparser, Ausgabeentitäten, JSON-/XML-/XSD-Verträge und DI durch tatsächlichen Paketkonsum integrieren. Vereinbarte Topic-, QoS- und Retain-Regeln beachten.

**Abnahme:** Vollständige Bibliotheksfunktionen für DWD-Messungen, OsthessenWetter und beide MOSMIX-Produkte über die Paket-API geprüft. Bibliothek unabhängig von CLI und Broker konsumierbar. Erst nach dieser Bibliotheksabnahme P3 beginnen; Beispieltransport, Broker-Bestätigungstests und Integration der fünf Commands gehören zu P3.

### P3 – CLI und Exportpipeline

Voraussetzung: P8 und vollständige Bibliotheksabnahme. CLI ausschließlich als Konsument des fertigen Pakets entwickeln, einschließlich Beispiel-MQTT-Transport, QoS-1-Bestätigungen und der fünf Commands. Bibliotheksphasen werden über direkte API-Tests abgenommen; dort genannte Command-Namen bezeichnen das entsprechende Bibliotheksverhalten und verlangen keine vorgezogene CLI.

Die CLI akzeptiert genau einen Befehl und validiert Optionen vor Abruf. Konfiguration und CLI werden deterministisch aufgelöst. Fehlende DWD-Station oder MOSMIX-ID sowie ungültige Muster/Formate führen zu aussagekräftigen Fehlern.

`--Output mqtt|json|xml` und `--OutputFile` werden gemäß Vertrag vorbereitet. JSON/XML laufen brokerunabhängig, erzeugen genau ein Dokument und respektieren bestehende Dateien. Atomare Dateifertigstellung bzw. dokumentiertes Fehlerverhalten verhindern irreführende Teil-Dateien. Logs, Ausgabe und Exitcodes bleiben getrennt; Ressourcen und Abbruch werden kontrolliert behandelt. Noch nicht implementierte Quellen dürfen nur mit Test-Doubles geprüft und nicht als funktionierende Live-Befehle ausgewiesen werden.

**Abnahme:** Prozess-/Command-Tests belegen genau eine Ausführung und anschließendes Ende; ungültige Eingaben haben keine externen Nebenwirkungen. Exporttests prüfen Datei, Standardausgabe, Schreibfehler, vorhandene Ziele und Teilfehler.

### P9 – ARM64-Abnahme und Nutzung

Auf dem dokumentierten Debian-/ARM64-System folgen Testläufe der veröffentlichten Artefakte: nichtinteraktive Ausführung, echte Serializer, UTF-8, UTC/Quellzeitzonen, Dateirechte, Exitcodes, Signalbehandlung und Ressourcenfreigabe. Offline-Fixtures, Live-Quellen und Testbroker-Ergebnisse werden getrennt erfasst.

Zweisprachige Anleitungen beschreiben Konfiguration und Aufruf durch cron/n8n, ohne eine interne Zeitsteuerung zu installieren. Sie erklären Topic-Rechte, Zeitschätzungen, Quellenbedingungen, Einschränkungen, Aktualitätsprüfung und Fehlerbehandlung. Release-Artefakte erhalten Versions-/Prüfsummeninformationen und nachvollziehbare Build-/Testergebnisse. Das versionierte NuGet-Paket ist das primäre Release-Artefakt; CLI-Artefakte dienen nur als Beispiele. API-Kompatibilität, Paketinhalt und Konsum ohne Quellbaum werden abgenommen. Feed-/GitHub-Veröffentlichung oder Deployment erfolgt erst im beauftragten Umfang.

**Abnahme:** Unabhängiger NuGet-Konsum, Bibliotheks-API, MQTT-Serializer mit konfigurierten Topics und DI-Integration sind primäre Produktnachweise; die CLI bleibt Beispiel. ARM64-Laufzeitnachweis, erfolgreiche fachliche Akzeptanztests, vollständige Dokumentation und offene Einschränkungen sind vorhanden. Gemockte Tests oder Cross-Publish ersetzen die Zielsystemabnahme nicht.

## Definition of Done je Änderung

- Anforderung und passendes Interface/Entitätsmodell sind benannt; keine nicht beauftragten Funktionen.
- Red/Green/Refactor mit aussagekräftigen Verhaltens-/Regressionstests; MSTest/Moq/FluentAssertions und Setup/Cleanup-Vorgaben eingehalten.
- Build und relevante Tests bestehen. Coverage-Berichte sind vorhanden und erfüllen 80 % sowie die PR-Regel einschließlich der exakten 100-%-Ausnahme, soweit ein PR vorliegt unter den beschlossenen Start-/Dokumentationsregeln. Offene Gates bleiben offen.
- Alle Kommentare sind Englisch; beide Dokumentfassungen sind konsistent. IDs, Einheiten und UTC-Vertrag bleiben erhalten.
- Keine Secrets, produktiven Nebenwirkungen oder falschen Abnahmebehauptungen. Zielplattformtests und ausgelassene Prüfungen sind klar ausgewiesen.

## Nächster konkreter Schritt

P1.02-Prüfung ist abgeschlossen. P1.03 hat den SDK-Befund geschlossen: global.json ist auf SDK 10.0.402 umgestellt, Locked Restore und CLI-Release-Build sind erfolgreich nachgewiesen ([Protokoll](docs/p1/P1_03_BUILD_SETUP.md)). Der Visual-Studio/MSBuild-Build-Fehler NETSDK1141 wurde behoben (durch `dotnet build-server shutdown` und einen vollständigen Neustart von Visual Studio); ein anschließender IDE-Solution-Build war erfolgreich. Offen bleibt die Reproduzierbarkeit auf sauberem Checkout nach dem ersten Commit. Paketversionen bleiben unverändert; weitere P1-Abnahmen stehen aus.

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


P1.01-Eingangsprüfung am 15.10.2026 abgeschlossen: [Protokoll und Ausgangsinventur](docs/p1/P1_01_BASELINE.md). Vorhandene Projektstruktur erhalten; kein neuer Fachcode und kein Test-/Coverage-Ergebnis behauptet. P1.02-Prüfung inzwischen abgeschlossen; aktueller Folgeschritt unten dokumentiert.


**P1.02-Prüfung (15.10.2026) abgeschlossen:** [Werkzeug-/Lizenz-/Kompatibilitätsbericht](docs/p1/P1_02_TOOLING_REVIEW.md). Alle 16 Testpakete geprüft; sechs direkte Versionen aktuell, keine gemeldeten NuGet-Schwachstellen/Abkündigungen.

**P1.03 (15.10.2026) abgeschlossen; Visual-Studio-Build-Einschränkung behoben:** [Build-Setup-Protokoll](docs/p1/P1_03_BUILD_SETUP.md). global.json auf SDK 10.0.402 umgestellt, Locked Restore und CLI-Release-Build erfolgreich; Build-/Secret-Ausschlüsse in .gitignore ausreichend. Der Visual-Studio/MSBuild-Fehler NETSDK1141 wurde durch `dotnet build-server shutdown` und einen vollständigen Neustart von Visual Studio behoben; ein anschließender IDE-Solution-Build war erfolgreich. Reproduzierbarkeit auf sauberem Checkout (noch kein Git-Commit vorhanden) steht weiterhin aus.

**P1.04 (16.10.2026) abgeschlossen:** [Paketkonsumenten-Protokoll](docs/p1/P1_04_PACKAGE_CONSUMER.md). Lokales Paket `WeatherParser.Parsing.0.1.0-initial.nupkg` gebaut, projektlokale Paketquelle eingerichtet, isolierter Konsument `tests/WeatherParser.Package.Tests` mit reiner PackageReference angelegt und getestet (2/2 Tests erfolgreich).
