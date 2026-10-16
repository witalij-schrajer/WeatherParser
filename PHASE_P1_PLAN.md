# WeatherParser – Detailplan P1

[Deutsch](PHASE_P1_PLAN.md) | [English](PHASE_P1_PLAN.en.md)

Stand: 16.10.2026. **P0 ist abgeschlossen. P1.01-P1.06 abgeschlossen; die Visual-Studio-Build-Einschränkung ist behoben, die Reproduzierbarkeit auf sauberem Checkout steht noch aus.** Der aktuelle Auftrag hat in P1.06 den vollständigen DI-Einstiegspunkt (`AddWeatherParsing`) testgetrieben abgesichert; Coverage-Report-Vertrag (P1.07), weitere fachliche Tests, XSDs und CI-Workflows stehen aus.

Grundlage: [Anforderungen](REQUIREMENTS.md), [Entwicklungsplan](DEVELOPMENT_PLAN.md) und [Repository-Regeln](AGENTS.md).

## 1. Ziel und Umfang

P1 schafft die reproduzierbare Build-, Paket-, Test- und CI-Grundlage für die eigenständig nutzbare NuGet-Bibliothek. Sie umfasst Parser, Ausgabeentitäten, generischen MQTT-Serializer und DI-Integration; diese fachlichen Komponenten werden in den nachfolgenden Phasen implementiert. Ein isolierter Paketkonsument belegt bereits in P1 die tatsächliche NuGet-Nutzung.

Die endgültige Beispiel-CLI einschließlich Argumenten, Konfiguration, Exitcodes, Bereitstellungsmodell und Ablauf wird erst in P3 entwickelt. P1 implementiert keinen CLI-Dispatcher, keine Wetteradapter, Exportentitäten, XSD-Generierung oder MQTT-Brokeranbindung. Keine Installation oder Konfigurationsänderung auf R76S ist Teil dieser Phase.

## 2. Beschlossene Grundlagen und verbleibende Aufgaben

| Thema | Verbindlicher Stand | Aufgabe in P1 |
|---|---|---|
| Plattform | .NET ≥10, `net10.0`, Debian ≥13, mindestens ARM64 | Kompatibilitätsmatrix und Paketkonsum prüfen |
| Entwicklungsumgebung | Visual Studio 2026; lokal nachgewiesen 18.8.3 mit SDK 10.0.303 | Wartungsstand prüfen; Ausgangsversion reproduzierbar fixieren, notwendige Änderung dokumentieren |
| Abhängigkeiten | Neueste stabile Versionen bei Aufnahme, kompatible Lizenzen | Offizielle Quellen prüfen, Versionen auflösen und fixieren; Konflikte offen ausweisen |
| Tests | MSTest, Moq, FluentAssertions, TDD | `[TestInitialize]`, `[TestCleanup]` und `VerifyAll()` verbindlich einrichten |
| Coverage | Microsoft.CodeCoverage-Report einschließlich Codeblöcken | Echtes Reportformat, Zähler und automatisierte Auswertung nachweisen |
| Sprache | Programmausgaben und Kommentare Englisch; Dokumentation Deutsch/Englisch | Beide Dokumentfassungen und Entwicklerabläufe synchron halten |
| Architektur | Konstruktor-DI, Interface je Doing-Klasse, konfigurierbare Endpunkte/Topics | Minimal benötigte öffentliche Paketverträge und DI-Grenzen prüfen |
| Veröffentlichung | NuGet-Hauptprodukt, CLI nur Beispiel | Lokale Paket-ID/-Version für Tests dokumentieren; öffentliche ID/Releaseverfahren bleiben Release-Aufgaben |

P0 ist kein offenes Gate mehr. Versionsauflösung und Coverage-Reportprüfung sind P1-Arbeit, keine neuen fachlichen P0-Fragen. GitHub-Aktivierung und Veröffentlichung werden gesondert beauftragt; lokale Prüfung bleibt davon unabhängig. Die Umsetzung von P1 benötigt weiterhin einen ausdrücklichen Implementierungsauftrag.

## 3. Vorgesehene Struktur

| Ablage | Aufgabe |
|---|---|
| `src/WeatherParser.Parsing/` | Packbare Bibliothek mit `net10.0`; keine CLI- oder Brokertransport-Abhängigkeit |
| `tests/WeatherParser.Parsing.Tests/` | Verhaltenstests mit Quellprojektbezug |
| `tests/WeatherParser.Package.Tests/` | Isolierter Konsument der tatsächlich gebauten `.nupkg` über `PackageReference` |
| `examples/WeatherParser.Cli/` | Für P3 vorgemerkt; in P1 kein CLI-Gerüst erforderlich |
| Repository-Wurzel | Solution, `global.json`, Build-/Paketkonfiguration, Lockdaten, `.editorconfig`, `.gitignore` |
| spätere CI-Ablage | Restore-, Build-, Pack-, Test- und Coverage-Prüfung samt Artefakten |

Die Namen sind Arbeitsnamen. Projekte entstehen nur mit konkretem Bedarf. Kein leeres Infrastrukturprojekt und keine Klassen allein zum Erreichen einer Coverage-Zahl. Öffentliche Paket-ID ist durch diese Pfadnamen nicht zugesichert. Beispiel- und Paketkonsumenten nutzen das echte Paket; Quellreferenzen sind nur in Bibliotheks-Unit-Tests zulässig.

## 4. Arbeitspakete

| Paket | Arbeit | Ergebnis / Abhängigkeit |
|---|---|---|
| P1.01 | Arbeitsstand und P0-Abschluss prüfen; P1-Umfang bestätigen | Abgeschlossen am 15.10.2026; [Ausgangsprotokoll](docs/p1/P1_01_BASELINE.md) |
| P1.02 | SDK-/Paketstand, Lizenzen und Kompatibilität prüfen | Prüfung abgeschlossen; SDK-Update offen; [Prüfbericht](docs/p1/P1_02_TOOLING_REVIEW.md) |
| P1.03 | Minimale Solution, Bibliothek und Testprojekte vorbereiten | Abgeschlossen (SDK 10.0.402, Locked Restore, CLI-Build, IDE-Build); [Protokoll](docs/p1/P1_03_BUILD_SETUP.md); Reproduzierbarkeit auf sauberem Checkout offen |
| P1.04 | Lokales NuGet-Pack und isolierten Paketkonsumenten aufbauen | Abgeschlossen; [Protokoll](docs/p1/P1_04_PACKAGE_CONSUMER.md) |
| P1.05 | MSTest/Moq/FluentAssertions-Testbasis und TDD-Nachweis | Abgeschlossen (Parser-Endpunktvalidierung, 6 Tests grün); [Protokoll](docs/p1/P1_05_TDD_BASELINE.md) |
| P1.06 | Minimal benötigte Paket-/DI-Verträge testgetrieben absichern | Abgeschlossen (`AddWeatherParsing`-Einstiegspunkt, echte DI-Auflösung, fehlende Registrierung erkannt, 8 Tests grün); [Protokoll](docs/p1/P1_06_DI_CONTRACTS.md) |
| P1.07 | Microsoft.CodeCoverage erfassen und Reportvertrag prüfen | Rohreport, Codeblock-Aufschlüsselung, belegte Zählerzuordnung; nach P1.05/P1.06 |
| P1.08 | Coverage-Auswertung und Ausnahmen testgetrieben umsetzen | Grenz-/Fehlerfälle nach Abschnitt 5; nach P1.07 |
| P1.09 | Lokale CI-Jobfolge und Artefakte vorbereiten | Ausführbare Prüfung ohne produktive Secrets; nach P1.04–P1.08 |
| P1.10 | Debian-/ARM64-Kompatibilität der Paketgrundlage prüfen | Paket-/Restore-Nachweis, Laufzeiterfolg nur bei tatsächlicher Ausführung; nach P1.09 |
| P1.11 | Entwickleranleitung und Nachweise zweisprachig fertigstellen | Restore/Build/Pack/Test/Coverage-Abläufe und Grenzen; laufend |
| P1.12 | Abnahme durchführen und P2-Übergabe dokumentieren | Prüfergebnisse, Artefakte und verbleibende Einschränkungen; nach allen Paketen |

### Testgetriebener Einstieg

Zuerst ein konkret benötigtes Bibliotheksverhalten auswählen, etwa die Validierung und eindeutige Zuordnung vom Konsumenten gelieferter Parser-Endpunkte in der DI-Integration. Erwarteten Vertragsfehler nachweisen, minimal implementieren, unter grünen Tests refaktorieren. Nur tatsächlich benötigte Services mit Interfaces anlegen. Nicht implementierte Parser nicht als funktionsfähig registrieren oder erfolgreich simulieren. Ein Restorefehler und reine Existenztests sind kein TDD-Nachweis.

Testobjekte und gemeinsame Mocks entstehen in `[TestInitialize]`; `[TestCleanup]` prüft alle Mocks mit `VerifyAll()`. Echte DI-Auflösung und echter Paketkonsum ergänzen die Mock-Tests. Tests bleiben offline. Serializer-, Zeit-/Rundungs- und XSD-Vertragstests folgen P2, Quellenadaptertests P4–P7.

## 5. Coverage-Vertrag

Microsoft.CodeCoverage liefert die Rohreports und die Codeblock-Aufschlüsselung. Das tatsächlich unterstützte Format und die Integration mit MSTest werden anhand der gewählten stabilen Toolversion geprüft. Codeblöcke sind keine Branches: Ein Reportwert darf nicht ohne Nachweis als andere Kennzahl ausgegeben werden. Die bisherigen Zeilen-/Branch-Anforderungen bleiben erhalten. Falls eine geforderte Kennzahl nicht nachweisbar ist, wird diese konkrete Einschränkung dokumentiert und vor einer bestandenen Coverage-Abnahme entschieden; weder Werte erfinden noch stillschweigend auf Block-Coverage umstellen.

| Fall | Prüfung je Kennzahl |
|---|---|
| Erster Implementierungsstand ohne messbare Zielbranch-Baseline | Mindestens 80 %; kein Zielbranch-Vergleich |
| Normaler Code-PR, Baseline unter 100 % | Mindestens 80 % und strikt höher als die Baseline |
| Baseline exakt 100 % | PR muss exakt 100 % erreichen; Gleichstand zulässig |
| Reiner Dokumentations-PR | Mindestens 80 %; Gleichstand zulässig, Rückgang nicht |
| Unerwartet fehlende/spätere defekte Baseline | Fehler; keine automatische Startausnahme |
| Fehlender/defekter PR-Report oder Null-Nenner | Fehler; keine erfundene Coverage |

Ungerundete Zählerverhältnisse statt Anzeigeprozente vergleichen. Dokumentations-PRs anhand des gesamten Diffs identifizieren: Änderungen an Quellcode, Projekten, Paketen, Build-/Test-/Coverage-/CI-Konfiguration oder Ausschlüssen sind keine reinen Dokumentationsänderungen. Ein Dateiname allein begründet keinen Bypass.

Produktionsumfang, Ausschlüsse, Toolversionen, Reportformat sowie PR- und Zielbranch-Commit dokumentieren. Testcode nicht als Produktionscode zählen. Dieselbe Bibliotheksassembly aus Quell- und Pakettests nicht doppelt zählen; Version/PDB/Quellprovenienz prüfen. Gesamtzähler aggregieren, keine Projektprozente mitteln. Der Coverage-Prüfer selbst erhält Verhaltens- und Regressionstests; keine Fachlogik für Kennzahlen ausblenden.

## 6. Geplante Testfälle

Diese IDs sind Planungskennungen, noch keine existierenden Tests.

| Test-ID | Gegenstand | Erwartung |
|---|---|---|
| T-P1-01 | Benötigtes erstes Bibliotheksverhalten | Dokumentierter Red→Green→Refactor-Zyklus |
| T-P1-02 | Ungültiger Endpunkt | Definierter englischer Fehler, kein Abruf |
| T-P1-03 | Unterschiedliche Parser-Endpunkte | Keine Verwechslung oder globale URI-Ersetzung |
| T-P1-04 | Interfaces/Konstruktor-DI | Keine versteckten Service-Abhängigkeiten |
| T-P1-05 | Echte DI-Auflösung | Benötigte Registrierung funktioniert; fehlende wird erkannt |
| T-P1-06 | Setup/Cleanup | Testaufbau und VerifyAll-Vorgabe eingehalten |
| T-P1-07 | Offline-Ausführung | Kein produktiver HTTP-/MQTT-/Gerätezugriff |
| T-P1-08 | Restore/Versionen | Sauberer Checkout reproduzierbar; unkontrollierte Änderungen erkannt |
| T-P1-09 | Mindestgrenze | 80 % besteht, darunter scheitert jede Kennzahl |
| T-P1-10 | Normaler Code-PR | Gleichstand unter 100 % scheitert, echter Anstieg besteht |
| T-P1-11 | Gemischte Kennzahlen | Eine Verbesserung kompensiert keinen Verstoß der anderen |
| T-P1-12 | Präzision | Gerundete 100 % werden nicht als exakte 100 % behandelt |
| T-P1-13 | Defekter/fehlender Report | Fehler statt grünem Ergebnis |
| T-P1-14 | Coverage-Start | Baseline initial nicht vorhanden: ≥80 %, kein Vergleich; später fehlende Baseline scheitert |
| T-P1-15 | Commit-Zuordnung | Veraltete/falsche Baseline erkannt |
| T-P1-16 | Vergleichbarkeit | Andere Tools/Ausschlüsse/Zählersemantik erkannt |
| T-P1-17 | Aggregation | Gesamtzähler und keine doppelte Paketassembly |
| T-P1-18 | Ausnahmen | Exakt 100 %→100 % und Dokumentations-Gleichstand bestehen; Rückgänge scheitern |
| T-P1-19 | CI-Fehlerweitergabe | Tests/Coverage schlagen Job fehl; Diagnose bleibt verfügbar |
| T-P1-20 | ARM64-Grundlage | Restore/Build-Nachweis getrennt von echtem Laufzeittest |
| T-P1-21 | Echtes NuGet-Artefakt | Isolierter Konsument verwendet gebaute Paketversion |
| T-P1-22 | Paketgrenze | Kein CLI-/Brokertransport im Paket; keine Quellreferenz im Pakettest |
| T-P1-23 | Dokumentationsklassifikation | Code-/Build-/Paket-/CI-/Coverage-Änderungen erhalten keinen Dokumentations-Bypass |
| T-P1-24 | Report-/Paket-Provenienz | Zähler, Codeblöcke, PDB und Paketversion nachvollziehbar; Block nicht als Branch umetikettiert |

## 7. CI-Ablauf und Liefergegenstände

1. SDK-/Paketprüfung, Restore und Build.
2. Lokales Pack, isolierter Konsumenten-Restore und Build.
3. Offline-Unit-/DI-/Pakettests mit maschinenlesbaren Testergebnissen.
4. Microsoft.CodeCoverage erfassen; Rohreport und lesbare Block-/Codezuordnung bereitstellen.
5. PR-Typ und vorhandene Baseline prüfen; passende Coverage-Regel anwenden.
6. `.nupkg`, Tool-/Lizenzliste, Test-/Coverage-Berichte, Commit-Zuordnung und Diagnosen als Artefakte bereitstellen.

Öffentliche PR-Ausführung ohne produktive Secrets oder privilegierte Veröffentlichungsrechte. Kein automatischer NuGet-/GitHub-Push. Eine lokal geprüfte Pipeline ist kein bestandener GitHub-Lauf; tatsächliche Aktivierung bleibt separat beauftragt. Bei fehlendem ARM64-Ziel nur die vorhandenen Build-/Konsumentennachweise ausweisen; vollständige Hardwareabnahme bleibt P9.

## 8. Abnahme und Übergabe

- [ ] P0-Abschluss und P1-Implementierungsauftrag dokumentiert.
- [ ] Minimaler Build, reproduzierbarer Restore und fixierte kompatible Abhängigkeiten mit Lizenznachweisen.
- [ ] Tatsächliches `.nupkg` und isolierter Paketkonsum geprüft.
- [ ] Sinnvoller TDD-Zyklus, Interfaces, Konstruktor-DI, MSTest/Moq/FluentAssertions und Setup/Cleanup nachgewiesen.
- [ ] Microsoft-CodeCoverage-Report mit nachvollziehbarer Codeblock-/Kennzahlzuordnung vorhanden.
- [ ] Mindestens 80 % je geforderter Kennzahl und korrekte Start-/Dokumentations-/100-%-Regeln nachgewiesen.
- [ ] Testfälle für Fehler, Vergleichbarkeit, Provenienz und PR-Klassifikation bestanden.
- [ ] Lokale CI-Abläufe und Artefakte geprüft; GitHub-/ARM-Laufzeitstatus wahrheitsgemäß dokumentiert.
- [ ] Englische Kommentare/Ausgaben und gleichwertige deutsche/englische Dokumentation.
- [ ] P1-Abnahmeprotokoll mit Versionen, Arbeitsstand, Nachweisen und Einschränkungen erstellt.

P1 ist erst nach diesen Nachweisen abgeschlossen. P2 übernimmt die Test-/Paketbasis und entwickelt die Exportentitäten, UTC-/Einheiten-/Rundungslogik, echten JSON-/XML-Serializer und generierten Ausgabeschemas. CLI-Fragen bleiben P3; Quellenformate den jeweiligen Parserphasen und öffentliche Paketveröffentlichung P9 zugeordnet.


## Verbindlicher Phasenstatus und Ablauf

**15.10.2026: P0 abgeschlossen; P1-Solution-/Projektstruktur erstellt. P1 ist noch nicht abgenommen.**

Reihenfolge bei unveränderten Phasen-IDs: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Erst die vollständige Bibliothek einschließlich Stationsauswahl, DWD-/OsthessenWetter-/MOSMIX_S-/MOSMIX_L-Parsern, Ausgabeentitäten, JSON/XML, XSDs, MQTT-Serializer und DI fertigstellen und abnehmen. Danach P3 mit CLI-Nutzungsbeispiel, Konfiguration und Beispiel-Brokertransport beginnen. Öffentliches Release bleibt P9.

P1 enthält nur einen technischen Paketkonsumenten als Integrationstest, kein CLI-/Anwendungsbeispiel. Vor P3 Quellenfunktionen direkt über die Bibliothek und ihre Tests abnehmen; kein CLI-Aufruf ist Voraussetzung für die Bibliotheksphasen. Die Phasenreihenfolge ersetzt frühere numerische Reihenfolge oder Abhängigkeiten auf P3. [Detailplan P1](PHASE_P1_PLAN.md). Der aktuelle Auftrag prüft die vorhandenen Werkzeuge und Abhängigkeiten in P1.02; Fachimplementierung bleibt vertagt.


## Aktueller Umsetzungsstand (15.10.2026)

Der Strukturanteil von P1.03 ist angelegt: Solution mit zwei Projekten, fixiertes SDK, gemeinsame Build-/Paketkonfiguration und reservierter Pakettestbereich. Restore-/Buildprüfung wird im README festgehalten. P1.02 besitzt mit DEPENDENCIES.md erste Nachweise zur Paketauswahl; vollständige transitive Lizenz-/Kompatibilitätsabnahme steht aus. P1.04–P1.12, TDD-Tests, Coverage-/Reportprüfung und Phasenabnahme sind nicht abgeschlossen. Paketkonsument und CLI bleiben wie geplant vertagt.


P1.01-Eingangsprüfung am 15.10.2026 abgeschlossen: [Protokoll und Ausgangsinventur](docs/p1/P1_01_BASELINE.md). Vorhandene Projektstruktur erhalten; kein neuer Fachcode und kein Test-/Coverage-Ergebnis behauptet. P1.02-Prüfung inzwischen abgeschlossen; aktueller Folgeschritt unten dokumentiert.


**P1.02-Prüfung (15.10.2026) abgeschlossen:** [Werkzeug-/Lizenz-/Kompatibilitätsbericht](docs/p1/P1_02_TOOLING_REVIEW.md). Alle 16 Testpakete geprüft; sechs direkte Versionen aktuell, keine gemeldeten NuGet-Schwachstellen/Abkündigungen. SDK 10.0.303 nicht mehr gewartet; vor P1.03-Abnahme auf ausgewähltes 10.0.402 aktualisieren und prüfen. Kein SDK-/Paketwechsel und keine neuen Tests in dieser Prüfung.
